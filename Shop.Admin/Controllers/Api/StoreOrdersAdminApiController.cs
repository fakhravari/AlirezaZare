using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;
using System.Security.Claims;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/orders")]
public sealed class StoreOrdersAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpPut("{id:long}/tracking")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTracking(long id, [FromBody] TrackingInput input, CancellationToken cancellationToken)
    {
        string carrier = input.Carrier?.Trim() ?? "", code = input.TrackingCode?.Trim() ?? "";
        if (id <= 0 || carrier.Length is < 1 or > 100 || code.Length is < 1 or > 100 ||
            carrier.Any(char.IsControl) || code.Any(char.IsControl) ||
            (input.ExpectedCarrier?.Length ?? 0) > 100 || (input.ExpectedTrackingCode?.Length ?? 0) > 100)
            return BadRequest(new { title = "شرکت حمل یا کد رهگیری معتبر نیست." });
        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (actor.Length > 256) return BadRequest(new { title = "شناسهٔ مدیر نامعتبر است." });
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var changes = await db.Database.SqlQuery<int>($"""
            DECLARE @changes TABLE ([IdOrder] bigint, [PreviousCarrier] nvarchar(100), [PreviousTrackingCode] nvarchar(100), [Carrier] nvarchar(100), [TrackingCode] nvarchar(100));
            UPDATE o SET [ShippingCarrier] = {carrier}, [ShippingTrackingCode] = {code}
            OUTPUT INSERTED.[Id], DELETED.[ShippingCarrier], DELETED.[ShippingTrackingCode], INSERTED.[ShippingCarrier], INSERTED.[ShippingTrackingCode]
                INTO @changes
            FROM [Store].[Order] o
            WHERE o.[Id] = {id} AND o.[Status] IN (N'warehouse-confirmed', N'in-transit', N'shipped-to-post', N'delivered')
              AND o.[IdWarehouseDocument] IS NOT NULL
              AND NOT (o.[PaymentMethod] = N'demo' AND COALESCE(o.[ExternalOrderKey], N'') LIKE N'demo-order-%')
              AND EXISTS (SELECT 1 FROM [Store].[PaymentAttempt] p WHERE p.[IdOrder] = o.[Id] AND p.[Status] = N'verified' AND p.[Amount] = o.[TotalAmount])
              AND (o.[ShippingCarrier] = {input.ExpectedCarrier} OR (o.[ShippingCarrier] IS NULL AND {input.ExpectedCarrier} IS NULL))
              AND (o.[ShippingTrackingCode] = {input.ExpectedTrackingCode} OR (o.[ShippingTrackingCode] IS NULL AND {input.ExpectedTrackingCode} IS NULL));
            INSERT [Store].[OrderShipmentTrackingHistory] ([IdOrder], [PreviousCarrier], [PreviousTrackingCode], [Carrier], [TrackingCode], [ActorUserKey])
            SELECT [IdOrder], [PreviousCarrier], [PreviousTrackingCode], [Carrier], [TrackingCode], {actor} FROM @changes;
            SELECT COUNT(*) AS [Value] FROM @changes;
            """).ToListAsync(cancellationToken);
        if (changes.Single() != 1) return Conflict(new { title = "اطلاعات سفارش تغییر کرده یا سفارش شرایط ثبت رهگیری را ندارد." });
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "اطلاعات رهگیری ثبت شد." });
    }

    [HttpPut("{id:long}/shipping-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingStatus(long id, [FromBody] ShippingStatusInput input, CancellationToken cancellationToken)
    {
        if (id <= 0 || input.ExpectedStatus is null || input.Status is null ||
            !((input.ExpectedStatus == "warehouse-confirmed" && input.Status is "in-transit" or "shipped-to-post") ||
              (input.ExpectedStatus is "in-transit" or "shipped-to-post" && input.Status == "delivered")))
            return BadRequest(new { title = "تغییر وضعیت ارسال مجاز نیست." });
        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        if (actor.Length > 256) return BadRequest(new { title = "شناسهٔ مدیر نامعتبر است." });
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        int changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE o SET [Status] = {input.Status}
            FROM [Store].[Order] o
            WHERE o.[Id] = {id} AND o.[Status] = {input.ExpectedStatus} AND o.[IdWarehouseDocument] IS NOT NULL
              AND NOT (o.[PaymentMethod] = N'demo' AND COALESCE(o.[ExternalOrderKey], N'') LIKE N'demo-order-%')
              AND EXISTS (SELECT 1 FROM [Store].[PaymentAttempt] p WHERE p.[IdOrder] = o.[Id]
                  AND p.[Status] = N'verified' AND p.[Amount] = o.[TotalAmount]);
            """, cancellationToken);
        if (changed != 1) return Conflict(new { title = "وضعیت سفارش تغییر کرده یا پرداخت و حوالهٔ معتبر ندارد." });
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT [Store].[OrderShippingHistory] ([IdOrder], [PreviousStatus], [NewStatus], [ActorUserKey])
            VALUES ({id}, {input.ExpectedStatus}, {input.Status}, {actor});
            """, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "وضعیت ارسال ثبت شد." });
    }

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
                   o.[IdWarehouseDocument], o.[ShippingCarrier], o.[ShippingTrackingCode], COALESCE(o.[AddressSnapshotJson], CONCAT(NCHAR(123), NCHAR(125))) AS [AddressSnapshotJson],
                   COALESCE((SELECT i.[ProductNameSnapshot] AS [name], i.[Quantity] AS [quantity], i.[UnitAmount] AS [unitAmount],
                           s.[SalesPlanKey] AS [salesPlan], JSON_QUERY(i.[OptionsJson]) AS [options]
                    FROM [Store].[OrderItem] i JOIN [Warehouse].[SalesPlanReference] s ON s.[Id] = i.[IdSalesPlan]
                    WHERE i.[IdCustomerOrder] = o.[Id] ORDER BY i.[Id] FOR JSON PATH), N'[]') AS [ItemsJson],
                   COALESCE((SELECT h.[PreviousStatus], h.[NewStatus], h.[ActorUserKey], h.[ChangedAtUtc]
                    FROM [Store].[OrderShippingHistory] h WHERE h.[IdOrder] = o.[Id] ORDER BY h.[Id] DESC FOR JSON PATH), N'[]') AS [ShippingHistoryJson]
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
        public string? ShippingCarrier { get; set; }
        public string? ShippingTrackingCode { get; set; }
        public string AddressSnapshotJson { get; set; } = "{}";
        public string ItemsJson { get; set; } = "[]";
        public string ShippingHistoryJson { get; set; } = "[]";
    }
    public sealed record ShippingStatusInput(string? ExpectedStatus, string? Status);
    public sealed record TrackingInput(string? Carrier, string? TrackingCode, string? ExpectedCarrier, string? ExpectedTrackingCode);
}
