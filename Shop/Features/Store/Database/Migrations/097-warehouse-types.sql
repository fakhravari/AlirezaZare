SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'097-warehouse-types')
BEGIN
    COMMIT TRANSACTION;
    RETURN;
END;
IF EXISTS (SELECT 1 FROM [Warehouse].[Warehouse] GROUP BY [IdCompany], LTRIM(RTRIM([Name])) HAVING COUNT(*) > 1)
    THROW 51097, 'Duplicate warehouse names within a company must be resolved before migration.', 1;
IF EXISTS (SELECT 1 FROM [Warehouse].[Warehouse] WHERE [Name] IS NULL OR LTRIM(RTRIM([Name])) = N'')
    THROW 51097, 'Warehouse names must not be empty.', 1;
CREATE TABLE [Warehouse].[WarehouseType] (
    [Id] bigint IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse_WarehouseType] PRIMARY KEY,
    [Name] nvarchar(200) NOT NULL,
    CONSTRAINT [UQ_Warehouse_WarehouseType_Name] UNIQUE ([Name])
);
INSERT INTO [Warehouse].[WarehouseType] ([Name]) SELECT DISTINCT LTRIM(RTRIM([Name])) FROM [Warehouse].[Warehouse];
ALTER TABLE [Warehouse].[Warehouse] ADD [IdWarehouseType] bigint NULL;
EXEC(N'UPDATE w SET IdWarehouseType = t.Id FROM [Warehouse].[Warehouse] w JOIN [Warehouse].[WarehouseType] t ON t.Name = LTRIM(RTRIM(w.Name))');
EXEC(N'ALTER TABLE [Warehouse].[Warehouse] ALTER COLUMN [IdWarehouseType] bigint NOT NULL');
EXEC(N'ALTER TABLE [Warehouse].[Warehouse] ADD CONSTRAINT [FK_Warehouse_WarehouseType] FOREIGN KEY ([IdWarehouseType]) REFERENCES [Warehouse].[WarehouseType] ([Id])');
EXEC(N'CREATE UNIQUE INDEX [UX_Warehouse_Company_Type] ON [Warehouse].[Warehouse] ([IdCompany], [IdWarehouseType])');
INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'097-warehouse-types');
COMMIT TRANSACTION;
