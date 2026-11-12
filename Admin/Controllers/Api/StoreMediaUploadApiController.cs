using FluentFTP;
using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Services;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/media")]
public sealed class StoreMediaUploadApiController(ShopDbContext db, FtpMediaStorage storage, IMediaCdnBaseUrlProvider mediaCdn, ILogger<StoreMediaUploadApiController> logger) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> LibraryFolders = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["slider"] = "sliders", ["testimonial"] = "testimonials", ["trust"] = "trust",
        ["identity"] = "identity", ["payment"] = "payments"
    };

    private static readonly IReadOnlyDictionary<string, (string Folder, uint Size)> ImageFolders =
        new Dictionary<string, (string, uint)>(StringComparer.Ordinal)
        {
            ["brand"] = ("brands", 800), ["slider"] = ("sliders", 1800),
            ["testimonial"] = ("testimonials", 400), ["trust"] = ("trust", 600),
            ["identity"] = ("identity", 800), ["payment"] = ("payments", 800), ["article"] = ("articles", 1600),
            ["country"] = ("countries/flags", 256)
        };

    [HttpGet("library")]
    public async Task<IActionResult> Library(CancellationToken cancellationToken)
    {
        var items = new List<MediaLibraryItem>();
        using var ftp = await storage.ConnectAsync(cancellationToken);
        string cdnBaseUrl = await mediaCdn.GetBaseUrlAsync(cancellationToken);
        foreach ((string kind, string folder) in LibraryFolders)
        {
            var entries = await ftp.GetListing($"/content/{folder}", FtpListOption.Recursive, cancellationToken);
            foreach (var entry in entries.Where(item => item.Type == FtpObjectType.File &&
                         new[] { ".webp", ".jpg", ".jpeg", ".png", ".svg" }.Contains(Path.GetExtension(item.Name), StringComparer.OrdinalIgnoreCase) &&
                         !item.Name.EndsWith("-thumbnail.webp", StringComparison.OrdinalIgnoreCase)))
            {
                string path = entry.FullName.TrimStart('/');
                string thumbnailPath = entry.Name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? path[..^5] + "-thumbnail.webp" : path;
                bool canDelete = TryGetLibraryPaths(path, folder, out _, out _);
                items.Add(new MediaLibraryItem(kind, path, entry.Name, entry.Size, entry.Modified, ContentCdn.ToUrl(path, cdnBaseUrl), ContentCdn.ToUrl(thumbnailPath, cdnBaseUrl), canDelete));
            }
        }
        return Ok(items.OrderByDescending(item => item.ModifiedUtc).ThenBy(item => item.Kind).ToArray());
    }

    [HttpPost("delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLibraryItem([FromForm] string kind, [FromForm] string path, CancellationToken cancellationToken)
    {
        if (!LibraryFolders.TryGetValue(kind ?? "", out string? folder) || !TryGetLibraryPaths(path, folder, out string? original, out string? thumbnail))
            return BadRequest(new { title = "رسانهٔ انتخاب‌شده معتبر نیست." });
        try
        {
            using var ftp = await storage.ConnectAsync(cancellationToken);
            if (!await ftp.FileExists("/" + original, cancellationToken)) return NotFound(new { title = "فایل رسانه پیدا نشد." });
            await ftp.DeleteFile("/" + original, cancellationToken);
            if (await ftp.FileExists("/" + original, cancellationToken)) return Problem(statusCode: 502, title: "فایل اصلی از فضای FTP حذف نشد.");
            if (await ftp.FileExists("/" + thumbnail, cancellationToken))
            {
                await ftp.DeleteFile("/" + thumbnail, cancellationToken);
                if (await ftp.FileExists("/" + thumbnail, cancellationToken)) return Problem(statusCode: 502, title: "فایل اصلی حذف شد، اما تصویر کوچک باقی ماند.");
            }
            return Ok(new { message = "رسانه و تصویر کوچک آن حذف شدند." });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Media library delete failed for {MediaPath}", path);
            return Problem(statusCode: 502, title: "حذف انجام نشد؛ اتصال FTP را بررسی کنید.");
        }
    }

    private static bool TryGetLibraryPaths(string? path, string folder, out string original, out string thumbnail)
    {
        original = thumbnail = "";
        string prefix = $"content/{folder}/";
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(prefix, StringComparison.Ordinal) || path.Contains('\\') || path.Contains("..", StringComparison.Ordinal)) return false;
        string relative = path[prefix.Length..];
        string[] segments = relative.Split('/');
        if (segments.Length is < 1 or > 2 || segments.Any(segment => segment.Length == 0) ||
            (segments.Length == 2 && (folder != "sliders" || segments[0] != "mobile")) ||
            !System.Text.RegularExpressions.Regex.IsMatch(segments[^1], "^[a-f0-9]{32}\\.webp$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return false;
        original = path;
        thumbnail = path[..^5] + "-thumbnail.webp";
        return true;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_000_000)]
    public async Task<IActionResult> Upload([FromForm] string kind, IFormFile file, [FromForm] long? productId, [FromForm] string? title, [FromForm] string? brandKey, [FromForm] string? countryCode, [FromForm] string? articleKey, [FromForm] string? paymentInstitutionCode, CancellationToken cancellationToken)
    {
        bool image = ImageFolders.TryGetValue(kind ?? "", out var imageFolder);
        bool vectorLogo = kind == "payment" && file?.ContentType?.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase) == true;
        bool document = kind == "product-document";
        bool video = kind == "product-video";
        if (!image && !document && !video) return BadRequest(new { title = "نوع رسانه نامعتبر است." });
        countryCode = countryCode?.Trim();
        if (kind == "country")
        {
            if (string.IsNullOrEmpty(countryCode) || countryCode.Length > 8) return BadRequest(new { title = "کشور را انتخاب کنید." });
            var country = await db.GeoCountries.SingleOrDefaultAsync(x => x.CountryCode == countryCode, cancellationToken);
            if (country is null) return NotFound(new { title = "کشور پیدا نشد." });
        }
        else if (!string.IsNullOrEmpty(countryCode)) return BadRequest(new { title = "کشور برای این نوع رسانه معتبر نیست." });
        articleKey = articleKey?.Trim();
        if (kind == "article")
        {
            if (string.IsNullOrEmpty(articleKey) || articleKey.Length > 150) return BadRequest(new { title = "مقاله را انتخاب کنید." });
            var article = await db.MagazineArticles.SingleOrDefaultAsync(x => x.ArticleKey == articleKey, cancellationToken);
            if (article is null) return NotFound(new { title = "مقاله پیدا نشد." });
        }
        else if (!string.IsNullOrEmpty(articleKey)) return BadRequest(new { title = "مقاله برای این نوع رسانه معتبر نیست." });
        brandKey = brandKey?.Trim();
        if (!string.IsNullOrEmpty(brandKey))
        {
            if (kind != "brand" || brandKey.Length > 100) return BadRequest(new { title = "برند نامعتبر است." });
            var brand = await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == brandKey, cancellationToken);
            if (brand is null) return NotFound(new { title = "برند پیدا نشد." });
        }
        paymentInstitutionCode = paymentInstitutionCode?.Trim();
        if (!string.IsNullOrEmpty(paymentInstitutionCode) && (kind != "payment" || paymentInstitutionCode.Length > 40 ||
            !System.Text.RegularExpressions.Regex.IsMatch(paymentInstitutionCode, "^[a-z0-9]+(?:-[a-z0-9]+)*$")))
            return BadRequest(new { title = "شناسهٔ درگاه پرداخت معتبر نیست." });
        if (!string.IsNullOrEmpty(paymentInstitutionCode) && !await db.StoreBanks.AnyAsync(x => x.Code == paymentInstitutionCode, cancellationToken))
            return NotFound(new { title = "درگاه پرداخت پیدا نشد." });
        string? mediaKey = null;
        if (document || video)
        {
            if (productId is null or <= 0) return BadRequest(new { title = "کالا را انتخاب کنید." });
            title = title?.Trim();
            if (document && (string.IsNullOrEmpty(title) || title.Length > 200))
                return BadRequest(new { title = "عنوان فایل باید بین ۱ تا ۲۰۰ نویسه باشد." });
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId && x.IsStoreEnabled, cancellationToken);
            if (product is null) return NotFound(new { title = "کالای فروشگاهی پیدا نشد." });
            mediaKey = "product-" + product.ProductKey;
            if (video)
            {
                var media = await db.StoreProductMedia.AsNoTracking().SingleOrDefaultAsync(x => x.IdProduct == productId && x.MediaKey == mediaKey, cancellationToken);
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
        if ((image && !supportedImage && !vectorLogo) || (document && !header.AsSpan(0, 5).SequenceEqual("%PDF-"u8)) ||
            (video && !header.AsSpan(4, 4).SequenceEqual("ftyp"u8)))
            return BadRequest(new { title = "محتوای فایل با نوع انتخاب‌شده سازگار نیست." });

        stream.Position = 0;
        string name = Guid.NewGuid().ToString("N");
        string folder = image ? imageFolder.Folder : document ? "products/files" : "products/videos";
        byte[]? vectorBytes = vectorLogo ? await SanitizePaymentSvgAsync(stream, cancellationToken) : null;
        if (vectorLogo && vectorBytes is null) return BadRequest(new { title = "فایل SVG لوگو معتبر یا ایمن نیست." });
        string path = !string.IsNullOrEmpty(paymentInstitutionCode)
            ? $"content/payments/{paymentInstitutionCode}-{name}.{(vectorLogo ? "svg" : "webp")}"
            : $"content/{folder}/{name}.{(image ? "webp" : document ? "pdf" : "mp4")}";
        string? thumbnail = image && !vectorLogo ? $"content/{folder}/{name}-thumbnail.webp" : null;
        byte[]? mainBytes = null;
        byte[]? thumbnailBytes = null;
        if (image && !vectorLogo)
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
            FtpStatus result = vectorLogo
                ? await ftp.UploadBytes(vectorBytes!, "/" + path, FtpRemoteExists.Skip, true, null, cancellationToken)
                : image
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
            if (document || video || !string.IsNullOrEmpty(brandKey) || !string.IsNullOrEmpty(paymentInstitutionCode) || kind == "country" || kind == "article")
            {
                if (document)
                    db.StoreProductDocuments.Add(new StoreProductDocument
                    {
                        IdProduct = productId!.Value, Title = title!, Path = path, CreatedAtUtc = DateTime.UtcNow
                    });
                else if (video)
                {
                    var media = await db.StoreProductMedia.SingleOrDefaultAsync(x => x.IdProduct == productId && x.MediaKey == mediaKey, cancellationToken);
                    if (media is null)
                    {
                        await CleanupUploadedAsync(ftp, uploaded);
                        return Conflict(new { title = "رسانهٔ کالا دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                    JsonArray videos;
                    try { videos = string.IsNullOrWhiteSpace(media.VideosJson) ? [] : JsonNode.Parse(media.VideosJson)?.AsArray() ?? []; }
                    catch (JsonException) { throw new InvalidOperationException("Product video list contains invalid JSON."); }
                    videos.Add(path);
                    media.VideosJson = videos.ToJsonString();
                }
                else if (!string.IsNullOrEmpty(paymentInstitutionCode))
                {
                    var institution = await db.StoreBanks.SingleOrDefaultAsync(x => x.Code == paymentInstitutionCode, cancellationToken);
                    if (institution is null)
                    {
                        await CleanupUploadedAsync(ftp, uploaded);
                        return Conflict(new { title = "درگاه دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                    institution.LogoPath = path;
                }
                else if (kind == "country" || kind == "article")
                {
                    bool updated;
                    if (kind == "country")
                    {
                        var country = await db.GeoCountries.SingleOrDefaultAsync(x => x.CountryCode == countryCode, cancellationToken);
                        updated = country is not null;
                        if (country is not null) country.FlagPath = path;
                    }
                    else
                    {
                        var article = await db.MagazineArticles.SingleOrDefaultAsync(x => x.ArticleKey == articleKey, cancellationToken);
                        updated = article is not null;
                        if (article is not null)
                        {
                            article.HeroImagePath = path; article.ContentImagePath = path;
                            article.UpdatedAtUtc = DateTime.UtcNow;
                        }
                    }
                    if (!updated)
                    {
                        await CleanupUploadedAsync(ftp, uploaded);
                        return Conflict(new { title = "رکورد دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                }
                else
                {
                    var brand = await db.StoreBrands.SingleOrDefaultAsync(x => x.BrandKey == brandKey, cancellationToken);
                    if (brand is null)
                    {
                        await CleanupUploadedAsync(ftp, uploaded);
                        return Conflict(new { title = "برند دیگر وجود ندارد؛ دوباره تلاش کنید." });
                    }
                    brand.LogoPath = path;
                }
                await db.SaveChangesAsync(cancellationToken);
                databaseStarted = true;
            }
            string cdnBaseUrl = await mediaCdn.GetBaseUrlAsync(cancellationToken);
            return Ok(new
            {
                message = document || video ? "فایل بارگذاری و به کالا متصل شد." : kind == "country" ? "تصویر کشور ذخیره شد." : kind == "article" ? "تصویر مقاله ذخیره شد." : !string.IsNullOrEmpty(brandKey) ? "نشان برند ذخیره شد." : "فایل بارگذاری شد.",
                path,
                url = ContentCdn.ToUrl(path, cdnBaseUrl),
                thumbnail,
                thumbnailUrl = ContentCdn.ToUrl(thumbnail, cdnBaseUrl)
            });
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

    private async Task CleanupUploadedAsync(AsyncFtpClient ftp, IEnumerable<string> paths)
    {
        foreach (string item in paths)
            try { await ftp.DeleteFile(item, CancellationToken.None); }
            catch (Exception cleanupException) { logger.LogWarning(cleanupException, "Failed to clean up uploaded media"); }
    }

    private static async Task<byte[]?> SanitizePaymentSvgAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_000_000 };
            using var reader = System.Xml.XmlReader.Create(stream, settings);
            var root = await System.Xml.Linq.XDocument.LoadAsync(reader, System.Xml.Linq.LoadOptions.None, cancellationToken);
            XNamespace svg = "http://www.w3.org/2000/svg";
            var allowedElements = new HashSet<string>(StringComparer.Ordinal) { "svg", "g", "path", "circle", "rect", "ellipse", "line", "polyline", "polygon", "defs", "linearGradient", "radialGradient", "stop", "clipPath", "mask", "use" };
            var allowedAttributes = new HashSet<string>(StringComparer.Ordinal) { "xmlns", "viewBox", "width", "height", "fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin", "fill-rule", "fill-opacity", "stroke-opacity", "opacity", "transform", "d", "cx", "cy", "r", "rx", "ry", "x", "y", "x1", "x2", "y1", "y2", "points", "id", "offset", "stop-color", "stop-opacity", "gradientUnits", "gradientTransform", "clip-path", "mask" };
            if (root.Root?.Name != svg + "svg") return null;
            foreach (var element in root.Root.DescendantsAndSelf().ToArray())
            {
                if (element.Name.Namespace != svg || !allowedElements.Contains(element.Name.LocalName)) return null;
                foreach (var attribute in element.Attributes().ToArray())
                {
                    if (attribute.IsNamespaceDeclaration) continue;
                    bool internalXlink = attribute.Name.Namespace == "http://www.w3.org/1999/xlink" && attribute.Name.LocalName == "href" &&
                        System.Text.RegularExpressions.Regex.IsMatch(attribute.Value, "^#[A-Za-z0-9_-]+$");
                    if ((!internalXlink && attribute.Name.Namespace != XNamespace.None) || (!internalXlink && !allowedAttributes.Contains(attribute.Name.LocalName)) ||
                        attribute.Value.Contains("javascript:", StringComparison.OrdinalIgnoreCase) ||
                        (attribute.Value.Contains("url(", StringComparison.OrdinalIgnoreCase) && !System.Text.RegularExpressions.Regex.IsMatch(attribute.Value, "^url\\(#[A-Za-z0-9_-]+\\)$"))) return null;
                }
            }
            return System.Text.Encoding.UTF8.GetBytes(root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting));
        }
        catch (Exception error) when (error is System.Xml.XmlException or InvalidOperationException or ArgumentException) { return null; }
    }

    public sealed record MediaLibraryItem(string Kind, string Path, string Name, long SizeBytes, DateTime ModifiedUtc, string? Url, string? ThumbnailUrl, bool CanDelete);
}
