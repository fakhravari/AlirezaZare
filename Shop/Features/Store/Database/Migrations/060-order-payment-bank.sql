SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH(N'[Store].[Order]', N'IdBank') IS NULL
        ALTER TABLE [Store].[Order] ADD [IdBank] BIGINT NULL;

    IF NOT EXISTS (SELECT 1 FROM [Store].[Bank] WHERE [Code] = N'zarinpal')
        THROW 51060, 'The Zarinpal bank catalog row is required before linking orders.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Store_Order_Bank')
        ALTER TABLE [Store].[Order] ADD CONSTRAINT [FK_Store_Order_Bank]
            FOREIGN KEY ([IdBank]) REFERENCES [Store].[Bank] ([Id]);

    EXEC(N'UPDATE orders
        SET [IdBank] = bank.[Id]
        FROM [Store].[Order] orders
        INNER JOIN [Store].[Bank] bank ON bank.[Code] = N''zarinpal''
        WHERE orders.[IdBank] IS NULL;');

    IF EXISTS (SELECT 1 FROM [Store].[Order] WHERE [IdBank] IS NULL)
        THROW 51061, 'Every store order must reference a bank before IdBank becomes required.', 1;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Store].[Order]') AND name = N'IX_Store_Order_IdBank')
        DROP INDEX [IX_Store_Order_IdBank] ON [Store].[Order];

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Store].[Order]') AND name = N'IdBank' AND is_nullable = 1)
        ALTER TABLE [Store].[Order] ALTER COLUMN [IdBank] BIGINT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Store].[Order]') AND name = N'IX_Store_Order_IdBank')
        CREATE INDEX [IX_Store_Order_IdBank] ON [Store].[Order] ([IdBank]);

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'060-order-payment-bank')
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'060-order-payment-bank');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
