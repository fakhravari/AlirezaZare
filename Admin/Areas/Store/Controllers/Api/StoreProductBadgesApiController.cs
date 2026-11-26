using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/product-badges")]
public sealed class StoreProductBadgesApiController(IStoreProductBadgeAdminData data) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await data.ReadBadgesAsync(ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] BadgeInput input, CancellationToken ct) =>
        await Save(null, input, ct);

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] BadgeInput input, CancellationToken ct) =>
        await Save(id, input, ct);

    private async Task<IActionResult> Save(long? id, BadgeInput input, CancellationToken ct)
    {
        string title = input.Title?.Trim() ?? "";
        string color = (input.BackgroundColor ?? "#102A43").Trim().ToUpperInvariant();
        string textColor = (input.TextColor ?? "#FFFFFF").Trim().ToUpperInvariant();
        string borderColor = (input.BorderColor ?? "#FFFFFF").Trim().ToUpperInvariant();
        if (title.Length is < 1 or > 100) return BadRequest(new { title = "عنوان نشان باید بین ۱ تا ۱۰۰ نویسه باشد." });
        if (!System.Text.RegularExpressions.Regex.IsMatch(color, "^#[0-9A-F]{6}$"))
            return BadRequest(new { title = "رنگ نشان معتبر نیست." });
        if (!System.Text.RegularExpressions.Regex.IsMatch(textColor, "^#[0-9A-F]{6}$"))
            return BadRequest(new { title = "رنگ متن نشان معتبر نیست." });
        if (!System.Text.RegularExpressions.Regex.IsMatch(borderColor, "^#[0-9A-F]{6}$"))
            return BadRequest(new { title = "رنگ حاشیهٔ نشان معتبر نیست." });
        var result = await data.SaveBadgeAsync(id, title, color, textColor, borderColor, ct);
        if (result.Kind == ProductBadgeSaveResultKind.Duplicate)
            return Conflict(new { title = "این نشان قبلاً ثبت شده است." });
        if (result.Kind == ProductBadgeSaveResultKind.NotFound) return NotFound();
        return Ok(result.Badge);
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await data.DeleteBadgeAsync(id, ct);
        if (result == ProductBadgeDeleteResult.NotFound) return NotFound();
        if (result == ProductBadgeDeleteResult.Assigned)
            return Conflict(new { title = "این نشان به کالا تخصیص دارد؛ ابتدا تخصیص آن را بردارید." });
        return Ok(new { deleted = true });
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string? q, [FromQuery] long? badgeId,
        [FromQuery] bool withoutBadge = false, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        q = q?.Trim() ?? "";
        if (q.Length > 100 || page is < 1 or > 100000 || badgeId is < 1 || (badgeId.HasValue && withoutBadge))
            return BadRequest();
        var (total, rows) = await data.ReadProductsAsync(q, badgeId, withoutBadge, (page - 1) * 30, 30, ct);
        var items = rows.Select(row => new { row.Id, Name = row.Name, code = row.Code, IdBadge = row.BadgeId });
        return Ok(new { items, total, page, pageSize = 30 });
    }

    [HttpPut("products/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignmentInput input, CancellationToken ct)
    {
        var result = await data.AssignAsync(id, input.BadgeId, ct);
        if (result == ProductBadgeAssignmentResult.ProductNotFound) return NotFound();
        if (result == ProductBadgeAssignmentResult.BadgeNotFound)
            return BadRequest(new { title = "نشان انتخاب‌شده معتبر نیست." });
        return Ok(new { saved = true });
    }

    public sealed record BadgeInput(string? Title, string? BackgroundColor, string? TextColor, string? BorderColor);

    public sealed record AssignmentInput(long? BadgeId);
}
