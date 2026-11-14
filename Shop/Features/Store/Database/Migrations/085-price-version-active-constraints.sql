SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[PriceVersion]', N'U') IS NULL
        THROW 51085, 'Store.PriceVersion must exist before migration 085.', 1;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'085-price-version-active-constraints')
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM [Store].[PriceVersion]
            WHERE [IsActive] = 1
            GROUP BY [IdProduct], [IdSalesPlan], [IdProductVariant], [IdOptionValue], [IdAccessory], [IdOrderItem]
            HAVING COUNT_BIG(*) > 1
        )
            THROW 51086, 'Duplicate active Store.PriceVersion rows exist for one typed reference.', 1;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_Store_PriceVersion_ActiveTypedScope' AND [object_id] = OBJECT_ID(N'[Store].[PriceVersion]'))
            CREATE UNIQUE INDEX [UX_Store_PriceVersion_ActiveTypedScope]
                ON [Store].[PriceVersion]([IdProduct], [IdSalesPlan], [IdProductVariant], [IdOptionValue], [IdAccessory], [IdOrderItem])
                WHERE [IsActive] = 1;

        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Store_PriceVersion_NonNegativeAmount' AND [parent_object_id] = OBJECT_ID(N'[Store].[PriceVersion]'))
            ALTER TABLE [Store].[PriceVersion] WITH CHECK ADD CONSTRAINT [CK_Store_PriceVersion_NonNegativeAmount] CHECK ([PriceAmount] >= 0);
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Store_PriceVersion_DiscountRange' AND [parent_object_id] = OBJECT_ID(N'[Store].[PriceVersion]'))
            ALTER TABLE [Store].[PriceVersion] WITH CHECK ADD CONSTRAINT [CK_Store_PriceVersion_DiscountRange] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] < 100);

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'085-price-version-active-constraints');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
