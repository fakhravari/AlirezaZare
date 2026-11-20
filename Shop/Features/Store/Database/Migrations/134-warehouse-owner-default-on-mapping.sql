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
    THROW 51340, 'Store.SchemaMigration is required before migration 134.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'134-warehouse-owner-default-on-mapping')
BEGIN
    IF COL_LENGTH(N'Warehouse.Warehouse', N'IdOwnerSalesPlanReference') IS NULL
        THROW 51341, 'Warehouse default-owner column is missing before migration 134.', 1;
    IF OBJECT_ID(N'[Warehouse].[WarehouseStockOwner]', N'U') IS NULL
        THROW 51342, 'WarehouseStockOwner is required before migration 134.', 1;
    IF COL_LENGTH(N'Warehouse.WarehouseStockOwner', N'IsDefault') IS NOT NULL
        THROW 51343, 'WarehouseStockOwner.IsDefault already exists without migration marker 134.', 1;
    IF EXISTS (
        SELECT 1 FROM [Warehouse].[Warehouse] w
        LEFT JOIN [Warehouse].[WarehouseStockOwner] o
          ON o.[IdWarehouse] = w.[Id] AND o.[IdStockOwner] = w.[IdOwnerSalesPlanReference]
        WHERE w.[IdOwnerSalesPlanReference] IS NULL OR o.[IdWarehouse] IS NULL
    )
        THROW 51344, 'A warehouse default owner has no matching owner mapping; migration 134 made no changes.', 1;

    ALTER TABLE [Warehouse].[WarehouseStockOwner]
      ADD [IsDefault] bit NOT NULL CONSTRAINT [DF_WarehouseStockOwner_IsDefault] DEFAULT (0) WITH VALUES;
    EXEC sys.sp_executesql N'
      UPDATE o SET [IsDefault] = 1
        FROM [Warehouse].[WarehouseStockOwner] o
        INNER JOIN [Warehouse].[Warehouse] w
          ON w.[Id] = o.[IdWarehouse] AND w.[IdOwnerSalesPlanReference] = o.[IdStockOwner];';

    DECLARE @dropSql nvarchar(max) = N'';
    SELECT @dropSql += N'ALTER TABLE [Warehouse].[Warehouse] DROP CONSTRAINT ' + QUOTENAME(fk.[name]) + N';'
      FROM sys.foreign_keys fk
      INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
      INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
      WHERE fk.parent_object_id = OBJECT_ID(N'[Warehouse].[Warehouse]') AND c.[name] = N'IdOwnerSalesPlanReference';
    IF @dropSql <> N'' EXEC sys.sp_executesql @dropSql;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Warehouse_Warehouse_SalesOwnerRequired' AND [parent_object_id] = OBJECT_ID(N'[Warehouse].[Warehouse]'))
        ALTER TABLE [Warehouse].[Warehouse] DROP CONSTRAINT [CK_Warehouse_Warehouse_SalesOwnerRequired];
    ALTER TABLE [Warehouse].[Warehouse] DROP COLUMN [IdOwnerSalesPlanReference];
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_WarehouseStockOwner_OneDefaultPerWarehouse]
      ON [Warehouse].[WarehouseStockOwner] ([IdWarehouse]) WHERE [IsDefault] = 1;';

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'134-warehouse-owner-default-on-mapping');
END;

COMMIT TRANSACTION;
