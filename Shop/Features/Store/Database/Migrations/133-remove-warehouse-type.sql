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
    THROW 51330, 'Store.SchemaMigration is required before migration 133.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'133-remove-warehouse-type')
BEGIN
    IF OBJECT_ID(N'[Warehouse].[WarehouseType]', N'U') IS NULL
        THROW 51331, 'Warehouse.WarehouseType is missing before migration 133.', 1;
    IF OBJECT_ID(N'[Warehouse].[Warehouse]', N'U') IS NULL
        THROW 51332, 'Warehouse.Warehouse is required before migration 133.', 1;
    IF COL_LENGTH(N'Warehouse.Warehouse', N'IdType') IS NULL OR COL_LENGTH(N'Warehouse.Warehouse', N'Name') IS NULL
        THROW 51333, 'Warehouse rows must have both IdType and Name before migration 133.', 1;
    IF EXISTS (
        SELECT 1 FROM [Warehouse].[Warehouse] w
        LEFT JOIN [Warehouse].[WarehouseType] t ON t.[Id] = w.[IdType]
        WHERE t.[Id] IS NULL OR NULLIF(LTRIM(RTRIM(w.[Name])), N'') IS NULL
    )
        THROW 51334, 'Warehouse type links or direct warehouse names are incomplete; migration 133 made no changes.', 1;
    IF EXISTS (
        SELECT 1 FROM [Warehouse].[Warehouse]
        GROUP BY [IdCompany], [Name]
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51335, 'Duplicate warehouse names exist within a company; migration 133 made no changes.', 1;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_WarehouseType' AND [parent_object_id] = OBJECT_ID(N'[Warehouse].[Warehouse]'))
        ALTER TABLE [Warehouse].[Warehouse] DROP CONSTRAINT [FK_Warehouse_WarehouseType];
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'UX_Warehouse_Company_Type' AND [object_id] = OBJECT_ID(N'[Warehouse].[Warehouse]'))
        DROP INDEX [UX_Warehouse_Company_Type] ON [Warehouse].[Warehouse];

    ALTER TABLE [Warehouse].[Warehouse] DROP COLUMN [IdType];
    DROP TABLE [Warehouse].[WarehouseType];
    CREATE UNIQUE INDEX [UX_Warehouse_Company_Name] ON [Warehouse].[Warehouse] ([IdCompany], [Name]);

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'133-remove-warehouse-type');
END;

COMMIT TRANSACTION;
