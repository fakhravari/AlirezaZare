using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/orders")]
public sealed class StoreOrdersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        q = q?.Trim() ?? "";
        if (q.Length > 100 || page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { title = "جست‌وجو یا صفحه‌بندی نامعتبر است." });
        int offset = (page - 1) * pageSize;
        int take = pageSize + 1;
        var rows = await db.Database.SqlQuery<OrderRow>($"""
            SELECT o.[Id], o.[OrderNumber], o.[Status], o.[PaymentMethod], o.[ItemCount], o.[TotalAmount], o.[OrderedAtUtc],
                   CONVERT(bit, CASE WHEN o.[PaymentMethod] = N'demo' AND o.[ExternalOrderKey] LIKE N'demo-order-%' THEN 1 ELSE 0 END) AS [IsSample],
                   LTRIM(RTRIM(CONCAT(p.[FirstName], N' ', p.[LastName]))) AS [CustomerName]
            FROM [Store].[Order] o JOIN [Account].[Profile] p ON p.[Id] = o.[IdProfile]
            WHERE {q} = N'' OR CHARINDEX({q}, o.[OrderNumber]) > 0
                OR CHARINDEX({q}, CONCAT(p.[FirstName], N' ', p.[LastName])) > 0
            ORDER BY o.[Id] DESC OFFSET {offset} ROWS FETCH NEXT {take} ROWS ONLY
            """).ToListAsync(cancellationToken);
        return Ok(new { items = rows.Take(pageSize), page, hasNext = rows.Count > pageSize });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Details(long id, CancellationToken cancellationToken)
    {
        if (id <= 0) return NotFound();
        var order = await db.Database.SqlQuery<OrderDetailsRow>($"""
            SELECT o.[Id], o.[OrderNumber], o.[Status], o.[PaymentMethod], o.[ItemCount], o.[TotalAmount], o.[OrderedAtUtc],
                   CONVERT(bit, CASE WHEN o.[PaymentMethod] = N'demo' AND o.[ExternalOrderKey] LIKE N'demo-order-%' THEN 1 ELSE 0 END) AS [IsSample],
                   LTRIM(RTRIM(CONCAT(p.[FirstName], N' ', p.[LastName]))) AS [CustomerName],
                   o.[IdWarehouseDocument], COALESCE(o.[AddressSnapshotJson], CONCAT(NCHAR(123), NCHAR(125))) AS [AddressSnapshotJson],
                   COALESCE((SELECT i.[ProductNameSnapshot] AS [name], i.[Quantity] AS [quantity], i.[UnitAmount] AS [unitAmount],
                           s.[SalesPlanKey] AS [salesPlan], JSON_QUERY(i.[OptionsJson]) AS [options]
                    FROM [Store].[OrderItem] i JOIN [Warehouse].[SalesPlanReference] s ON s.[Id] = i.[IdSalesPlan]
                    WHERE i.[IdCustomerOrder] = o.[Id] ORDER BY i.[Id] FOR JSON PATH), N'[]') AS [ItemsJson]
            FROM [Store].[Order] o JOIN [Account].[Profile] p ON p.[Id] = o.[IdProfile] WHERE o.[Id] = {id}
            """).SingleOrDefaultAsync(cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    public class OrderRow
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string Status { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderedAtUtc { get; set; }
        public string CustomerName { get; set; } = "";
        public bool IsSample { get; set; }
    }

    public sealed class OrderDetailsRow : OrderRow
    {
        public long? IdWarehouseDocument { get; set; }
        public string AddressSnapshotJson { get; set; } = "{}";
        public string ItemsJson { get; set; } = "[]";
    }
}
