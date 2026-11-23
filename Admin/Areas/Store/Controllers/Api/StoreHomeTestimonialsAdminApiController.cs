using FluentFTP;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Helpers;
using Shop.Data.Infrastructure.Ftp;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/home-testimonials")]
public sealed class StoreHomeTestimonialsAdminApiController(
    ShopDbContext db,
    FtpMediaUploadService storage,
    IMediaCdnBaseUrlProvider mediaCdn,
    ILogger<StoreHomeTestimonialsAdminApiController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken = default)
    {
        var query = from feedback in db.ProductFeedback.AsNoTracking()
                    join product in db.Products.AsNoTracking() on feedback.IdProduct equals product.Id
                    join featured in db.StoreHomeTestimonials.AsNoTracking() on feedback.Id equals featured.IdProductFeedback into featuredRows
                    from featured in featuredRows.DefaultIfEmpty()
                    where feedback.FeedbackType == "reviews" && feedback.Status == "approved"
                    select new
                    {
                        feedback.Id,
                        feedback.IdProduct,
                        feedback.DisplayName,
                        feedback.FeedbackText,
                        feedback.Rating,
                        feedback.IsSample,
                        feedback.CreatedAtUtc,
                        ProductName = product.Name,
                        ProductKey = product.ProductKey,
                        IsActive = featured != null && featured.IsActive,
                        SortOrder = featured == null ? 0 : featured.SortOrder,
                        ImagePath = featured == null ? null : featured.ImagePath
                    };
        var items = await query.OrderByDescending(x => x.IsActive).ThenBy(x => x.SortOrder).ThenByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
        string cdnBaseUrl = await mediaCdn.GetBaseUrlAsync(cancellationToken);
        var result = items.Select(item => new
        {
            item.Id, item.IdProduct, item.DisplayName, item.FeedbackText, item.Rating, item.IsSample, item.CreatedAtUtc,
            item.ProductName, item.ProductKey, item.IsActive, item.SortOrder, item.ImagePath,
            ImageUrl = string.IsNullOrWhiteSpace(item.ImagePath) ? null : ContentCdn.ToUrl(item.ImagePath, cdnBaseUrl)
        }).ToArray();

        return Ok(new { items = result });
    }

    [HttpGet("products")]
    [Authorize(Roles = "system.full-access")]
    public async Task<IActionResult> Products(CancellationToken cancellationToken) => Ok(await db.Products.AsNoTracking()
        .OrderBy(x => x.Name).ThenBy(x => x.Id)
        .Select(x => new { x.Id, x.Name, x.ProductKey, x.IsStoreEnabled }).ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "system.full-access")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] HomeTestimonialReviewInput input, CancellationToken cancellationToken)
    {
        var error = await ValidateReviewAsync(input, cancellationToken);
        if (error is not null) return BadRequest(new { title = error });
        var feedback = new ProductFeedback { IdProduct = input.ProductId, FeedbackType = "reviews", DisplayName = input.DisplayName.Trim(),
            FeedbackText = input.FeedbackText.Trim(), Rating = (byte)input.Rating, IsSample = true, Status = "approved", CreatedAtUtc = DateTime.UtcNow };
        db.ProductFeedback.Add(feedback);
        db.StoreHomeTestimonials.Add(new StoreHomeTestimonial { Feedback = feedback, IsActive = input.IsActive, SortOrder = input.SortOrder, UpdatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { id = feedback.Id, message = "دیدگاه نمونه ثبت شد." });
    }

    [HttpPut("{id:long}/review")]
    [Authorize(Roles = "system.full-access")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReview(long id, [FromBody] HomeTestimonialReviewInput input, CancellationToken cancellationToken)
    {
        var error = await ValidateReviewAsync(input, cancellationToken);
        if (error is not null) return BadRequest(new { title = error });
        var feedback = await db.ProductFeedback.SingleOrDefaultAsync(x => x.Id == id && x.FeedbackType == "reviews" && x.Status == "approved", cancellationToken);
        if (feedback is null) return NotFound();
        feedback.IdProduct = input.ProductId;
        feedback.DisplayName = input.DisplayName.Trim();
        feedback.FeedbackText = input.FeedbackText.Trim();
        feedback.Rating = (byte)input.Rating;
        var item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.IdProductFeedback == id, cancellationToken);
        if (item is null) { item = new StoreHomeTestimonial { IdProductFeedback = id }; db.StoreHomeTestimonials.Add(item); }
        item.IsActive = input.IsActive;
        item.SortOrder = input.SortOrder;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "دیدگاه و تنظیمات نمایش ذخیره شد." });
    }

    private async Task<string?> ValidateReviewAsync(HomeTestimonialReviewInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.DisplayName) || input.DisplayName.Trim().Length > 80 ||
            string.IsNullOrWhiteSpace(input.FeedbackText) || input.FeedbackText.Trim().Length > 800 ||
            input.Rating is < 1 or > 5 || input.SortOrder is < 0 or > 9999)
            return "نام، متن، امتیاز یا ترتیب معتبر نیست.";
        return await db.Products.AnyAsync(x => x.Id == input.ProductId, cancellationToken) ? null : "کالا پیدا نشد.";
    }

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(long id, [FromBody] HomeTestimonialInput input, CancellationToken cancellationToken)
    {
        if (input.SortOrder is < 0 or > 9999) return BadRequest(new { title = "ترتیب باید بین صفر و ۹۹۹۹ باشد." });

        if (!await db.ProductFeedback.AnyAsync(x => x.Id == id && x.FeedbackType == "reviews" && x.Status == "approved", cancellationToken))
            return NotFound(new { title = "فقط دیدگاه تأییدشده قابل نمایش در صفحهٔ نخست است." });

        StoreHomeTestimonial? item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.IdProductFeedback == id, cancellationToken);
        if (item is null)
        {
            item = new StoreHomeTestimonial { IdProductFeedback = id };
            db.StoreHomeTestimonials.Add(item);
        }
        item.IsActive = input.IsActive;
        item.SortOrder = input.SortOrder;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "تنظیم نمایش دیدگاه ذخیره شد." });
    }

    [HttpPost("{id:long}/image")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> UploadImage(long id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is <= 0 or > 10_000_000)
            return BadRequest(new { title = "تصویر باید حداکثر ۱۰ مگابایت باشد." });

        if (!await db.ProductFeedback.AnyAsync(x => x.Id == id && x.FeedbackType == "reviews" && x.Status == "approved", cancellationToken))
            return NotFound(new { title = "دیدگاه تأییدشده پیدا نشد." });

        byte[] sourceBytes;
        await using (var input = new MemoryStream())
        {
            await file.CopyToAsync(input, cancellationToken);
            sourceBytes = input.ToArray();
        }
        bool supportedImage = sourceBytes.Length >= 3 && sourceBytes[0] == 0xff && sourceBytes[1] == 0xd8 && sourceBytes[2] == 0xff
            || sourceBytes.Length >= 8 && sourceBytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            || sourceBytes.Length >= 12 && sourceBytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && sourceBytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        if (!supportedImage) return BadRequest(new { title = "فقط تصویر JPEG، PNG یا WebP پذیرفته می‌شود." });

        byte[] mainBytes;
        byte[] thumbBytes;
        try
        {
            using var source = new MagickImage(sourceBytes);
            if ((long)source.Width * source.Height > 40_000_000) return BadRequest(new { title = "ابعاد تصویر بیش از حد مجاز است." });

            source.AutoOrient();
            using var main = source.Clone();
            main.Resize(new MagickGeometry(600, 600) { Greater = true });
            main.Strip(); main.Quality = 85;
            mainBytes = main.ToByteArray(MagickFormat.WebP);
            using var thumbnail = source.Clone();
            thumbnail.Resize(new MagickGeometry(256, 256) { Greater = true });
            thumbnail.Strip(); thumbnail.Quality = 82;
            thumbBytes = thumbnail.ToByteArray(MagickFormat.WebP);
        }
        catch (MagickException) { return BadRequest(new { title = "فایل تصویر معتبر نیست." }); }

        string basePath = $"content/testimonials/{Guid.NewGuid():N}";
        string imagePath = basePath + ".webp";
        string thumbnailPath = basePath + "-thumbnail.webp";
        AsyncFtpClient? ftp = null;
        try
        {
            ftp = await storage.ConnectAsync(cancellationToken);
            if (await ftp.UploadBytes(mainBytes, "/" + imagePath, FtpRemoteExists.Skip, true, null, cancellationToken) != FtpStatus.Success
                || await ftp.UploadBytes(thumbBytes, "/" + thumbnailPath, FtpRemoteExists.Skip, true, null, cancellationToken) != FtpStatus.Success)
                throw new IOException("Testimonial image upload failed.");

            StoreHomeTestimonial? item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.IdProductFeedback == id, cancellationToken);
            if (item is null)
            {
                item = new StoreHomeTestimonial { IdProductFeedback = id, IsActive = false };
                db.StoreHomeTestimonials.Add(item);
            }
            string? previousPath = item.ImagePath;
            item.ImagePath = imagePath;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            try { await DeleteStoredImageAsync(ftp, previousPath, cancellationToken); }
            catch (Exception exception) when (exception is not OperationCanceledException)
            { logger.LogWarning(exception, "Previous home testimonial image could not be removed for review {ReviewId}", id); }
            string baseUrl = await mediaCdn.GetBaseUrlAsync(cancellationToken);

            return Ok(new { message = "تصویر دیدگاه بارگذاری شد.", imagePath, imageUrl = ContentCdn.ToUrl(imagePath, baseUrl) });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Home testimonial image upload failed for review {ReviewId}", id);
            if (ftp is not null)
            {
                await DeleteStoredImageAsync(ftp, imagePath, cancellationToken);
                await DeleteStoredImageAsync(ftp, thumbnailPath, cancellationToken);
            }

            return Problem(statusCode: 502, title: "بارگذاری تصویر انجام نشد؛ اتصال FTP را بررسی کنید.");
        }
        finally { ftp?.Dispose(); }
    }

    [HttpDelete("{id:long}/image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(long id, CancellationToken cancellationToken)
    {
        StoreHomeTestimonial? item = await db.StoreHomeTestimonials.SingleOrDefaultAsync(x => x.IdProductFeedback == id, cancellationToken);
        if (item is null || string.IsNullOrWhiteSpace(item.ImagePath)) return NotFound();

        try
        {
            using AsyncFtpClient ftp = await storage.ConnectAsync(cancellationToken);
            await DeleteStoredImageAsync(ftp, item.ImagePath, cancellationToken);
            item.ImagePath = null;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "تصویر دیدگاه حذف شد." });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Home testimonial image delete failed for review {ReviewId}", id);

            return Problem(statusCode: 502, title: "حذف تصویر انجام نشد؛ اتصال FTP را بررسی کنید.");
        }
    }

    private static async Task DeleteStoredImageAsync(AsyncFtpClient ftp, string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("content/testimonials/", StringComparison.Ordinal)
            || path.Contains("..", StringComparison.Ordinal) || path.Contains('\\')) return;

        string thumb = path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? path[..^5] + "-thumbnail.webp" : "";
        if (await ftp.FileExists("/" + path, cancellationToken)) await ftp.DeleteFile("/" + path, cancellationToken);
        if (thumb.Length > 0 && await ftp.FileExists("/" + thumb, cancellationToken)) await ftp.DeleteFile("/" + thumb, cancellationToken);
    }
}

public sealed record HomeTestimonialInput(bool IsActive, int SortOrder);
public sealed record HomeTestimonialReviewInput(long ProductId, string DisplayName, string FeedbackText, int Rating, bool IsActive, int SortOrder);
