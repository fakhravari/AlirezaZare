SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[PriceVersion]', N'U') IS NULL
        THROW 51083, 'Store.PriceVersion must exist before migration 083.', 1;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'083-price-version-remove-kind')
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Store_PriceVersion_TargetMatchesKind' AND parent_object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
            ALTER TABLE [Store].[PriceVersion] DROP CONSTRAINT [CK_Store_PriceVersion_TargetMatchesKind];
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Store_PriceVersion_TypedLookup' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
            DROP INDEX [IX_Store_PriceVersion_TypedLookup] ON [Store].[PriceVersion];
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Store_PriceVersion_ActiveTypedScope' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
            DROP INDEX [UX_Store_PriceVersion_ActiveTypedScope] ON [Store].[PriceVersion];
        IF COL_LENGTH(N'Store.PriceVersion', N'PriceKind') IS NOT NULL
            ALTER TABLE [Store].[PriceVersion] DROP COLUMN [PriceKind];
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Store_PriceVersion_TypedLookup' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
            CREATE INDEX [IX_Store_PriceVersion_TypedLookup]
                ON [Store].[PriceVersion]([IdProduct],[IdSalesPlan],[IdProductVariant],[IdOptionValue],[IdAccessory],[IdOrderItem],[Id]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Store_PriceVersion_ActiveTypedScope' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
            CREATE UNIQUE INDEX [UX_Store_PriceVersion_ActiveTypedScope]
                ON [Store].[PriceVersion]([IdProduct],[IdSalesPlan],[IdProductVariant],[IdOptionValue],[IdAccessory],[IdOrderItem])
                WHERE [IsActive]=1;
        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'083-price-version-remove-kind');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
