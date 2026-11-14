using System.Text.Json;
using System.Text.Json.Nodes;
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

        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId && x.IsStoreEnabled, cancellationToken);
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
            var media = await db.StoreProductMedia.SingleOrDefaultAsync(x => x.IdProduct == productId && x.MediaKey == mediaKey, cancellationToken);
            if (media is null)
            {
                media = new Shop.Data.Domain.Store.Entities.StoreProductMedia
                {
                    IdProduct = productId,
                    MediaKey = mediaKey,
                    MainPath = variants[0].Path,
                    CardPath = variants[1].Path,
                    ThumbnailPath = variants[2].Path,
                    ImagesJson = gallery,
                    VideosJson = "[]"
                };
                db.StoreProductMedia.Add(media);
            }
            else
            {
                media.MainPath = variants[0].Path; media.CardPath = variants[1].Path;
                media.ThumbnailPath = variants[2].Path;
                var images = string.IsNullOrWhiteSpace(media.ImagesJson)
                    ? new JsonArray()
                    : JsonNode.Parse(media.ImagesJson)?.AsArray() ?? new JsonArray();
                images.Add(new JsonObject { ["src"] = variants[0].Path, ["thumbnail"] = variants[2].Path });
                media.ImagesJson = images.ToJsonString();
            }
            await db.SaveChangesAsync(cancellationToken);
            databaseStarted = true;
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

}
