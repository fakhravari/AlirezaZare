using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/account-messages")]
public sealed class StoreAccountMessagesAdminApiController(IStoreAccountMessageAdminData data) : ControllerBase
{
    [HttpGet("customers")]
    public async Task<IActionResult> Customers([FromQuery] string? q, CancellationToken cancellationToken)
    {
        string term = AdminListQuery.NormalizeSearch(q);
        if (term.Length > AdminListQuery.MaximumSearchLength)
            return ValidationProblem(detail: "عبارت جست‌وجو بیش از حد طولانی است.", statusCode: 400);

        if (term.Length < 2) return Ok(Array.Empty<object>());

        return Ok(await data.FindCustomersAsync(term, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

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
        var (total, items) = await data.ReadMessagesAsync(paging.Search, fromUtc, untilUtc, paging.Offset,
            paging.PageSize, cancellationToken);
        var result = items.Select(item => new
        {
            item.Id,
            item.Title,
            item.Category,
            Body = StoreHtmlSanitizer.Sanitize(item.Body),
            item.IsRead,
            item.CreatedAtUtc,
            item.ProfileId,
            item.Email,
            item.FirstName,
            item.LastName
        }).ToArray();

        return Ok(AdminListQuery.CreateResult(result, total, paging));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] AccountMessageInput input, CancellationToken cancellationToken)
    {
        string title = input.Title?.Trim() ?? "", body = StoreHtmlSanitizer.Sanitize(input.Body).Trim();
        if (input.ProfileId <= 0 || title.Length is < 2 or > 200 || body.Length is < 1 or > 4000 ||
            input.Category is not ("notice" or "support" or "orders" or "products"))
            return ValidationProblem(detail: "اطلاعات پیام معتبر نیست.", statusCode: 400);

        if (!await data.SendMessageAsync(input.ProfileId, title, input.Category, body, cancellationToken))
            return NotFound();

        return Ok(new { message = "پیام در حساب مشتری ثبت شد." });
    }
}

public sealed record AccountMessageInput(long ProfileId, string? Title, string? Category, string? Body);
