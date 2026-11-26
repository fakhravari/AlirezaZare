using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Helpers;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/newsletter-subscribers")]
public sealed class StoreNewsletterSubscribersAdminApiController(IStoreNewsletterAdminData data) : ControllerBase
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

        int total = await data.CountAsync(paging.Search, includeUnsubscribed, cancellationToken);
        var items = await data.ReadPageAsync(paging.Search, includeUnsubscribed, paging.Offset, paging.PageSize,
            cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    [HttpPut("{id:long}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(long id, [FromBody] NewsletterStatusInput input,
        CancellationToken cancellationToken)
    {
        if (!await data.SetSubscribedAsync(id, input.Subscribed, cancellationToken)) return NotFound();

        return Ok(new { message = input.Subscribed ? "عضویت فعال شد." : "عضویت لغو شد." });
    }

    [HttpGet("export")]
    public IActionResult Export([FromQuery] string? q, [FromQuery] bool includeUnsubscribed = false,
        CancellationToken cancellationToken = default)
    {
        string search = AdminListQuery.NormalizeSearch(q);
        if (search.Length > AdminListQuery.MaximumSearchLength || search.Any(char.IsControl))
            return ValidationProblem(detail: "عبارت جست‌وجو معتبر نیست.", statusCode: 400);

        return new AdminExcelFileResult("newsletter-subscribers.xlsx", "اعضا", async (worksheet, ct) =>
        {
            await worksheet.WriteRowAsync(ct, "ایمیل", "تاریخ عضویت", "تاریخ لغو");
            decimal lastId = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                var rows = await data.ReadExportBatchAsync(search, includeUnsubscribed, lastId,
                    AdminListQuery.ExportBatchSize, ct);
                if (rows.Count == 0) break;

                foreach (var row in rows)
                    await worksheet.WriteRowAsync(ct, row.Email, row.SubscribedAtUtc.ToString("O"),
                        row.UnsubscribedAtUtc?.ToString("O"));

                lastId = rows[^1].Id;
                if (rows.Count < AdminListQuery.ExportBatchSize) break;
            }
        });
    }
}

public sealed record NewsletterStatusInput(bool Subscribed);
