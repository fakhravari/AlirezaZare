SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
    THROW 51370, 'Store.SchemaMigration is required before migration 137.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'137-remove-purchase-option-price-history')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'136-remove-purchase-option-tables')
        THROW 51371, 'Migration 136 must complete before migration 137.', 1;

    IF COL_LENGTH(N'Store.PriceVersion', N'IdOptionValue') IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM [Store].[PriceVersion]
            WHERE [IdOptionValue] IS NULL AND [PriceAmount] < 0
        )
            THROW 51372, 'A non-option price has a negative amount; migration 137 made no changes.', 1;

        IF EXISTS (
            SELECT 1
            FROM [Store].[PriceVersion]
            WHERE [IdOptionValue] IS NULL AND [IsActive] = 1
            GROUP BY [IdProduct], [IdSalesPlan], [IdVariant], [IdAccessory]
            HAVING COUNT_BIG(*) > 1
        )
            THROW 51373, 'Active price scopes collide after option history removal; migration 137 made no changes.', 1;

        DELETE reference
        FROM [Store].[OrderItemPriceReference] reference
        INNER JOIN [Store].[PriceVersion] version ON version.[Id] = reference.[IdPriceVersion]
        WHERE version.[IdOptionValue] IS NOT NULL;

        UPDATE version
        SET [IdPreviousVersion] = NULL
        FROM [Store].[PriceVersion] version
        INNER JOIN [Store].[PriceVersion] previous ON previous.[Id] = version.[IdPreviousVersion]
        WHERE previous.[IdOptionValue] IS NOT NULL;

        DELETE FROM [Store].[PriceVersion]
        WHERE [IdOptionValue] IS NOT NULL;

        DROP INDEX IF EXISTS [IX_Store_PriceVersion_TypedLookup] ON [Store].[PriceVersion];
        DROP INDEX IF EXISTS [UX_Store_PriceVersion_ActiveTypedScope] ON [Store].[PriceVersion];

        IF OBJECT_ID(N'[Store].[CK_Store_PriceVersion_SingleTypedReference]', N'C') IS NOT NULL
            ALTER TABLE [Store].[PriceVersion] DROP CONSTRAINT [CK_Store_PriceVersion_SingleTypedReference];
        IF OBJECT_ID(N'[Store].[CK_PriceVersion_AmountByComponent]', N'C') IS NOT NULL
            ALTER TABLE [Store].[PriceVersion] DROP CONSTRAINT [CK_PriceVersion_AmountByComponent];

        ALTER TABLE [Store].[PriceVersion] DROP COLUMN [IdOptionValue];
        ALTER TABLE [Store].[PriceVersion] WITH CHECK ADD CONSTRAINT [CK_Store_PriceVersion_SingleTypedReference]
            CHECK ((CASE WHEN [IdVariant] IS NULL THEN 0 ELSE 1 END + CASE WHEN [IdAccessory] IS NULL THEN 0 ELSE 1 END) <= 1);
        ALTER TABLE [Store].[PriceVersion] WITH CHECK ADD CONSTRAINT [CK_PriceVersion_AmountByComponent]
            CHECK ([PriceAmount] >= 0);

        CREATE NONCLUSTERED INDEX [IX_Store_PriceVersion_TypedLookup]
            ON [Store].[PriceVersion] ([IdProduct], [IdSalesPlan], [IdVariant], [IdAccessory], [Id]);
        CREATE UNIQUE NONCLUSTERED INDEX [UX_Store_PriceVersion_ActiveTypedScope]
            ON [Store].[PriceVersion] ([IdProduct], [IdSalesPlan], [IdVariant], [IdAccessory])
            WHERE [IsActive] = 1;
    END;

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'137-remove-purchase-option-price-history');
END;

COMMIT TRANSACTION;
