SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'041-move-orders-and-coupons-to-store')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Account].[CustomerOrder]', N'U') IS NULL
       OR OBJECT_ID(N'[Account].[CustomerOrderItem]', N'U') IS NULL
       OR OBJECT_ID(N'[Account].[Coupon]', N'U') IS NULL
       OR OBJECT_ID(N'[Account].[CouponUsage]', N'U') IS NULL
        THROW 51041, 'Expected account order and coupon tables are missing.', 1;

    IF OBJECT_ID(N'[Store].[Order]') IS NOT NULL OR OBJECT_ID(N'[Store].[OrderItem]') IS NOT NULL
       OR OBJECT_ID(N'[Store].[Coupon]') IS NOT NULL OR OBJECT_ID(N'[Store].[CouponUsage]') IS NOT NULL
        THROW 51041, 'A destination order or coupon object already exists.', 1;

    IF OBJECT_ID(N'[Account].[Order]') IS NOT NULL OR OBJECT_ID(N'[Account].[OrderItem]') IS NOT NULL
        THROW 51041, 'Temporary order names in Account already exist.', 1;

    EXEC sys.sp_rename N'[Account].[CustomerOrder]', N'Order';
    EXEC sys.sp_rename N'[Account].[CustomerOrderItem]', N'OrderItem';
    ALTER SCHEMA [Store] TRANSFER [Account].[Order];
    ALTER SCHEMA [Store] TRANSFER [Account].[OrderItem];
    ALTER SCHEMA [Store] TRANSFER [Account].[Coupon];
    ALTER SCHEMA [Store] TRANSFER [Account].[CouponUsage];

    -- Transitional names keep the previous application version working during deployment.
    CREATE SYNONYM [Account].[CustomerOrder] FOR [Store].[Order];
    CREATE SYNONYM [Account].[CustomerOrderItem] FOR [Store].[OrderItem];
    CREATE SYNONYM [Account].[Coupon] FOR [Store].[Coupon];
    CREATE SYNONYM [Account].[CouponUsage] FOR [Store].[CouponUsage];

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'041-move-orders-and-coupons-to-store');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
