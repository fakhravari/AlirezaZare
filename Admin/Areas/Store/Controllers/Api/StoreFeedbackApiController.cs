using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/feedback")]
public sealed class StoreFeedbackApiController(IProductFeedbackRepository feedbackData) : ControllerBase
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
            || (statusId.HasValue && !await feedbackData.StatusExistsAsync(statusId.Value, cancellationToken)))
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
        var (total, items) = await feedbackData.ReadAdminAsync(type, statusId, paging.Search, fromUtc, untilUtc,
            paging.Offset, paging.PageSize, cancellationToken);
        var result = items.Select(item => new
        {
            item.Id,
            item.IdProduct,
            StoreProductKey = item.ProductKey,
            item.ProductName,
            item.DisplayName,
            FeedbackText = item.Text,
            item.Rating,
            IdStatus = item.StatusId,
            item.StatusName,
            AdminReply = item.AdminReply is null ? null : StoreHtmlSanitizer.Sanitize(item.AdminReply),
            item.CreatedAtUtc,
            item.RepliedAtUtc
        }).ToArray();

        var paged = AdminListQuery.CreateResult(result, total, paging);
        var statuses = await feedbackData.ReadStatusesAsync(cancellationToken);
        return Ok(new { paged.Items, paged.Total, paged.Page, paged.PageSize, paged.HasNext, Statuses = statuses });
    }

    private async Task<IActionResult> ModerateAsync(long id, string type, FeedbackModerationInput input,
        CancellationToken cancellationToken)
    {
        if ((input.AdminReply?.Length ?? 0) > 1000 ||
            !await feedbackData.StatusExistsAsync(input.StatusId, cancellationToken))
            return ValidationProblem(detail: "وضعیت یا متن پاسخ معتبر نیست.", statusCode: 400);
        string? reply = string.IsNullOrWhiteSpace(input.AdminReply)
            ? null
            : StoreHtmlSanitizer.Sanitize(input.AdminReply).Trim();
        if (!await feedbackData.ModerateAsync(id, type, input.StatusId, reply, cancellationToken)) return NotFound();

        return Ok(new { message = type == "reviews" ? "وضعیت دیدگاه ذخیره شد." : "وضعیت پرسش و پاسخ ذخیره شد." });
    }
}

public sealed record FeedbackModerationInput(int StatusId, string? AdminReply);
