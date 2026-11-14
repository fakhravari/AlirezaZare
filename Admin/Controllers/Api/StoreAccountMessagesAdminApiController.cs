using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/account-messages")]
public sealed class StoreAccountMessagesAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet("customers")]
    public async Task<IActionResult> Customers([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return Ok(Array.Empty<object>());
        string term = q.Trim();
        var rows = await db.AccountProfiles.AsNoTracking()
            .Where(x => x.Email.Contains(term) || x.FirstName.Contains(term) || x.LastName.Contains(term))
            .OrderBy(x => x.FirstName).ThenBy(x => x.LastName).Take(20)
            .Select(x => new { x.Id, x.Email, x.FirstName, x.LastName })
            .ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || page > 100000) return ValidationProblem(detail: "شمارهٔ صفحه معتبر نیست.", statusCode: 400);
        var query = from message in db.AccountMessages.AsNoTracking()
                    join profile in db.AccountProfiles.AsNoTracking() on message.IdProfile equals profile.Id
                    select new
                    {
                        message.Id,
                        message.Title,
                        message.Category,
                        message.Body,
                        message.IsRead,
                        message.CreatedAtUtc,
                        ProfileId = profile.Id,
                        profile.Email,
                        profile.FirstName,
                        profile.LastName
                    };
        if (!string.IsNullOrWhiteSpace(q))
        {
            string term = q.Trim();
            query = query.Where(x => x.Email.Contains(term) || x.FirstName.Contains(term) || x.LastName.Contains(term));
        }
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 50).Take(50).ToListAsync(cancellationToken);
        return Ok(new { total, page, pageSize = 50, items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] AccountMessageInput input, CancellationToken cancellationToken)
    {
        string title = input.Title?.Trim() ?? "", body = input.Body?.Trim() ?? "";
        if (input.ProfileId <= 0 || title.Length is < 2 or > 200 || body.Length is < 1 or > 4000 ||
            input.Category is not ("notice" or "support" or "orders" or "products"))
            return ValidationProblem(detail: "اطلاعات پیام معتبر نیست.", statusCode: 400);
        if (!await db.AccountProfiles.AnyAsync(x => x.Id == input.ProfileId, cancellationToken)) return NotFound();
        db.AccountMessages.Add(new AccountMessage
        {
            IdProfile = input.ProfileId,
            Title = title,
            Body = body,
            Category = input.Category,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "پیام در حساب مشتری ثبت شد." });
    }
}

public sealed record AccountMessageInput(long ProfileId, string? Title, string? Category, string? Body);
