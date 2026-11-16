SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'094-store-brand-display-setting')
BEGIN
    IF COL_LENGTH(N'Store.Brand', N'ShowInStore') IS NULL
        THROW 51094, 'Brand display migration is recorded but the column is missing.', 1;
    COMMIT TRANSACTION;
    RETURN;
END;

IF COL_LENGTH(N'Store.Brand', N'ShowInStore') IS NULL
    ALTER TABLE [Store].[Brand] ADD [ShowInStore] BIT NOT NULL
        CONSTRAINT [DF_Store_Brand_ShowInStore] DEFAULT (1) WITH VALUES;

INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'094-store-brand-display-setting');

COMMIT TRANSACTION;
