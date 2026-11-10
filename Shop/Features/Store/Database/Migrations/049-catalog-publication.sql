SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'049-catalog-publication')
BEGIN
    IF COL_LENGTH(N'Store.Brand', N'IsActive') IS NULL OR
       COL_LENGTH(N'Store.AttributeDefinition', N'IsActive') IS NULL
        THROW 51049, 'Catalog publication migration is recorded but incomplete.', 1;
    COMMIT TRANSACTION;
    RETURN;
END;

IF COL_LENGTH(N'Store.Brand', N'IsActive') IS NULL
    ALTER TABLE [Store].[Brand] ADD [IsActive] BIT NOT NULL
        CONSTRAINT [DF_Store_Brand_IsActive] DEFAULT (1) WITH VALUES;

IF COL_LENGTH(N'Store.AttributeDefinition', N'IsActive') IS NULL
    ALTER TABLE [Store].[AttributeDefinition] ADD [IsActive] BIT NOT NULL
        CONSTRAINT [DF_Store_AttributeDefinition_IsActive] DEFAULT (1) WITH VALUES;

INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'049-catalog-publication');

COMMIT TRANSACTION;
