using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Store;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/feedback")]
public sealed class StoreFeedbackApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet("reviews")]
    public Task<IActionResult> Reviews([FromQuery] int? statusId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default) =>
        ListAsync("reviews", statusId, search, page, pageSize, from, to, cancellationToken);

    [HttpGet("questions")]
    public Task<IActionResult> Questions([FromQuery] int? statusId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default) =>
        ListAsync("questions", statusId, search, page, pageSize, from, to, cancellationToken);

    [HttpPut("reviews/{id:long}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ModerateReview(long id, [FromBody] FeedbackModerationInput input,
        CancellationToken cancellationToken) =>
        ModerateAsync(id, "reviews", input, cancellationToken);

    [HttpPut("questions/{id:long}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ModerateQuestion(long id, [FromBody] FeedbackModerationInput input,
        CancellationToken cancellationToken) =>
        ModerateAsync(id, "questions", input, cancellationToken);

    private async Task<IActionResult> ListAsync(string type, int? statusId, string? search, int page, int pageSize,
        DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, search, out AdminPageRequest paging)
            || (statusId.HasValue &&
                !await db.ProductFeedbackStatuses.AnyAsync(x => x.Id == statusId, cancellationToken)))
            return ValidationProblem(detail: "فیلتر یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return ValidationProblem(detail: "تاریخ شروع باید قبل از تاریخ پایان باشد.", statusCode: 400);

        if (to == DateOnly.MaxValue)
            return ValidationProblem(detail: "تاریخ پایان معتبر نیست.", statusCode: 400);

        if (from.HasValue && to.HasValue && to.Value.DayNumber - from.Value.DayNumber > 365)
            return ValidationProblem(detail: "بازهٔ تاریخ نمی‌تواند بیشتر از ۳۶۶ روز باشد.", statusCode: 400);

        DateTime? fromUtc = from.HasValue
            ? DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
        DateTime? untilUtc = to.HasValue
            ? DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
        var query = db.ProductFeedback
            .AsNoTracking()
            .Join(
                db.Products.AsNoTracking(),
                feedback => feedback.IdProduct,
                product => product.Id,
                (feedback, product) => new { feedback, product })
            .Where(feedbackProduct => feedbackProduct.feedback.FeedbackType == type)
            .Select(feedbackProduct => new
            {
                feedbackProduct.feedback.Id,
                feedbackProduct.feedback.IdProduct,
                StoreProductKey = feedbackProduct.product.ProductKey,
                ProductName = feedbackProduct.product.Name,
                feedbackProduct.feedback.DisplayName,
                feedbackProduct.feedback.FeedbackText,
                feedbackProduct.feedback.Rating,
                feedbackProduct.feedback.IdStatus,
                StatusName = feedbackProduct.feedback.Status.Name,
                feedbackProduct.feedback.AdminReply,
                feedbackProduct.feedback.CreatedAtUtc,
                feedbackProduct.feedback.RepliedAtUtc
            });
        if (statusId.HasValue) query = query.Where(x => x.IdStatus == statusId.Value);
        if (fromUtc.HasValue) query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
        if (untilUtc.HasValue) query = query.Where(x => x.CreatedAtUtc < untilUtc.Value);
        if (paging.Search.Length > 0)
        {
            string term = paging.Search;
            query = query.Where(x =>
                x.StoreProductKey.Contains(term) || x.ProductName.Contains(term) || x.DisplayName.Contains(term) ||
                x.FeedbackText.Contains(term));
        }

        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip(paging.Offset)
            .Take(paging.PageSize).ToListAsync(cancellationToken);
        var result = items.Select(item => new
        {
            item.Id,
            item.IdProduct,
            item.StoreProductKey,
            item.ProductName,
            item.DisplayName,
            item.FeedbackText,
            item.Rating,
            item.IdStatus,
            item.StatusName,
            AdminReply = item.AdminReply is null ? null : StoreHtmlSanitizer.Sanitize(item.AdminReply),
            item.CreatedAtUtc,
            item.RepliedAtUtc
        }).ToArray();

        var paged = AdminListQuery.CreateResult(result, total, paging);
        var statuses = await db.ProductFeedbackStatuses.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Name, x.NameEn }).ToListAsync(cancellationToken);
        return Ok(new { paged.Items, paged.Total, paged.Page, paged.PageSize, paged.HasNext, Statuses = statuses });
    }

    private async Task<IActionResult> ModerateAsync(long id, string type, FeedbackModerationInput input,
        CancellationToken cancellationToken)
    {
        if ((input.AdminReply?.Length ?? 0) > 1000 ||
            !await db.ProductFeedbackStatuses.AnyAsync(x => x.Id == input.StatusId, cancellationToken))
            return ValidationProblem(detail: "وضعیت یا متن پاسخ معتبر نیست.", statusCode: 400);

        ProductFeedback? feedback =
            await db.ProductFeedback.SingleOrDefaultAsync(x => x.Id == id && x.FeedbackType == type, cancellationToken);
        if (feedback is null) return NotFound();

        feedback.IdStatus = input.StatusId;
        feedback.AdminReply = string.IsNullOrWhiteSpace(input.AdminReply)
            ? null
            : StoreHtmlSanitizer.Sanitize(input.AdminReply).Trim();
        feedback.RepliedAtUtc = feedback.AdminReply is null ? null : DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = type == "reviews" ? "وضعیت دیدگاه ذخیره شد." : "وضعیت پرسش و پاسخ ذخیره شد." });
    }
}

public sealed record FeedbackModerationInput(int StatusId, string? AdminReply);
