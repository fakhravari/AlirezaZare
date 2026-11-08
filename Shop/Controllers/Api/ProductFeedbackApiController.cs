using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Database.Application.Store.Contracts;
using Shop.Database.Application.Store.Models;

namespace Shop.Controllers.Api;

[ApiController]
[AllowAnonymous]
[Route("api/store/products/{productId:int}/feedback")]
public sealed class ProductFeedbackApiController(IProductFeedbackRepository feedback) : ControllerBase
{
    private long? ProfileId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out long id) ? id : null;

    [HttpGet("{type}")]
    public async Task<IActionResult> List(int productId, string type, CancellationToken cancellationToken)
    {
        if (!IsValidType(type)) return BadRequest(new { code = "invalid-feedback-type" });
        ProductFeedbackSnapshot? snapshot = await feedback.ReadAsync(productId, type, ProfileId, cancellationToken);
        return snapshot is null ? NotFound() : Ok(snapshot);
    }

    [HttpPost("{type}")]
    [EnableRateLimiting("product-feedback")]
    public async Task<IActionResult> Submit(int productId, string type, [FromBody] FeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidType(type)) return BadRequest(new { code = "invalid-feedback-type" });
        string name = (request.Name ?? string.Empty).Trim();
        string text = (request.Text ?? string.Empty).Trim();
        int maxTextLength = type == "reviews" ? 800 : 600;
        if (name.Length is < 5 or > 80 || name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2 ||
            text.Length is < 1 || text.Length > maxTextLength ||
            type == "reviews" && request.Rating is not (>= 1 and <= 5) ||
            type == "questions" && request.Rating is not null)
            return BadRequest(new { code = "invalid-feedback" });

        long? id = await feedback.AddAsync(productId, ProfileId, type, name, text, request.Rating, cancellationToken);
        if (!id.HasValue) return NotFound();
        var item = new ProductFeedbackItem(id.Value, name, text, request.Rating, "pending", null,
            DateTimeOffset.UtcNow, null);
        return Ok(new { saved = true, item });
    }

    [HttpPost("{type}/import")]
    [EnableRateLimiting("product-feedback")]
    public async Task<IActionResult> ImportLegacy(int productId, string type,
        [FromBody] LegacyFeedbackRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidType(type) || request.Items is null || request.Items.Count is < 1 or > 50)
            return BadRequest(new { code = "invalid-feedback-import" });
        int maxTextLength = type == "reviews" ? 800 : 600;
        if (request.Items.Any(item => item is null || (item.Name?.Trim().Length ?? 0) is < 5 or > 80 ||
            item.Name!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2 ||
            (item.Text?.Trim().Length ?? 0) is < 1 || item.Text!.Trim().Length > maxTextLength ||
            type == "reviews" && item.Rating is not (>= 1 and <= 5)))
            return BadRequest(new { code = "invalid-feedback-import" });

        ProductFeedbackLegacyItem[] items = request.Items.Select(item => new ProductFeedbackLegacyItem(
            item.Name!.Trim(),
            item.Text!.Trim(),
            type == "reviews" ? item.Rating : null,
            item.CreatedAt)).ToArray();
        int? imported = await feedback.ImportLegacyAsync(productId, ProfileId, type, items, cancellationToken);
        if (!imported.HasValue) return NotFound();
        return Ok(new { saved = true, imported });
    }

    private static bool IsValidType(string type) => type is "reviews" or "questions";

    public sealed record FeedbackRequest(string? Name, string? Text, byte? Rating);
    public sealed record LegacyFeedbackRequest(IReadOnlyList<LegacyFeedbackItem>? Items);
    public sealed record LegacyFeedbackItem(string? Name, string? Text, byte? Rating, DateTimeOffset? CreatedAt);
}
