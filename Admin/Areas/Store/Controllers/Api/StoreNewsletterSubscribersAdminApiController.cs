using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Infrastructure.Persistence;

using Shop.Data.Helpers;
namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/newsletter-subscribers")]
public sealed class StoreNewsletterSubscribersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] bool includeUnsubscribed = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        IQueryable<Shop.Data.Domain.Store.Entities.StoreNewsletterSubscriber> query = db.StoreNewsletterSubscribers.AsNoTracking();
        if (!includeUnsubscribed)
            query = query.Where(x => x.UnsubscribedAtUtc == null);
        if (paging.Search.Length > 0)
            query = query.Where(x => x.Email.Contains(paging.Search));

        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.SubscribedAtUtc).ThenByDescending(x => x.Id).Skip(paging.Offset).Take(paging.PageSize).Select(x => new { x.Id, x.Email, x.SubscribedAtUtc, x.UnsubscribedAtUtc }).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    [HttpPut("{id:long}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] NewsletterStatusInput input,
        CancellationToken cancellationToken)
    {
        var item = await db.StoreNewsletterSubscribers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        item.UnsubscribedAtUtc = input.Subscribed ? null : DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { message = input.Subscribed ? "عضویت فعال شد." : "عضویت لغو شد." });
    }

    [HttpGet("export")]
    public IActionResult Export([FromQuery] string? q, [FromQuery] bool includeUnsubscribed = false,
        CancellationToken cancellationToken = default)
    {
        string search = AdminListQuery.NormalizeSearch(q);
        if (search.Length > AdminListQuery.MaximumSearchLength || search.Any(char.IsControl))
            return ValidationProblem(detail: "عبارت جست‌وجو معتبر نیست.", statusCode: 400);

        IQueryable<Shop.Data.Domain.Store.Entities.StoreNewsletterSubscriber> query = db.StoreNewsletterSubscribers.AsNoTracking();
        if (!includeUnsubscribed)
            query = query.Where(x => x.UnsubscribedAtUtc == null);
        if (search.Length > 0)
            query = query.Where(x => x.Email.Contains(search));

        return new AdminExcelFileResult("newsletter-subscribers.xlsx", "اعضا", async (worksheet, ct) =>
        {
            await worksheet.WriteRowAsync(ct, "ایمیل", "تاریخ عضویت", "تاریخ لغو");
            long lastId = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var rows = await query.Where(x => x.Id > lastId).OrderBy(x => x.Id).Take(AdminListQuery.ExportBatchSize).Select(x => new { x.Id, x.Email, x.SubscribedAtUtc, x.UnsubscribedAtUtc }).ToListAsync(ct);
                if (rows.Count == 0) break;

                foreach (var row in rows)
                    await worksheet.WriteRowAsync(ct, row.Email, row.SubscribedAtUtc.ToString("O"), row.UnsubscribedAtUtc?.ToString("O"));

                lastId = rows[^1].Id;
                if (rows.Count < AdminListQuery.ExportBatchSize) break;
            }
        });

    }
}

public sealed record NewsletterStatusInput(bool Subscribed);
