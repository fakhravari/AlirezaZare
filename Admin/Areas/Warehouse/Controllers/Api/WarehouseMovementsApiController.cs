using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Warehouse.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Warehouse.Controllers.Api;

[Area("Warehouse")]
[ApiController]
[Route("api/admin/warehouse/movements")]
public sealed class WarehouseMovementsApiController(IWarehouseMovementsAdminData data) : AdminOperationController
{
    [HttpGet]
    [HttpGet("products")]
    public async Task<IActionResult> Get([FromQuery] long? warehouseId, [FromQuery] string? search,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1,
        [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        [FromQuery] bool audit = false, [FromQuery] bool summary = false, [FromQuery] string? salesPlan = null,
        [FromQuery] string? direction = null, [FromQuery] long? productId = null,
        CancellationToken cancellationToken = default) =>
        Respond(await data.Get(Request.Path.Value?.EndsWith("/products", StringComparison.OrdinalIgnoreCase) == true, warehouseId, search, from, to, page, pageSize, audit, summary, salesPlan, direction, productId, cancellationToken));
}
