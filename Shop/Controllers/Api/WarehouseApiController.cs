using Microsoft.AspNetCore.Mvc;
using Shop.Database.Application.Warehouse.Contracts;
using Shop.Database.Application.Warehouse.Models;

namespace Shop.Controllers.Api;

[ApiController]
[Route("api/warehouse")]
public sealed class WarehouseApiController(IWarehouseReadService warehouseReadService) : ControllerBase
{
    [HttpGet("warehouses")]
    [ProducesResponseType<IReadOnlyList<WarehouseInfo>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseInfo>>> GetWarehouses(CancellationToken cancellationToken) =>
        Ok(await warehouseReadService.GetWarehousesAsync(cancellationToken));

    [HttpGet("stock")]
    [ProducesResponseType<IReadOnlyList<WarehouseStockItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WarehouseStockItem>>> GetStock(
        [FromQuery] long? warehouseId,
        [FromQuery] string? salesPlanKey,
        [FromQuery] string? search,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200)
            return ValidationProblem(detail: "page must be positive and pageSize must be between 1 and 200.", statusCode: 400);

        return Ok(await warehouseReadService.GetStockAsync(
            warehouseId, salesPlanKey, search, lowStockOnly, page, pageSize, cancellationToken));
    }
}