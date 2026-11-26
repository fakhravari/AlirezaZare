using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/vouchers")]
public sealed class VouchersApiController(IWarehouseVouchersAdminData data) : AdminOperationController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? warehouseId, [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await data.List(warehouseId, status, q, page, pageSize, cancellationToken));

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken cancellationToken) =>
        Respond(await data.Lookups(cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detail(decimal id, CancellationToken cancellationToken) =>
        Respond(await data.Detail(id, cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] SaveVoucherInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.Create(input, cancellationToken));

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(decimal id, [FromBody] SaveVoucherInput input,
        CancellationToken cancellationToken) =>
        Respond(await data.Update(id, input, cancellationToken));

    [HttpDelete("{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(decimal id, CancellationToken cancellationToken) =>
        Respond(await data.Delete(id, cancellationToken));
}
