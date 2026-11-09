using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/paid-orders-dispatch")]
public sealed class StorePaidOrdersDispatchApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await db.Database.SqlQueryRaw<PaidOrderRow>("""
            SELECT TOP (200) o.[Id], o.[OrderNumber], o.[TotalAmount], o.[ItemCount], p.[PaidAtUtc],
                LTRIM(RTRIM(CONCAT(profile.[FirstName], N' ', profile.[LastName]))) AS [CustomerName]
            FROM [Store].[Order] o
            JOIN [Account].[Profile] profile ON profile.[Id]=o.[IdProfile]
            JOIN [Store].[PaymentAttempt] p ON p.[IdOrder]=o.[Id] AND p.[Status]=N'verified' AND p.[Amount]=o.[TotalAmount]
            JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=o.[OrderNumber] AND inbox.[Status]='P'
            WHERE o.[Status]=N'payment-confirmed' AND o.[IdWarehouseDocument] IS NULL
                AND EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxLine] l WHERE l.[IdPaidOrderInbox]=inbox.[Id])
            ORDER BY p.[PaidAtUtc], o.[Id]
            """).ToListAsync(cancellationToken);
        return Ok(new { items = rows });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] DispatchInput input, CancellationToken cancellationToken)
    {
        if (input.OrderIds is null || input.OrderIds.Length is < 1 or > 200 || input.OrderIds.Any(id => id <= 0)
            || input.OrderIds.Distinct().Count() != input.OrderIds.Length)
            return BadRequest(new { detail = "سفارش‌های انتخاب‌شده معتبر نیستند." });
        string ids = JsonSerializer.Serialize(input.OrderIds);
        string actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "system";
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var result = await db.Database.SqlQuery<DispatchResult>($"""
                DECLARE @Ids TABLE ([Id] bigint NOT NULL PRIMARY KEY);
                INSERT @Ids SELECT CONVERT(bigint, [value]) FROM OPENJSON({ids});
                DECLARE @Warehouse bigint, @Type bigint, @Party bigint, @Plan bigint, @Document bigint, @Number bigint;
                SELECT @Warehouse=s.[IdWarehouse] FROM [Store].[SiteSetting] s
                    JOIN [Warehouse].[Warehouse] w ON w.[Id]=s.[IdWarehouse] AND w.[IsActive]=1 WHERE s.[Id]=1;
                SELECT @Type=[Id] FROM [Warehouse].[DocumentType] WHERE [Code]=N'SALES-DISPATCH' AND [IsActive]=1 AND [Direction]='I';
                SELECT @Party=[Id] FROM [Warehouse].[Party] WHERE [ExternalPartyKey]=N'STORE-SALES' AND [IsActive]=1;
                IF @Warehouse IS NULL OR @Type IS NULL OR @Party IS NULL THROW 51040, 'Dispatch configuration missing.', 1;
                IF (SELECT COUNT(*) FROM @Ids) <> (SELECT COUNT(*) FROM @Ids ids
                    JOIN [Store].[Order] o WITH (UPDLOCK, HOLDLOCK) ON o.[Id]=ids.[Id]
                    JOIN [Store].[PaymentAttempt] p ON p.[IdOrder]=o.[Id] AND p.[Status]=N'verified' AND p.[Amount]=o.[TotalAmount]
                    JOIN [Warehouse].[PaidOrderInbox] inbox WITH (UPDLOCK, HOLDLOCK) ON inbox.[StoreOrderKey]=o.[OrderNumber] AND inbox.[Status]='P'
                    WHERE o.[Status]=N'payment-confirmed' AND o.[IdWarehouseDocument] IS NULL
                      AND EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxLine] l WHERE l.[IdPaidOrderInbox]=inbox.[Id]))
                    THROW 51041, 'Orders are no longer eligible.', 1;
                IF EXISTS (SELECT 1 FROM @Ids ids JOIN [Store].[Order] o ON o.[Id]=ids.[Id]
                    JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=o.[OrderNumber]
                    JOIN [Warehouse].[PaidOrderInboxLine] l ON l.[IdPaidOrderInbox]=inbox.[Id]
                    LEFT JOIN [Warehouse].[WarehouseStockOwner] owner ON owner.[IdWarehouse]=@Warehouse AND owner.[IdStockOwner]=l.[IdSalesPlanReference]
                    WHERE owner.[IdWarehouse] IS NULL)
                    THROW 51042, 'Sales plan is not assigned to the sales warehouse.', 1;
                SELECT TOP (1) @Plan=l.[IdSalesPlanReference] FROM @Ids ids
                    JOIN [Store].[Order] o ON o.[Id]=ids.[Id] JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=o.[OrderNumber]
                    JOIN [Warehouse].[PaidOrderInboxLine] l ON l.[IdPaidOrderInbox]=inbox.[Id] ORDER BY l.[IdSalesPlanReference];
                DECLARE @StockKeys TABLE ([ItemId] bigint NOT NULL, [PlanId] bigint NOT NULL, PRIMARY KEY ([ItemId], [PlanId]));
                INSERT @StockKeys ([ItemId], [PlanId])
                    SELECT DISTINCT l.[IdInventoryItem], l.[IdSalesPlanReference] FROM @Ids ids
                    JOIN [Store].[Order] o ON o.[Id]=ids.[Id]
                    JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=o.[OrderNumber]
                    JOIN [Warehouse].[PaidOrderInboxLine] l ON l.[IdPaidOrderInbox]=inbox.[Id];
                DECLARE @LockItem bigint, @LockPlan bigint, @LockBalance bigint;
                WHILE EXISTS (SELECT 1 FROM @StockKeys)
                BEGIN
                    SELECT TOP (1) @LockItem=[ItemId], @LockPlan=[PlanId] FROM @StockKeys ORDER BY [ItemId], [PlanId];
                    SELECT @LockBalance=[Id] FROM [Warehouse].[StockBalance] WITH (UPDLOCK, HOLDLOCK, INDEX([UQ_Warehouse_StockBalance_Key]))
                        WHERE [IdWarehouse]=@Warehouse AND [IdInventoryItem]=@LockItem AND [IdSalesPlanReference]=@LockPlan;
                    DELETE FROM @StockKeys WHERE [ItemId]=@LockItem AND [PlanId]=@LockPlan;
                END;
                INSERT [Warehouse].[WarehouseDocument] ([IdWarehouse],[IdDocumentType],[IdParty],[IdSalesPlanReference],[DocumentDate],[Status],[Reason],[CreatedByUserKey])
                    VALUES (@Warehouse,@Type,@Party,@Plan,CONVERT(date,GETDATE()),'F',N'حواله تجمیعی سفارش‌های فروشگاه',{actor});
                SET @Document=SCOPE_IDENTITY();
                SELECT @Number=[DocumentNumber] FROM [Warehouse].[WarehouseDocument] WHERE [Id]=@Document;
                INSERT [Warehouse].[WarehouseDocumentLine] ([IdDocument],[IdInventoryItem],[IdUnit],[IdSalesPlanReference],[Quantity],[ConversionFactor],[BaseQuantity],[ProductNameSnapshot],[VariantNameSnapshot],[UnitNameSnapshot])
                SELECT @Document,l.[IdInventoryItem],l.[IdUnit],l.[IdSalesPlanReference],SUM(l.[Quantity]),l.[ConversionFactor],SUM(l.[BaseQuantity]),product.[Name],variant.[Name],unit.[Name]
                FROM @Ids ids JOIN [Store].[Order] o ON o.[Id]=ids.[Id]
                JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=o.[OrderNumber]
                JOIN [Warehouse].[PaidOrderInboxLine] l ON l.[IdPaidOrderInbox]=inbox.[Id]
                JOIN [Warehouse].[InventoryItem] item ON item.[Id]=l.[IdInventoryItem]
                JOIN [Warehouse].[Product] product ON product.[Id]=item.[IdProduct]
                LEFT JOIN [Warehouse].[ProductVariant] variant ON variant.[Id]=item.[IdProductVariant]
                JOIN [Warehouse].[Unit] unit ON unit.[Id]=l.[IdUnit]
                GROUP BY l.[IdInventoryItem],l.[IdUnit],l.[IdSalesPlanReference],l.[ConversionFactor],product.[Name],variant.[Name],unit.[Name];
                EXEC [Warehouse].[RebuildDocumentStock] @IdDocument=@Document;
                INSERT [Warehouse].[PaidOrderInboxDocument] ([IdPaidOrderInbox],[IdWarehouseDocument])
                    SELECT inbox.[Id],@Document FROM @Ids ids JOIN [Store].[Order] o ON o.[Id]=ids.[Id]
                    JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=o.[OrderNumber];
                UPDATE inbox SET [Status]='C' FROM [Warehouse].[PaidOrderInbox] inbox JOIN [Store].[Order] o ON o.[OrderNumber]=inbox.[StoreOrderKey] JOIN @Ids ids ON ids.[Id]=o.[Id];
                UPDATE o SET [IdWarehouseDocument]=@Document,[Status]=N'warehouse-confirmed' FROM [Store].[Order] o JOIN @Ids ids ON ids.[Id]=o.[Id];
                INSERT [Warehouse].[DocumentAudit] ([DocumentId],[DocumentNumber],[WarehouseId],[Action],[ActorUserKey],[Reason],[AfterJson])
                    VALUES (@Document,@Number,@Warehouse,N'Created',{actor},N'حواله تجمیعی سفارش‌های فروشگاه',CONCAT(NCHAR(123),NCHAR(34),N'orderIds',NCHAR(34),N':', {ids},NCHAR(125)));
                SELECT @Document AS [Id], @Number AS [DocumentNumber];
                """).ToListAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(result.Single());
        }
        catch (SqlException exception) when (exception.Number is 51002 or 51040 or 51041 or 51042)
        {
            return Conflict(new { detail = exception.Number switch
            {
                51002 => "موجودی برای صدور حواله کافی نیست.",
                51040 => "انبار فروش، نوع حواله فروش یا مرجع آن تنظیم نشده است.",
                51042 => "طرح یکی از اقلام به انبار فروش متصل نیست.",
                _ => "وضعیت یکی از سفارش‌ها تغییر کرده است؛ فهرست را دوباره بارگذاری کنید."
            }});
        }
    }

    public sealed class PaidOrderRow
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaidAtUtc { get; set; }
    }
    public sealed class DispatchResult { public long Id { get; set; } public long DocumentNumber { get; set; } }
    public sealed record DispatchInput(long[]? OrderIds);
}
