SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() NOT LIKE N'ShopVerify[_]%'
    THROW 51110, 'Dispatch fixtures are restricted to ShopVerify_ databases.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @Profile BIGINT = (SELECT TOP (1) [Id] FROM [Account].[Profile] ORDER BY [Id]);
    IF @Profile IS NULL THROW 51111, 'A test profile is required.', 1;
    DECLARE @Fixtures TABLE ([OrderNumber] NVARCHAR(32), [ItemId] BIGINT, [PlanId] BIGINT);
    INSERT @Fixtures VALUES (N'DISPATCH-TEST-001', 1, 1), (N'DISPATCH-TEST-002', 1, 2), (N'DISPATCH-TEST-003', 2, 1);
    IF EXISTS (SELECT 1 FROM @Fixtures f LEFT JOIN [Warehouse].[InventoryItem] i ON i.[Id]=f.[ItemId]
        LEFT JOIN [Warehouse].[SalesPlanReference] salesPlan ON salesPlan.[Id]=f.[PlanId]
        WHERE i.[Id] IS NULL OR salesPlan.[Id] IS NULL)
        THROW 51112, 'Required test inventory or sales plan is missing.', 1;
    INSERT [Store].[Order] ([IdProfile],[OrderNumber],[Status],[PaymentMethod],[ItemCount],[TotalAmount],[AddressSnapshotJson],[ExternalOrderKey])
    SELECT @Profile,f.[OrderNumber],N'payment-confirmed',N'demo',1,100,N'{}',f.[OrderNumber]
    FROM @Fixtures f WHERE NOT EXISTS (SELECT 1 FROM [Store].[Order] o WHERE o.[OrderNumber]=f.[OrderNumber]);
    INSERT [Store].[OrderItem] ([IdCustomerOrder],[IdProduct],[IdProductVariant],[ProductNameSnapshot],[IdSalesPlan],[Quantity],[UnitAmount],[OptionsJson])
    SELECT o.[Id],item.[IdProduct],item.[IdProductVariant],product.[Name],f.[PlanId],1,100,N'{}'
    FROM @Fixtures f JOIN [Store].[Order] o ON o.[OrderNumber]=f.[OrderNumber]
    JOIN [Warehouse].[InventoryItem] item ON item.[Id]=f.[ItemId] JOIN [Warehouse].[Product] product ON product.[Id]=item.[IdProduct]
    WHERE NOT EXISTS (SELECT 1 FROM [Store].[OrderItem] line WHERE line.[IdCustomerOrder]=o.[Id]);
    INSERT [Store].[PaymentAttempt] ([Id],[IdOrder],[Amount],[Status],[GatewayReference],[ReceiptReference],[PaidAtUtc])
    SELECT NEWID(),o.[Id],100,N'verified',CONCAT(N'test-gateway-',f.[OrderNumber]),CONCAT(N'test-receipt-',f.[OrderNumber]),SYSUTCDATETIME()
    FROM @Fixtures f JOIN [Store].[Order] o ON o.[OrderNumber]=f.[OrderNumber]
    WHERE NOT EXISTS (SELECT 1 FROM [Store].[PaymentAttempt] p WHERE p.[IdOrder]=o.[Id] AND p.[Status]=N'verified');
    INSERT [Warehouse].[PaidOrderInbox] ([StoreOrderKey],[CustomerKey],[PaymentAtUtc])
    SELECT f.[OrderNumber],CONVERT(NVARCHAR(128),@Profile),SYSUTCDATETIME()
    FROM @Fixtures f WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInbox] inbox WHERE inbox.[StoreOrderKey]=f.[OrderNumber]);
    INSERT [Warehouse].[PaidOrderInboxLine] ([IdPaidOrderInbox],[IdInventoryItem],[IdUnit],[Quantity],[ConversionFactor],[BaseQuantity],[IdSalesPlanReference])
    SELECT inbox.[Id],item.[Id],product.[IdBaseUnit],1,1,1,f.[PlanId]
    FROM @Fixtures f JOIN [Warehouse].[PaidOrderInbox] inbox ON inbox.[StoreOrderKey]=f.[OrderNumber]
    JOIN [Warehouse].[InventoryItem] item ON item.[Id]=f.[ItemId] JOIN [Warehouse].[Product] product ON product.[Id]=item.[IdProduct]
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxLine] l WHERE l.[IdPaidOrderInbox]=inbox.[Id]);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
