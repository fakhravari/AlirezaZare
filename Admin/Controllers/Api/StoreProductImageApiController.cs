using System.Text.Json;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
using Shop.Admin.Services;
using ImageMagick;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/product-images")]
public sealed class StoreProductImageApiController(ShopDbContext db, FtpMediaStorage storage, ILogger<StoreProductImageApiController> logger) : ControllerBase
{
    [HttpPost("{productId:long}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11_000_000)]
    public async Task<IActionResult> Upload(long productId, IFormFile image, CancellationToken cancellationToken)
    {
        if (image is null || image.Length is <= 0 or > 10_000_000)
            return BadRequest(new { title = "تصویر باید حداکثر ۱۰ مگابایت باشد." });

        var product = await db.Database.SqlQueryRaw<ProductKeyRow>("SELECT [ProductKey] FROM [Warehouse].[Product] WHERE [Id] = {0} AND [IsStoreEnabled] = 1", productId).SingleOrDefaultAsync(cancellationToken);
        if (product is null) return NotFound();
        var variants = new List<(string Path, byte[] Bytes)>();
        string filename = $"product-{productId}-{Guid.NewGuid():N}.webp";
        try
        {
            await using var stream = image.OpenReadStream();
            byte[] header = new byte[12];
            if (await stream.ReadAsync(header, cancellationToken) < header.Length ||
                !(header.AsSpan(0, 3).SequenceEqual(new byte[] { 0xff, 0xd8, 0xff }) ||
                  header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                  (header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8))))
                return BadRequest(new { title = "فقط تصویر JPEG، PNG یا WebP مجاز است." });
            stream.Position = 0;
            var info = new MagickImageInfo(stream);
            if ((long)info.Width * info.Height > 40_000_000)
                return BadRequest(new { title = "ابعاد تصویر نامعتبر یا بیش از حد مجاز است." });
            stream.Position = 0;
            using var source = new MagickImage(stream);
            source.AutoOrient();
            foreach (var variant in new[] { ("main", 1600), ("card", 800), ("thumbnail", 256) })
            {
                using var resized = source.Clone();
                resized.Resize(new MagickGeometry((uint)variant.Item2, (uint)variant.Item2) { Greater = true });
                resized.Strip();
                resized.Quality = 85;
                variants.Add(($"content/products/images/{variant.Item1}/{filename}", resized.ToByteArray(MagickFormat.WebP)));
            }
        }
        catch (Exception exception) when (exception is MagickException or NotSupportedException)
        { return BadRequest(new { title = "فایل انتخاب‌شده تصویر معتبر و پشتیبانی‌شده نیست." }); }

        AsyncFtpClient? ftp = null;
        var uploaded = new List<string>();
        bool databaseStarted = false;
        try
        {
            ftp = await storage.ConnectAsync(cancellationToken);
            foreach (var variant in variants)
            {
                string remote = "/" + variant.Path;
                var result = await ftp.UploadBytes(variant.Bytes, remote, FtpRemoteExists.Skip, true, null, cancellationToken);
                if (result != FtpStatus.Success) throw new IOException("Image upload failed.");
                uploaded.Add(remote);
            }
            string mediaKey = "product-" + product.ProductKey;
            string gallery = JsonSerializer.Serialize(new[] { new { src = variants[0].Path, thumbnail = variants[2].Path } });
            databaseStarted = true;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [Store].[ProductMedia] SET [MainPath]={variants[0].Path}, [CardPath]={variants[1].Path},
                    [ThumbnailPath]={variants[2].Path}, [ImagesJson]=CONCAT(LEFT({gallery}, LEN({gallery}) - 1),
                        CASE WHEN NOT EXISTS (SELECT 1 FROM OPENJSON(COALESCE([ImagesJson], N'[]'))) THEN N']'
                        ELSE CONCAT(N',', SUBSTRING(LTRIM(RTRIM([ImagesJson])), 2, LEN(LTRIM(RTRIM([ImagesJson]))))) END)
                WHERE [IdProduct]={productId} AND [MediaKey]={mediaKey};
                IF @@ROWCOUNT = 0
                    INSERT INTO [Store].[ProductMedia] ([IdProduct], [MediaKey], [MainPath], [CardPath], [ThumbnailPath], [ImagesJson], [VideosJson])
                    VALUES ({productId}, {mediaKey}, {variants[0].Path}, {variants[1].Path}, {variants[2].Path}, {gallery}, N'[]');
                """, cancellationToken);
            return Ok(new { message = "تصویر محصول و نسخه‌های آن ذخیره شد." });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Product image upload failed for product {ProductId}", productId);
            if (!databaseStarted && ftp is not null)
                foreach (string path in uploaded)
                    try { await ftp.DeleteFile(path, CancellationToken.None); } catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded image"); }
            if (exception is OperationCanceledException) throw;
            return Problem(statusCode: 502, title: "بارگذاری انجام نشد؛ اتصال FTP و تنظیمات سرور را بررسی کنید.");
        }
        finally { ftp?.Dispose(); }
    }

    public sealed class ProductKeyRow { public string ProductKey { get; set; } = ""; }
}
