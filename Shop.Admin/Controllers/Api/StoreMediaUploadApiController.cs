using FluentFTP;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Services;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/media")]
public sealed class StoreMediaUploadApiController(ShopDbContext db, FtpMediaStorage storage, ILogger<StoreMediaUploadApiController> logger) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, (string Folder, uint Size)> ImageFolders =
        new Dictionary<string, (string, uint)>(StringComparer.Ordinal)
        {
            ["brand"] = ("brands", 800), ["slider"] = ("sliders", 1800),
            ["testimonial"] = ("testimonials", 400), ["trust"] = ("trust", 600),
            ["identity"] = ("identity", 800), ["payment"] = ("payments", 800), ["article"] = ("articles", 1600),
            ["country"] = ("countries/flags", 256)
        };

    [HttpGet("articles")]
    public async Task<IActionResult> Articles(CancellationToken cancellationToken) =>
        Ok(await db.Database.SqlQueryRaw<ArticleRow>("SELECT [ArticleKey], [Title] FROM [Mag].[MagazineArticle] ORDER BY [Title], [Id]").ToListAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_000_000)]
    public async Task<IActionResult> Upload([FromForm] string kind, IFormFile file, [FromForm] long? productId, [FromForm] string? title, [FromForm] string? brandKey, [FromForm] string? countryCode, [FromForm] string? articleKey, CancellationToken cancellationToken)
    {
        bool image = ImageFolders.TryGetValue(kind ?? "", out var imageFolder);
        bool document = kind == "product-document";
        bool video = kind == "product-video";
        if (!image && !document && !video) return BadRequest(new { title = "نوع رسانه نامعتبر است." });
        countryCode = countryCode?.Trim();
        if (kind == "country")
        {
            if (string.IsNullOrEmpty(countryCode) || countryCode.Length > 8) return BadRequest(new { title = "کشور را انتخاب کنید." });
            var country = await db.Database.SqlQueryRaw<MediaRow>("SELECT [Id] FROM [Geo].[Country] WHERE [CountryCode] = {0}", countryCode).SingleOrDefaultAsync(cancellationToken);
            if (country is null) return NotFound(new { title = "کشور پیدا نشد." });
        }
        else if (!string.IsNullOrEmpty(countryCode)) return BadRequest(new { title = "کشور برای این نوع رسانه معتبر نیست." });
        articleKey = articleKey?.Trim();
        if (kind == "article")
        {
            if (string.IsNullOrEmpty(articleKey) || articleKey.Length > 150) return BadRequest(new { title = "مقاله را انتخاب کنید." });
            var article = await db.Database.SqlQueryRaw<MediaRow>("SELECT [Id] FROM [Mag].[MagazineArticle] WHERE [ArticleKey] = {0}", articleKey).SingleOrDefaultAsync(cancellationToken);
            if (article is null) return NotFound(new { title = "مقاله پیدا نشد." });
        }
        else if (!string.IsNullOrEmpty(articleKey)) return BadRequest(new { title = "مقاله برای این نوع رسانه معتبر نیست." });
        brandKey = brandKey?.Trim();
        if (!string.IsNullOrEmpty(brandKey))
        {
            if (kind != "brand" || brandKey.Length > 100) return BadRequest(new { title = "برند نامعتبر است." });
            var brand = await db.Database.SqlQueryRaw<MediaRow>("SELECT [Id] FROM [Store].[Brand] WHERE [BrandKey] = {0}", brandKey).SingleOrDefaultAsync(cancellationToken);
            if (brand is null) return NotFound(new { title = "برند پیدا نشد." });
        }
        string? mediaKey = null;
        if (document || video)
        {
            if (productId is null or <= 0) return BadRequest(new { title = "کالا را انتخاب کنید." });
            title = title?.Trim();
            if (document && (string.IsNullOrEmpty(title) || title.Length > 200))
                return BadRequest(new { title = "عنوان فایل باید بین ۱ تا ۲۰۰ نویسه باشد." });
            var product = await db.Database.SqlQueryRaw<ProductRow>("SELECT [ProductKey] FROM [Warehouse].[Product] WHERE [Id] = {0} AND [IsStoreEnabled] = 1", productId).SingleOrDefaultAsync(cancellationToken);
            if (product is null) return NotFound(new { title = "کالای فروشگاهی پیدا نشد." });
            mediaKey = "product-" + product.ProductKey;
            if (video)
            {
                var media = await db.Database.SqlQueryRaw<MediaRow>("SELECT [Id] FROM [Store].[ProductMedia] WHERE [IdProduct] = {0} AND [MediaKey] = {1}", productId, mediaKey).SingleOrDefaultAsync(cancellationToken);
                if (media is null) return Conflict(new { title = "ابتدا تصویر اصلی کالا را ثبت کنید." });
            }
        }
        long maxBytes = video ? 50_000_000 : 10_000_000;
        if (file is null || file.Length <= 0 || file.Length > maxBytes)
            return BadRequest(new { title = video ? "حجم ویدئو باید حداکثر ۵۰ مگابایت باشد." : "حجم فایل باید حداکثر ۱۰ مگابایت باشد." });

        await using var stream = file.OpenReadStream();
        byte[] header = new byte[12];
        int count = await stream.ReadAtLeastAsync(header, header.Length, false, cancellationToken);
        if (count < header.Length) return BadRequest(new { title = "فایل معتبر نیست." });
        bool supportedImage = header.AsSpan(0, 3).SequenceEqual(new byte[] { 0xff, 0xd8, 0xff }) ||
            header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
            (header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8));
        if ((image && !supportedImage) || (document && !header.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) ||
            (video && !header.AsSpan(4, 4).SequenceEqual("ftyp"u8)))
            return BadRequest(new { title = "محتوای فایل با نوع انتخاب‌شده سازگار نیست." });

        stream.Position = 0;
        string name = Guid.NewGuid().ToString("N");
        string folder = image ? imageFolder.Folder : document ? "products/files" : "products/videos";
        string path = $"content/{folder}/{name}.{(image ? "webp" : document ? "pdf" : "mp4")}";
        string? thumbnail = image ? $"content/{folder}/{name}-thumbnail.webp" : null;
        byte[]? mainBytes = null;
        byte[]? thumbnailBytes = null;
        if (image)
        {
            try
            {
                var info = new MagickImageInfo(stream);
                if ((long)info.Width * info.Height > 40_000_000)
                    return BadRequest(new { title = "ابعاد تصویر بیش از حد مجاز است." });
                stream.Position = 0;
                using var source = new MagickImage(stream);
                source.AutoOrient();
                using var main = source.Clone();
                main.Resize(new MagickGeometry(imageFolder.Size, imageFolder.Size) { Greater = true });
                main.Strip(); main.Quality = 85;
                mainBytes = main.ToByteArray(MagickFormat.WebP);
                using var thumb = source.Clone();
                thumb.Resize(new MagickGeometry(256, 256) { Greater = true });
                thumb.Strip(); thumb.Quality = 85;
                thumbnailBytes = thumb.ToByteArray(MagickFormat.WebP);
            }
            catch (MagickException) { return BadRequest(new { title = "تصویر معتبر نیست." }); }
        }

        AsyncFtpClient? ftp = null;
        var uploaded = new List<string>();
        bool databaseStarted = false;
        try
        {
            ftp = await storage.ConnectAsync(cancellationToken);
            FtpStatus result = image
                ? await ftp.UploadBytes(mainBytes!, "/" + path, FtpRemoteExists.Skip, true, null, cancellationToken)
                : await ftp.UploadStream(stream, "/" + path, FtpRemoteExists.Skip, true, null, cancellationToken);
            if (result != FtpStatus.Success) throw new IOException("Media upload failed.");
            uploaded.Add("/" + path);
            if (thumbnail is not null)
            {
                result = await ftp.UploadBytes(thumbnailBytes!, "/" + thumbnail, FtpRemoteExists.Skip, true, null, cancellationToken);
                if (result != FtpStatus.Success) throw new IOException("Thumbnail upload failed.");
                uploaded.Add("/" + thumbnail);
            }
            if (document || video || !string.IsNullOrEmpty(brandKey) || kind == "country" || kind == "article")
            {
                databaseStarted = true;
                if (document)
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [Store].[ProductDocument] ([IdProduct], [Title], [Path]) VALUES ({productId}, {title}, {path})", cancellationToken);
                else if (video)
                {
                    int updated = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[ProductMedia] SET [VideosJson] = JSON_MODIFY(COALESCE([VideosJson], N'[]'), 'append $', {path}) WHERE [IdProduct] = {productId} AND [MediaKey] = {mediaKey}", cancellationToken);
                    if (updated == 0)
                    {
                        try { await ftp.DeleteFile("/" + path, CancellationToken.None); }
                        catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded media"); }
                        return Conflict(new { title = "رسانهٔ کالا دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                }
                else if (kind == "country" || kind == "article")
                {
                    int updated = kind == "country"
                        ? await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Geo].[Country] SET [FlagPath] = {path} WHERE [CountryCode] = {countryCode}", cancellationToken)
                        : await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Mag].[MagazineArticle] SET [HeroImagePath] = {path}, [ContentImagePath] = {path}, [UpdatedAtUtc] = SYSUTCDATETIME() WHERE [ArticleKey] = {articleKey}", cancellationToken);
                    if (updated == 0)
                    {
                        foreach (string item in uploaded)
                            try { await ftp.DeleteFile(item, CancellationToken.None); }
                            catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded media"); }
                        return Conflict(new { title = "رکورد دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                }
                else
                {
                    int updated = await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Store].[Brand] SET [LogoPath] = {path} WHERE [BrandKey] = {brandKey}", cancellationToken);
                    if (updated == 0)
                    {
                        foreach (string item in uploaded)
                            try { await ftp.DeleteFile(item, CancellationToken.None); }
                            catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded media"); }
                        return Conflict(new { title = "برند دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                }
            }
            return Ok(new { message = document || video ? "فایل بارگذاری و به کالا متصل شد." : kind == "country" ? "تصویر کشور ذخیره شد." : kind == "article" ? "تصویر مقاله ذخیره شد." : !string.IsNullOrEmpty(brandKey) ? "نشان برند ذخیره شد." : "فایل بارگذاری شد.", path, thumbnail });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Media upload failed for {MediaKind}", kind);
            if (!databaseStarted && ftp is not null)
                foreach (string item in uploaded)
                    try { await ftp.DeleteFile(item, CancellationToken.None); } catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded media"); }
            if (exception is OperationCanceledException) throw;
            return Problem(statusCode: 502, title: "بارگذاری انجام نشد؛ تنظیمات FTP و اتصال را بررسی کنید.");
        }
        finally { ftp?.Dispose(); }
    }

    public sealed class ProductRow { public string ProductKey { get; set; } = ""; }
    public sealed class MediaRow { public long Id { get; set; } }
    public sealed class ArticleRow { public string ArticleKey { get; set; } = ""; public string Title { get; set; } = ""; }
}
