using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/product-badges")]
public sealed class StoreProductBadgesApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await db.StoreProductBadges.AsNoTracking()
        .OrderBy(x => x.Title).Select(x => new { x.Id, x.Title, ProductCount = db.Products.Count(p => p.IdProductBadge == x.Id) }).ToListAsync(ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] BadgeInput input, CancellationToken ct) => await Save(null, input, ct);

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] BadgeInput input, CancellationToken ct) => await Save(id, input, ct);

    private async Task<IActionResult> Save(long? id, BadgeInput input, CancellationToken ct)
    {
        string title = input.Title?.Trim() ?? "";
        if (title.Length is < 1 or > 100) return BadRequest(new { title = "عنوان نشان باید بین ۱ تا ۱۰۰ نویسه باشد." });
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (await db.StoreProductBadges.AnyAsync(x => x.Id != id && x.Title == title, ct)) return Conflict(new { title = "این نشان قبلاً ثبت شده است." });
        StoreProductBadge? badge = id.HasValue ? await db.StoreProductBadges.SingleOrDefaultAsync(x => x.Id == id.Value, ct) : new StoreProductBadge();
        if (badge is null) return NotFound();
        badge.Title = title;
        if (!id.HasValue) db.StoreProductBadges.Add(badge);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { badge.Id, badge.Title });
    }

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var badge = await db.StoreProductBadges.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (badge is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.IdProductBadge == id, ct)) return Conflict(new { title = "این نشان به کالا تخصیص دارد؛ ابتدا تخصیص آن را بردارید." });
        db.StoreProductBadges.Remove(badge);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { deleted = true });
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string? q, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        q = q?.Trim() ?? "";
        if (q.Length > 100 || page is < 1 or > 100000) return BadRequest();
        var query = db.Products.AsNoTracking().Where(x => q == "" || x.Name.Contains(q) || x.ProductKey.Contains(q));
        int total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Skip((page - 1) * 30).Take(30)
            .Select(x => new { x.Id, x.Name, code = x.ProductKey, x.IdProductBadge }).ToListAsync(ct);
        return Ok(new { items, total, page, pageSize = 30 });
    }

    [HttpPut("products/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignmentInput input, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound();
        if (input.BadgeId.HasValue && !await db.StoreProductBadges.AnyAsync(x => x.Id == input.BadgeId, ct)) return BadRequest(new { title = "نشان انتخاب‌شده معتبر نیست." });
        product.IdProductBadge = input.BadgeId;
        product.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { saved = true });
    }

    public sealed record BadgeInput(string? Title);
    public sealed record AssignmentInput(long? BadgeId);
}
