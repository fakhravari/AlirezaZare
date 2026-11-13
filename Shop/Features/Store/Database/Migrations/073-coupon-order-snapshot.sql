SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1 FROM [Store].[SchemaMigration]
        WHERE [MigrationKey] = N'073-coupon-order-snapshot'
    )
    BEGIN
        IF COL_LENGTH(N'Store.Order', N'IdCoupon') IS NULL
            EXEC sys.sp_executesql N'ALTER TABLE [Store].[Order] ADD [IdCoupon] BIGINT NULL;';

        IF COL_LENGTH(N'Store.Order', N'SubtotalAmount') IS NULL
            EXEC sys.sp_executesql N'ALTER TABLE [Store].[Order] ADD [SubtotalAmount] DECIMAL(19,0) NOT NULL
                CONSTRAINT [DF_Store_Order_SubtotalAmount] DEFAULT (0);';

        IF COL_LENGTH(N'Store.Order', N'CouponDiscountAmount') IS NULL
            EXEC sys.sp_executesql N'ALTER TABLE [Store].[Order] ADD [CouponDiscountAmount] DECIMAL(19,0) NOT NULL
                CONSTRAINT [DF_Store_Order_CouponDiscountAmount] DEFAULT (0);';

        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE [name] = N'FK_Store_Order_Coupon'
              AND [parent_object_id] = OBJECT_ID(N'[Store].[Order]')
        )
            EXEC sys.sp_executesql N'ALTER TABLE [Store].[Order] WITH CHECK ADD CONSTRAINT [FK_Store_Order_Coupon]
                FOREIGN KEY ([IdCoupon]) REFERENCES [Store].[Coupon] ([Id]);';

        EXEC sys.sp_executesql N'
            UPDATE [Store].[Order]
            SET [SubtotalAmount] = [TotalAmount]
            WHERE [SubtotalAmount] = 0 AND [CouponDiscountAmount] = 0;';

        IF NOT EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE [name] = N'CK_Store_Order_CouponDiscountAmount'
              AND [parent_object_id] = OBJECT_ID(N'[Store].[Order]')
        )
            EXEC sys.sp_executesql N'ALTER TABLE [Store].[Order] WITH CHECK ADD CONSTRAINT [CK_Store_Order_CouponDiscountAmount]
                CHECK ([CouponDiscountAmount] >= 0);';

        IF NOT EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE [name] = N'CK_Store_Order_CouponAmounts'
              AND [parent_object_id] = OBJECT_ID(N'[Store].[Order]')
        )
            EXEC sys.sp_executesql N'ALTER TABLE [Store].[Order] WITH CHECK ADD CONSTRAINT [CK_Store_Order_CouponAmounts]
                CHECK ([SubtotalAmount] >= [CouponDiscountAmount]);';

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'073-coupon-order-snapshot');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
