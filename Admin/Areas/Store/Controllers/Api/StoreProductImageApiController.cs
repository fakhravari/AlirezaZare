using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FluentFTP;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Ftp;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/product-images")]
public sealed class StoreProductImageApiController(
    ShopDbContext db,
    FtpMediaUploadService storage,
    IMediaCdnBaseUrlProvider mediaCdn,
    ILogger<StoreProductImageApiController> logger) : ControllerBase
{
    private const long MaximumFileSize = 10_000_000;
    private const long MaximumRequestSize = 55_000_000;
    private const int MaximumFiles = 10;
    private const string MainFolder = "content/products/images/main/";

    [HttpGet("{productId:long}")]
    public async Task<IActionResult> GetImages(long productId, CancellationToken cancellationToken)
    {
        if (!await db.Products.AnyAsync(x => x.Id == productId, cancellationToken)) return NotFound();

        var media = await GetMediaAsync(productId, cancellationToken);
        var images = ReadGallery(media?.ImagesJson);
        AddImageIfMissing(images, media?.MainPath, media?.ThumbnailPath);
        string cdnBaseUrl = await mediaCdn.GetBaseUrlAsync(cancellationToken);
        var items = images.Select(item =>
        {
            string? path = ReadString(item, "src");
            string? thumbnail = ReadString(item, "thumbnail");

            return new ProductImageItem(path!, ContentCdn.ToUrl(path, cdnBaseUrl), ContentCdn.ToUrl(thumbnail, cdnBaseUrl),
                string.Equals(path, media?.MainPath, StringComparison.OrdinalIgnoreCase));
        }).Where(x => !string.IsNullOrWhiteSpace(x.Path)).ToArray();

        return Ok(new { images = items });
    }

    [HttpPost("{productId:long}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaximumRequestSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestSize)]
    public async Task<IActionResult> Upload(long productId, [FromForm] List<IFormFile> images, CancellationToken cancellationToken)
    {
        if (images is not { Count: > 0 and <= MaximumFiles } || images.Any(image => image.Length is <= 0 or > MaximumFileSize) ||
            images.Sum(image => image.Length) > MaximumRequestSize)
            return BadRequest(new { title = "حداکثر ۱۰ تصویر و هرکدام حداکثر ۱۰ مگابایت انتخاب کنید." });

        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId, cancellationToken);
        if (product is null) return NotFound();

        var preparedImages = new List<PreparedImage>(images.Count);
        try
        {
            foreach (IFormFile image in images)
                preparedImages.Add(await PrepareImageAsync(image, productId, cancellationToken));
        }
        catch (InvalidDataException exception)
        {
            return BadRequest(new { title = exception.Message });
        }
        catch (Exception exception) when (exception is MagickException or NotSupportedException)
        {
            return BadRequest(new { title = "یکی از فایل‌های انتخاب‌شده تصویر معتبر و پشتیبانی‌شده نیست." });
        }

        AsyncFtpClient? ftp = null;
        var uploadedPaths = new List<string>();
        bool databaseSaved = false;
        try
        {
            ftp = await storage.ConnectAsync(cancellationToken);
            foreach (PreparedImage image in preparedImages)
            foreach (var variant in image.Variants)
            {
                string remotePath = "/" + variant.Path;
                var result = await ftp.UploadBytes(variant.Bytes, remotePath, FtpRemoteExists.Skip, true, null, cancellationToken);
                if (result != FtpStatus.Success) throw new IOException("Image upload failed.");
                uploadedPaths.Add(remotePath);
            }

            string mediaKey = "product-" + product.ProductKey;
            var media = await db.StoreProductMedia.SingleOrDefaultAsync( x => x.IdProduct == productId && x.MediaKey == mediaKey, cancellationToken);
            var gallery = ReadGallery(media?.ImagesJson);
            AddImageIfMissing(gallery, media?.MainPath, media?.ThumbnailPath);

            if (media is null)
            {
                media = new StoreProductMedia { IdProduct = productId, MediaKey = mediaKey, VideosJson = "[]" };
                db.StoreProductMedia.Add(media);
            }

            foreach (PreparedImage image in preparedImages)
                gallery.Add(new JsonObject { ["src"] = image.MainPath, ["thumbnail"] = image.ThumbnailPath });

            if (string.IsNullOrWhiteSpace(media.MainPath))
            {
                string firstPath = ReadString(gallery.FirstOrDefault(), "src") ?? preparedImages[0].MainPath;
                string firstThumbnail = ReadString(gallery.FirstOrDefault(), "thumbnail") ?? GetVariantPath(firstPath, "thumbnail");
                SetDefaultImage(media, firstPath, firstThumbnail);
            }

            media.ImagesJson = gallery.ToJsonString();
            await db.SaveChangesAsync(cancellationToken);

            databaseSaved = true;

            return Ok(new { message = "تصاویر کالا بارگذاری و ثبت شدند." });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Product image upload failed for product {ProductId}", productId);
            if (!databaseSaved && ftp is not null)
                foreach (string path in uploadedPaths)
                    try { await ftp.DeleteFile(path, CancellationToken.None); }
                    catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded image"); }
            if (exception is OperationCanceledException) throw;

            return Problem(statusCode: 502, title: "بارگذاری انجام نشد؛ اتصال FTP و تنظیمات سرور را بررسی کنید.");
        }
        finally
        {
            ftp?.Dispose();
        }
    }

    [HttpPut("{productId:long}/default")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(long productId, [FromBody] ProductImageSelection input, CancellationToken cancellationToken)
    {
        var media = await GetMediaAsync(productId, cancellationToken);
        if (media is null) return NotFound();

        var gallery = ReadGallery(media.ImagesJson);
        JsonNode? selected = gallery.FirstOrDefault(item => string.Equals(ReadString(item, "src"), input.Path, StringComparison.OrdinalIgnoreCase));
        if (selected is null || !IsValidImagePath(input.Path, productId))
            return BadRequest(new { title = "تصویر انتخاب‌شده در گالری کالا وجود ندارد." });

        SetDefaultImage(media, input.Path, ReadString(selected, "thumbnail") ?? GetVariantPath(input.Path, "thumbnail"));
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "تصویر پیش‌فرض کالا تغییر کرد." });
    }

    [HttpDelete("{productId:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long productId, [FromQuery] string path, CancellationToken cancellationToken)
    {
        var media = await GetMediaAsync(productId, cancellationToken);
        if (media is null) return NotFound();

        if (string.Equals(media.MainPath, path, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { title = "تصویر پیش‌فرض قابل حذف نیست؛ ابتدا تصویر دیگری را پیش‌فرض کنید." });

        var gallery = ReadGallery(media.ImagesJson);
        int index = -1;
        for (int itemIndex = 0; itemIndex < gallery.Count; itemIndex++)
            if (string.Equals(ReadString(gallery[itemIndex], "src"), path, StringComparison.OrdinalIgnoreCase))
            {
                index = itemIndex;
                break;
            }

        if (index < 0 || !IsValidImagePath(path, productId)) return NotFound();

        try
        {
            using AsyncFtpClient ftp = await storage.ConnectAsync(cancellationToken);
            foreach (string variantPath in GetVariantPaths(path))
            {
                string remotePath = "/" + variantPath;
                if (await ftp.FileExists(remotePath, cancellationToken))
                    await ftp.DeleteFile(remotePath, cancellationToken);
                if (await ftp.FileExists(remotePath, cancellationToken))
                    throw new IOException($"FTP image file remains after deletion: {variantPath}");
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Product image files could not be deleted for product {ProductId}", productId);

            return Problem(statusCode: 502, title: "تصویر از FTP حذف نشد؛ رکورد گالری حفظ شد. اتصال FTP را بررسی کنید.");
        }

        gallery.RemoveAt(index);
        media.ImagesJson = gallery.ToJsonString();
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "تصویر از گالری کالا حذف شد." });
    }

    private async Task<StoreProductMedia?> GetMediaAsync(long productId, CancellationToken cancellationToken)
    {
        string? productKey = await db.Products.AsNoTracking().Where(x => x.Id == productId).Select(x => x.ProductKey).SingleOrDefaultAsync(cancellationToken);
        if (productKey is null) return null;

        return await db.StoreProductMedia.SingleOrDefaultAsync( x => x.IdProduct == productId && x.MediaKey == $"product-{productKey}", cancellationToken);
    }

    private static async Task<PreparedImage> PrepareImageAsync(IFormFile image, long productId, CancellationToken cancellationToken)
    {
        await using var stream = image.OpenReadStream();
        byte[] header = new byte[12];
        if (await stream.ReadAsync(header, cancellationToken) < header.Length ||
            !(header.AsSpan(0, 3).SequenceEqual(new byte[] { 0xff, 0xd8, 0xff }) ||
              header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
              (header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8))))
            throw new InvalidDataException("فقط تصویر JPEG، PNG یا WebP مجاز است.");

        stream.Position = 0;
        var info = new MagickImageInfo(stream);
        if ((long)info.Width * info.Height > 40_000_000)
            throw new InvalidDataException("ابعاد یکی از تصاویر بیش از حد مجاز است.");

        stream.Position = 0;
        using var source = new MagickImage(stream);
        source.AutoOrient();
        string filename = $"product-{productId}-{Guid.NewGuid():N}.webp";
        var variants = new List<ImageVariant>(3);
        foreach (var variant in new[] { (Folder: "main", Size: 1600), (Folder: "card", Size: 800), (Folder: "thumbnail", Size: 256) })
        {
            using var resized = source.Clone();
            resized.Resize(new MagickGeometry((uint)variant.Size, (uint)variant.Size) { Greater = true });
            resized.Strip();
            resized.Quality = 85;
            variants.Add(new ImageVariant($"content/products/images/{variant.Folder}/{filename}", resized.ToByteArray(MagickFormat.WebP)));
        }

        return new PreparedImage(variants[0].Path, variants[2].Path, variants);
    }

    private static JsonArray ReadGallery(string? json)
    {
        try { return JsonNode.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json) as JsonArray ?? new JsonArray(); }
        catch (JsonException) { return new JsonArray(); }
    }

    private static void AddImageIfMissing(JsonArray gallery, string? mainPath, string? thumbnailPath)
    {
        if (string.IsNullOrWhiteSpace(mainPath) || gallery.Any(item => string.Equals(ReadString(item, "src"), mainPath, StringComparison.OrdinalIgnoreCase))) return;

        gallery.Insert(0, new JsonObject { ["src"] = mainPath, ["thumbnail"] = thumbnailPath });
    }

    private static string? ReadString(JsonNode? item, string property) =>
        item is JsonObject obj && obj[property] is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    private static void SetDefaultImage(StoreProductMedia media, string mainPath, string thumbnailPath)
    {
        media.MainPath = mainPath;
        media.CardPath = GetVariantPath(mainPath, "card");
        media.ThumbnailPath = thumbnailPath;
    }

    private static string GetVariantPath(string mainPath, string variant) =>
        mainPath.Replace(MainFolder, $"content/products/images/{variant}/", StringComparison.Ordinal);

    private static string[] GetVariantPaths(string mainPath) =>
        [mainPath, GetVariantPath(mainPath, "card"), GetVariantPath(mainPath, "thumbnail")];

    private static bool IsValidImagePath(string? path, long productId) =>
        !string.IsNullOrWhiteSpace(path) && path.StartsWith(MainFolder, StringComparison.Ordinal) &&
        Regex.IsMatch(path, $"^content/products/images/main/product-{productId}-[a-f0-9]{{32}}\\.webp$", RegexOptions.CultureInvariant);

    public sealed record ProductImageSelection(string Path);
    public sealed record ProductImageItem(string Path, string? Url, string? ThumbnailUrl, bool IsDefault);
    private sealed record ImageVariant(string Path, byte[] Bytes);
    private sealed record PreparedImage(string MainPath, string ThumbnailPath, IReadOnlyList<ImageVariant> Variants);
}
