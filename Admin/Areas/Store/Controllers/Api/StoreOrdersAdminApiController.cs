using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/orders")]
public sealed class StoreOrdersAdminApiController(IStoreOrdersAdminData orderData) : ControllerBase
{
    [HttpGet("statuses")]
    public async Task<IActionResult> Statuses(CancellationToken cancellationToken) =>
        Ok(await orderData.ReadStatusesAsync(cancellationToken));

    [HttpPut("{id:long}/shipping-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingStatus(long id, [FromBody] ShippingStatusInput input,
        CancellationToken cancellationToken)
    {
        if (id <= 0 || input.ExpectedStatus is null || input.Status is null ||
            !((input.ExpectedStatus == "warehouse-confirmed" && input.Status is "in-transit" or "shipped-to-post") ||
              (input.ExpectedStatus is "in-transit" or "shipped-to-post" && input.Status == "delivered")))
            return BadRequest(new { title = "تغییر وضعیت ارسال مجاز نیست." });

        var update = await orderData.UpdateShippingStatusAsync(id, input.ExpectedStatus, input.Status,
            cancellationToken);
        if (update == ShippingStatusUpdateStatus.Conflict)
            return Conflict(new { title = "وضعیت سفارش تغییر کرده یا پرداخت و حوالهٔ معتبر ندارد." });
        if (update == ShippingStatusUpdateStatus.MissingStatus)
            return BadRequest(new { title = "وضعیت سفارش در جدول مرجع پیدا نشد." });

        return Ok(new { message = "وضعیت ارسال ثبت شد." });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize, [FromQuery] string? status = null,
        [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging))
            return ValidationProblem(detail: "عبارت جست‌وجو یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        string normalizedStatus = status?.Trim() ?? "";
        if (normalizedStatus.Length > 40 || normalizedStatus.Any(char.IsControl) ||
            normalizedStatus != "" && normalizedStatus is not ("pending-payment" or
                "payment-confirmed" or "warehouse-confirmed" or "in-transit" or "shipped-to-post" or "delivered"
                or "cancelled"))
            return ValidationProblem(detail: "وضعیت سفارش معتبر نیست.", statusCode: 400);

        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return ValidationProblem(detail: "تاریخ شروع باید قبل از تاریخ پایان باشد.", statusCode: 400);

        if (to == DateOnly.MaxValue)
            return ValidationProblem(detail: "تاریخ پایان معتبر نیست.", statusCode: 400);

        if (from.HasValue && to.HasValue && to.Value.DayNumber - from.Value.DayNumber > 365)
            return ValidationProblem(detail: "بازهٔ تاریخ نمی‌تواند بیشتر از ۳۶۶ روز باشد.", statusCode: 400);

        q = paging.Search;
        DateTime? fromUtc = from.HasValue
            ? DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
        DateTime? untilUtc = to.HasValue
            ? DateTime.SpecifyKind(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
            : null;
        var rows = await orderData.ReadOrdersAsync(q ?? "", normalizedStatus, fromUtc, untilUtc,
            paging.Offset, paging.PageSize, cancellationToken);
        return Ok(AdminListQuery.CreateResult(rows.Items, rows.Total, paging));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        var order = await orderData.ReadOrderDetailsAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }
    public sealed record ShippingStatusInput(string? ExpectedStatus, string? Status);

}
