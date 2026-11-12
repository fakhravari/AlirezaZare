SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[Order] WHERE [ExternalOrderKey] LIKE N'demo-order-fixture-20261112-%')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    DECLARE @ProfileId BIGINT;
    SELECT @ProfileId = [Id] FROM [Account].[Profile] WHERE [Email] = N'demo-orders-20261112@invalid.example';
    IF @ProfileId IS NULL
    BEGIN
        INSERT [Account].[Profile]([Email],[FirstName],[LastName],[Phone],[CreatedAtUtc],[NewsletterEnabled])
        VALUES(N'demo-orders-20261112@invalid.example',N'دادهٔ آزمایشی',N'سفارش فروشگاه',NULL,SYSUTCDATETIME(),0);
        SET @ProfileId = CONVERT(BIGINT,SCOPE_IDENTITY());
    END
    ELSE IF NOT EXISTS (SELECT 1 FROM [Account].[Profile] WHERE [Id]=@ProfileId AND [FirstName]=N'دادهٔ آزمایشی' AND [LastName]=N'سفارش فروشگاه')
        THROW 51063, 'The reserved demo profile email is already owned by another profile.', 1;

    DECLARE @BankId BIGINT = (SELECT [Id] FROM [Store].[Bank] WHERE [Code]=N'zarinpal' AND [IsActive]=1);
    IF @BankId IS NULL THROW 51064, 'An active Zarinpal bank row is required for demo order fixtures.', 1;

    CREATE TABLE #DemoLines(
        [SequenceNo] INT NOT NULL PRIMARY KEY,
        [PriceVersionId] BIGINT NOT NULL,
        [ProductId] BIGINT NOT NULL,
        [ProductName] NVARCHAR(300) NOT NULL,
        [PlanId] BIGINT NOT NULL,
        [PriceAmount] DECIMAL(19,0) NOT NULL,
        [DiscountPercent] DECIMAL(5,2) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [DayOffset] INT NOT NULL
    );

    ;WITH Eligible AS (
        SELECT v.[Id] AS PriceVersionId,p.[Id] AS ProductId,p.[Name] AS ProductName,sp.[Id] AS PlanId,
               v.[PriceAmount],v.[DiscountPercent],
               ROW_NUMBER() OVER(PARTITION BY p.[Id] ORDER BY sp.[Id]) AS PlanRank
        FROM [Store].[PriceVersion] v
        JOIN [Warehouse].[Product] p ON p.[Id]=v.[IdProduct]
        JOIN [Warehouse].[SalesPlanReference] sp ON sp.[Id]=v.[IdSalesPlan]
        WHERE v.[IsActive]=1 AND v.[PriceKind]=N'product' AND p.[IsCatalogActive]=1 AND p.[IsStoreEnabled]=1
          AND sp.[IsActive]=1 AND sp.[IsRegular]=1
    ), Chosen AS (
        SELECT TOP(7) * FROM Eligible WHERE PlanRank=1 ORDER BY ProductId
    ), Numbered AS (
        SELECT ROW_NUMBER() OVER(ORDER BY ProductId) AS SequenceNo,* FROM Chosen
    )
    INSERT #DemoLines([SequenceNo],[PriceVersionId],[ProductId],[ProductName],[PlanId],[PriceAmount],[DiscountPercent],[Status],[DayOffset])
    SELECT n.SequenceNo,n.PriceVersionId,n.ProductId,n.ProductName,n.PlanId,n.PriceAmount,n.DiscountPercent,s.Status,s.DayOffset
    FROM Numbered n JOIN (VALUES
        (1,N'pending-payment',0),(2,N'payment-confirmed',1),(3,N'warehouse-confirmed',2),
        (4,N'shipped-to-post',3),(5,N'in-transit',4),(6,N'delivered',5),(7,N'cancelled',6)
    ) s(SequenceNo,Status,DayOffset) ON s.SequenceNo=n.SequenceNo;

    IF (SELECT COUNT(*) FROM #DemoLines)<>7
        THROW 51065, 'Seven active catalog products with regular price versions are required for the demo fixtures.', 1;

    DECLARE @CreatedOrders TABLE([SequenceNo] INT PRIMARY KEY,[Id] BIGINT NOT NULL);
    DECLARE @SequenceNo INT=1,@OrderId BIGINT,@OrderNumber NVARCHAR(32),@ExternalKey NVARCHAR(100),@UnitAmount DECIMAL(19,0),@Status NVARCHAR(30),@OrderedAtUtc DATETIME2(3),@Carrier NVARCHAR(100),@TrackingCode NVARCHAR(100);
    WHILE @SequenceNo<=7
    BEGIN
        SELECT @OrderNumber=CONCAT(N'DEMO-ORD-20261112-',RIGHT(CONCAT(N'00',@SequenceNo),3)),
               @ExternalKey=CONCAT(N'demo-order-fixture-20261112-',RIGHT(CONCAT(N'00',@SequenceNo),3)),
               @UnitAmount=CONVERT(DECIMAL(19,0),ROUND([PriceAmount]*(100.0-[DiscountPercent])/100.0,0)),
               @Status=[Status],@OrderedAtUtc=DATEADD(DAY,-[DayOffset],SYSUTCDATETIME())
        FROM #DemoLines WHERE [SequenceNo]=@SequenceNo;
        SET @Carrier=CASE WHEN @SequenceNo IN (4,5,6) THEN N'نمونهٔ غیرواقعی' ELSE NULL END;
        SET @TrackingCode=CASE WHEN @SequenceNo IN (4,5,6) THEN CONCAT(N'DEMO-TRK-20261112-',RIGHT(CONCAT(N'00',@SequenceNo),3)) ELSE NULL END;

        INSERT [Store].[Order]([IdProfile],[OrderNumber],[ExternalOrderKey],[Status],[PaymentMethod],[IdBank],[ItemCount],[TotalAmount],[OrderedAtUtc],[AddressSnapshotJson],[ShippingCarrier],[ShippingTrackingCode])
        VALUES(@ProfileId,@OrderNumber,@ExternalKey,@Status,N'demo',@BankId,1,@UnitAmount,@OrderedAtUtc,
               N'{"recipient":"دادهٔ آزمایشی","phone":"0000000000","street":"آدرس نمونهٔ غیرواقعی","postalCode":"0000000000"}',@Carrier,@TrackingCode);
        SET @OrderId=CONVERT(BIGINT,SCOPE_IDENTITY());
        INSERT @CreatedOrders VALUES(@SequenceNo,@OrderId);

        DECLARE @OrderItemId BIGINT;
        INSERT [Store].[OrderItem]([IdCustomerOrder],[IdProduct],[IdProductVariant],[ProductNameSnapshot],[IdSalesPlan],[Quantity],[UnitAmount],[OptionsJson])
        SELECT @OrderId,[ProductId],NULL,[ProductName],[PlanId],1,@UnitAmount,N'{}' FROM #DemoLines WHERE [SequenceNo]=@SequenceNo;
        SET @OrderItemId=CONVERT(BIGINT,SCOPE_IDENTITY());
        INSERT [Store].[OrderItemPriceReference]([IdOrderItem],[IdPriceVersion])
        SELECT @OrderItemId,[PriceVersionId] FROM #DemoLines WHERE [SequenceNo]=@SequenceNo;

        SET @SequenceNo+=1;
    END;

    INSERT [Store].[OrderShippingHistory]([IdOrder],[PreviousStatus],[NewStatus],[ActorUserKey],[ChangedAtUtc])
    SELECT o.[Id],t.PreviousStatus,t.NewStatus,N'demo-seed',DATEADD(MINUTE,5+t.StepNo,stored.[OrderedAtUtc])
    FROM (VALUES
        (2,1,N'pending-payment',N'payment-confirmed'),
        (3,1,N'pending-payment',N'payment-confirmed'),(3,2,N'payment-confirmed',N'warehouse-confirmed'),
        (4,1,N'pending-payment',N'payment-confirmed'),(4,2,N'payment-confirmed',N'warehouse-confirmed'),(4,3,N'warehouse-confirmed',N'shipped-to-post'),
        (5,1,N'pending-payment',N'payment-confirmed'),(5,2,N'payment-confirmed',N'warehouse-confirmed'),(5,3,N'warehouse-confirmed',N'shipped-to-post'),(5,4,N'shipped-to-post',N'in-transit'),
        (6,1,N'pending-payment',N'payment-confirmed'),(6,2,N'payment-confirmed',N'warehouse-confirmed'),(6,3,N'warehouse-confirmed',N'shipped-to-post'),(6,4,N'shipped-to-post',N'in-transit'),(6,5,N'in-transit',N'delivered'),
        (7,1,N'pending-payment',N'cancelled')
    ) t(SequenceNo,StepNo,PreviousStatus,NewStatus)
    JOIN @CreatedOrders o ON o.SequenceNo=t.SequenceNo
    JOIN [Store].[Order] stored ON stored.[Id]=o.[Id];

    INSERT [Store].[OrderShipmentTrackingHistory]([IdOrder],[PreviousCarrier],[PreviousTrackingCode],[Carrier],[TrackingCode],[ActorUserKey],[ChangedAtUtc])
    SELECT o.[Id],NULL,NULL,CONVERT(NVARCHAR(100),N'نمونهٔ غیرواقعی'),CONCAT(N'DEMO-TRK-20261112-',RIGHT(CONCAT(N'00',l.[SequenceNo]),3)),N'demo-seed',DATEADD(MINUTE,30,stored.[OrderedAtUtc])
    FROM @CreatedOrders o JOIN #DemoLines l ON l.SequenceNo=o.SequenceNo
    JOIN [Store].[Order] stored ON stored.[Id]=o.[Id]
    WHERE o.SequenceNo IN (4,5,6);

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NOT NULL
       AND NOT EXISTS(SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'063-order-page-demo-data')
        INSERT [Store].[SchemaMigration]([MigrationKey]) VALUES(N'063-order-page-demo-data');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
