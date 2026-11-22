SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @source BIGINT = (SELECT TOP (1) o.Id FROM [Store].[Order] o
        JOIN [Account].[Profile] p ON p.Id = o.IdProfile
        WHERE p.Email = N'demo-orders-20261112@invalid.example' AND o.PaymentMethod = N'demo'
          AND o.ExternalOrderKey LIKE N'demo-order-fixture-20261112-%'
          AND EXISTS (SELECT 1 FROM [Store].[OrderItem] i WHERE i.IdOrder = o.Id)
        ORDER BY o.Id);
    IF @source IS NULL THROW 51101, 'The existing demo order fixture is required.', 1;
    DECLARE @n INT = 1, @newId BIGINT, @key NVARCHAR(100), @number NVARCHAR(32);
    WHILE @n <= 5
    BEGIN
        SET @key = CONCAT(N'demo-order-warehouse-queue-', RIGHT(CONCAT(N'00', @n), 2));
        SET @number = CONCAT(N'DEMO-WH-QUEUE-', RIGHT(CONCAT(N'00', @n), 2));
        IF NOT EXISTS (SELECT 1 FROM [Store].[Order] WHERE ExternalOrderKey = @key)
        BEGIN
            INSERT [Store].[Order] (IdProfile, OrderNumber, ExternalOrderKey, Status, PaymentMethod, IdBank, ItemCount, TotalAmount, SubtotalAmount, CouponDiscountAmount, OrderedAtUtc, AddressSnapshotJson)
            SELECT IdProfile, @number, @key, N'payment-confirmed', N'demo', IdBank, ItemCount, TotalAmount, TotalAmount, 0,
                   DATEADD(MINUTE, -@n, SYSUTCDATETIME()), AddressSnapshotJson
            FROM [Store].[Order] WHERE Id = @source;
            SET @newId = SCOPE_IDENTITY();
            DECLARE @itemMap TABLE (OldId bigint NOT NULL, NewId bigint NOT NULL);
            MERGE [Store].[OrderItem] AS target
            USING (SELECT Id, IdProduct, IdVariant, IdSalesPlan, Quantity, IdPriceVersion FROM [Store].[OrderItem] WHERE IdOrder = @source) AS source
            ON 1 = 0
            WHEN NOT MATCHED THEN INSERT (IdOrder, IdProduct, IdVariant, IdSalesPlan, Quantity, IdPriceVersion)
                VALUES (@newId, source.IdProduct, source.IdVariant, source.IdSalesPlan, source.Quantity, source.IdPriceVersion)
            OUTPUT source.Id, inserted.Id INTO @itemMap (OldId, NewId);
            INSERT [Store].[OrderItemPriceReference] (IdOrderItem, IdPriceVersion)
            SELECT mapping.NewId, reference.IdPriceVersion FROM @itemMap mapping
            JOIN [Store].[OrderItemPriceReference] reference ON reference.IdOrderItem = mapping.OldId;
            DELETE FROM @itemMap;
        END
    SET @n += 1;
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
