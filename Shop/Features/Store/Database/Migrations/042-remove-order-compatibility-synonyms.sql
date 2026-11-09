-- Run only after every application instance and external consumer uses Store tables.
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'042-remove-order-compatibility-synonyms')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'041-move-orders-and-coupons-to-store')
        THROW 51042, 'Migration 041 must run first.', 1;
    IF OBJECT_ID(N'[Store].[Order]', N'U') IS NULL OR OBJECT_ID(N'[Store].[OrderItem]', N'U') IS NULL
       OR OBJECT_ID(N'[Store].[Coupon]', N'U') IS NULL OR OBJECT_ID(N'[Store].[CouponUsage]', N'U') IS NULL
        THROW 51042, 'The target order and coupon tables must exist.', 1;
    IF (SELECT COUNT(*) FROM sys.synonyms WHERE schema_id = SCHEMA_ID(N'Account') AND
        ((name = N'CustomerOrder' AND base_object_name = N'[Store].[Order]') OR
         (name = N'CustomerOrderItem' AND base_object_name = N'[Store].[OrderItem]') OR
         (name = N'Coupon' AND base_object_name = N'[Store].[Coupon]') OR
         (name = N'CouponUsage' AND base_object_name = N'[Store].[CouponUsage]'))) <> 4
        THROW 51042, 'The expected compatibility synonyms are missing or point elsewhere.', 1;

    DROP SYNONYM [Account].[CustomerOrder];
    DROP SYNONYM [Account].[CustomerOrderItem];
    DROP SYNONYM [Account].[Coupon];
    DROP SYNONYM [Account].[CouponUsage];
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'042-remove-order-compatibility-synonyms');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
