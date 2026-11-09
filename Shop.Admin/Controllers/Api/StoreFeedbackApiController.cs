using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Store.Entities;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/feedback")]
public sealed class StoreFeedbackApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet("reviews")]
    public Task<IActionResult> Reviews([FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        ListAsync("reviews", status, search, page, cancellationToken);

    [HttpGet("questions")]
    public Task<IActionResult> Questions([FromQuery] string? status, [FromQuery] string? search,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        ListAsync("questions", status, search, page, cancellationToken);

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

    private async Task<IActionResult> ListAsync(string type, string? status, string? search, int page,
        CancellationToken cancellationToken)
    {
        if (page < 1 || page > 100000 || status is not (null or "pending" or "approved" or "rejected"))
            return ValidationProblem("فیلتر یا شمارهٔ صفحه معتبر نیست.");
        var query = from feedback in db.ProductFeedback.AsNoTracking()
            join product in db.Products.AsNoTracking() on feedback.IdProduct equals product.Id
            where feedback.FeedbackType == type
            select new
            {
                feedback.Id, feedback.IdProduct, StoreProductKey = product.ProductKey, ProductName = product.Name, feedback.DisplayName,
                feedback.FeedbackText, feedback.Rating, feedback.Status, feedback.AdminReply, feedback.IsSample, feedback.CreatedAtUtc,
                feedback.RepliedAtUtc
            };
        if (status is not null) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            query = query.Where(x =>
                x.StoreProductKey.Contains(term) || x.ProductName.Contains(term) || x.DisplayName.Contains(term) || x.FeedbackText.Contains(term));
        }

        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 50).Take(50).ToListAsync(cancellationToken);
        return Ok(new { total, page, pageSize = 50, items });
    }

    private async Task<IActionResult> ModerateAsync(long id, string type, FeedbackModerationInput input,
        CancellationToken cancellationToken)
    {
        if (input.Status is not ("pending" or "approved" or "rejected") || (input.AdminReply?.Length ?? 0) > 1000)
            return ValidationProblem("وضعیت یا متن پاسخ معتبر نیست.");
        ProductFeedback? feedback =
            await db.ProductFeedback.SingleOrDefaultAsync(x => x.Id == id && x.FeedbackType == type, cancellationToken);
        if (feedback is null) return NotFound();
        feedback.Status = input.Status;
        feedback.AdminReply = string.IsNullOrWhiteSpace(input.AdminReply) ? null : input.AdminReply.Trim();
        feedback.RepliedAtUtc = feedback.AdminReply is null ? null : DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = type == "reviews" ? "وضعیت دیدگاه ذخیره شد." : "وضعیت پرسش و پاسخ ذخیره شد." });
    }
}

public sealed record FeedbackModerationInput(string Status, string? AdminReply);
