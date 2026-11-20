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
    THROW 51350, 'Store.SchemaMigration is required before migration 135.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'135-drop-legacy-sales-plan-reference-columns')
BEGIN
    IF OBJECT_ID(N'[Warehouse].[SalesPlanReference]', N'U') IS NULL
        THROW 51351, 'Warehouse.SalesPlanReference is required before migration 135.', 1;
    IF COL_LENGTH(N'Warehouse.SalesPlanReference', N'SalesPlanKey') IS NULL
       OR COL_LENGTH(N'Warehouse.SalesPlanReference', N'Name') IS NULL
       OR COL_LENGTH(N'Warehouse.SalesPlanReference', N'IsDefault') IS NULL
       OR COL_LENGTH(N'Warehouse.SalesPlanReference', N'IsEnabled') IS NULL
        THROW 51352, 'One or more legacy columns are missing before migration 135.', 1;

    IF EXISTS (
        SELECT 1 FROM [Warehouse].[SalesPlanReference]
        WHERE CONVERT(varbinary(max), [StoreSalesPlanKey]) <> CONVERT(varbinary(max), [SalesPlanKey])
           OR CONVERT(varbinary(max), [DisplayName]) <> CONVERT(varbinary(max), [Name])
           OR [IsRegular] <> [IsDefault]
           OR [IsActive] <> [IsEnabled]
    )
        THROW 51353, 'Legacy SalesPlanReference columns differ from their active counterparts; migration 135 made no changes.', 1;

    IF EXISTS (
        SELECT 1 FROM sys.index_columns ic
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE ic.object_id = OBJECT_ID(N'[Warehouse].[SalesPlanReference]')
          AND c.[name] IN (N'SalesPlanKey', N'Name', N'IsDefault', N'IsEnabled')
    ) OR EXISTS (
        SELECT 1 FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[Warehouse].[SalesPlanReference]')
          AND c.[name] IN (N'SalesPlanKey', N'Name', N'IsDefault', N'IsEnabled')
    ) OR EXISTS (
        SELECT 1 FROM sys.check_constraints cc
        WHERE cc.parent_object_id = OBJECT_ID(N'[Warehouse].[SalesPlanReference]')
          AND (cc.[definition] LIKE N'%[[]SalesPlanKey[]]%' OR cc.[definition] LIKE N'%[[]Name[]]%'
            OR cc.[definition] LIKE N'%[[]IsDefault[]]%' OR cc.[definition] LIKE N'%[[]IsEnabled[]]%')
    ) OR EXISTS (
        SELECT 1 FROM sys.triggers tr
        WHERE tr.parent_id = OBJECT_ID(N'[Warehouse].[SalesPlanReference]')
          AND (OBJECT_DEFINITION(tr.object_id) LIKE N'%[[]SalesPlanKey[]]%' OR OBJECT_DEFINITION(tr.object_id) LIKE N'%[[]Name[]]%'
            OR OBJECT_DEFINITION(tr.object_id) LIKE N'%[[]IsDefault[]]%' OR OBJECT_DEFINITION(tr.object_id) LIKE N'%[[]IsEnabled[]]%')
    )
        THROW 51354, 'A database constraint, index, or trigger uses a legacy column; migration 135 made no changes.', 1;

    ALTER TABLE [Warehouse].[SalesPlanReference]
      DROP COLUMN [SalesPlanKey], [Name], [IsDefault], [IsEnabled];

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'135-drop-legacy-sales-plan-reference-columns');
END;

COMMIT TRANSACTION;
