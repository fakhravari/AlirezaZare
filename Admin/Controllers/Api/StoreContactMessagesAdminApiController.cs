using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/contact-messages")]
public sealed class StoreContactMessagesAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? q,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > 100000 || status is not (null or "" or "new" or "read" or "closed"))
            return ValidationProblem(detail: "فیلتر یا شمارهٔ صفحه معتبر نیست.", statusCode: 400);
        var query = db.StoreContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(q))
        {
            string term = q.Trim();
            query = query.Where(x => x.Name.Contains(term) || x.Phone.Contains(term) || x.Message.Contains(term));
        }
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 50).Take(50)
            .Select(x => new { x.Id, x.Name, x.Phone, x.SubjectKey, x.Message, x.Status, x.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        return Ok(new { total, page, pageSize = 50, items });
    }

    [HttpPut("{id:long}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] ContactMessageStatusInput input,
        CancellationToken cancellationToken)
    {
        if (input.Status is not ("new" or "read" or "closed"))
            return ValidationProblem(detail: "وضعیت پیام معتبر نیست.", statusCode: 400);
        var item = await db.StoreContactMessages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();
        item.Status = input.Status;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "وضعیت پیام ذخیره شد." });
    }
}

public sealed record ContactMessageStatusInput(string Status);
