SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[PriceVersion]', N'U') IS NULL
        THROW 51082, 'Store.PriceVersion must exist before migration 082.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'082-price-version-typed-references')
    BEGIN

    IF COL_LENGTH(N'Store.PriceVersion', N'IdProductVariant') IS NULL
        ALTER TABLE [Store].[PriceVersion] ADD [IdProductVariant] BIGINT NULL;
    IF COL_LENGTH(N'Store.PriceVersion', N'IdOptionValue') IS NULL
        ALTER TABLE [Store].[PriceVersion] ADD [IdOptionValue] BIGINT NULL;
    IF COL_LENGTH(N'Store.PriceVersion', N'IdAccessory') IS NULL
        ALTER TABLE [Store].[PriceVersion] ADD [IdAccessory] BIGINT NULL;
    IF COL_LENGTH(N'Store.PriceVersion', N'IdOrderItem') IS NULL
        ALTER TABLE [Store].[PriceVersion] ADD [IdOrderItem] BIGINT NULL;

    IF COL_LENGTH(N'Store.PriceVersion', N'TargetId') IS NOT NULL
        EXEC(N'UPDATE [Store].[PriceVersion] SET [IdProductVariant]=[TargetId] WHERE [PriceKind]=N''variant'' AND [IdProductVariant] IS NULL;
              UPDATE [Store].[PriceVersion] SET [IdOptionValue]=[TargetId] WHERE [PriceKind]=N''option'' AND [IdOptionValue] IS NULL;
              UPDATE [Store].[PriceVersion] SET [IdAccessory]=[TargetId] WHERE [PriceKind]=N''accessory'' AND [IdAccessory] IS NULL;
              UPDATE [Store].[PriceVersion] SET [IdOrderItem]=[TargetId] WHERE [PriceKind]=N''order-snapshot'' AND [IdOrderItem] IS NULL;');

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Store_PriceVersion_ProductVariant')
        ALTER TABLE [Store].[PriceVersion] DROP CONSTRAINT [FK_Store_PriceVersion_ProductVariant];
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Store_PriceVersion_ProductVariant_Product')
        EXEC(N'ALTER TABLE [Store].[PriceVersion] ADD CONSTRAINT [FK_Store_PriceVersion_ProductVariant_Product] FOREIGN KEY([IdProductVariant],[IdProduct]) REFERENCES [Warehouse].[ProductVariant]([Id],[IdProduct]);');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Store_PriceVersion_OptionValue')
        EXEC(N'ALTER TABLE [Store].[PriceVersion] ADD CONSTRAINT [FK_Store_PriceVersion_OptionValue] FOREIGN KEY([IdOptionValue]) REFERENCES [Store].[ProductPurchaseOptionValue]([Id]);');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Store_PriceVersion_Accessory')
        EXEC(N'ALTER TABLE [Store].[PriceVersion] ADD CONSTRAINT [FK_Store_PriceVersion_Accessory] FOREIGN KEY([IdAccessory]) REFERENCES [Store].[Accessory]([Id]);');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Store_PriceVersion_OrderItem')
        EXEC(N'ALTER TABLE [Store].[PriceVersion] ADD CONSTRAINT [FK_Store_PriceVersion_OrderItem] FOREIGN KEY([IdOrderItem]) REFERENCES [Store].[OrderItem]([Id]);');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Store_PriceVersion_TargetMatchesKind')
        EXEC(N'ALTER TABLE [Store].[PriceVersion] WITH CHECK ADD CONSTRAINT [CK_Store_PriceVersion_TargetMatchesKind] CHECK (
            ([PriceKind]=N''product'' AND [IdProductVariant] IS NULL AND [IdOptionValue] IS NULL AND [IdAccessory] IS NULL AND [IdOrderItem] IS NULL) OR
            ([PriceKind]=N''variant'' AND [IdProductVariant] IS NOT NULL AND [IdOptionValue] IS NULL AND [IdAccessory] IS NULL AND [IdOrderItem] IS NULL) OR
            ([PriceKind]=N''option'' AND [IdOptionValue] IS NOT NULL AND [IdProductVariant] IS NULL AND [IdAccessory] IS NULL AND [IdOrderItem] IS NULL) OR
            ([PriceKind]=N''accessory'' AND [IdAccessory] IS NOT NULL AND [IdProductVariant] IS NULL AND [IdOptionValue] IS NULL AND [IdOrderItem] IS NULL) OR
            ([PriceKind]=N''order-snapshot'' AND [IdOrderItem] IS NOT NULL AND [IdProductVariant] IS NULL AND [IdOptionValue] IS NULL AND [IdAccessory] IS NULL)
        );');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Store_PriceVersion_Lookup' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
        DROP INDEX [IX_Store_PriceVersion_Lookup] ON [Store].[PriceVersion];
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Store_PriceVersion_ActiveScope' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
        DROP INDEX [UX_Store_PriceVersion_ActiveScope] ON [Store].[PriceVersion];
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Store_PriceVersion_TypedLookup' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
        EXEC(N'CREATE INDEX [IX_Store_PriceVersion_TypedLookup]
            ON [Store].[PriceVersion]([IdProduct],[IdSalesPlan],[PriceKind],[IdProductVariant],[IdOptionValue],[IdAccessory],[IdOrderItem],[Id]);');
    IF COL_LENGTH(N'Store.PriceVersion', N'TargetId') IS NOT NULL
        ALTER TABLE [Store].[PriceVersion] DROP COLUMN [TargetId];
    IF COL_LENGTH(N'Store.PriceVersion', N'ScopeKey') IS NOT NULL
        ALTER TABLE [Store].[PriceVersion] DROP COLUMN [ScopeKey];

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Store_PriceVersion_ActiveTypedScope' AND object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
        EXEC(N'CREATE UNIQUE INDEX [UX_Store_PriceVersion_ActiveTypedScope]
            ON [Store].[PriceVersion]([IdProduct],[IdSalesPlan],[PriceKind],[IdProductVariant],[IdOptionValue],[IdAccessory],[IdOrderItem])
            WHERE [IsActive]=1;');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
    VALUES (N'082-price-version-typed-references');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
