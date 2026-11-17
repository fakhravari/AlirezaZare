SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'100-product-store-publication')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'IsStoreEnabled') IS NULL
           OR COL_LENGTH(N'Warehouse.Product', N'IsCatalogActive') IS NULL
            THROW 51100, 'Both legacy product publication columns must exist before migration 100.', 1;

        -- Preserve the old public visibility condition for every existing product.
        EXEC sys.sp_executesql N'
            UPDATE [Warehouse].[Product]
            SET [IsStoreEnabled] = CASE WHEN [IsStoreEnabled] = 1 AND [IsCatalogActive] = 1 THEN 1 ELSE 0 END;';

        DECLARE @defaultConstraint sysname;
        SELECT @defaultConstraint = dc.[name]
        FROM sys.default_constraints AS dc
        JOIN sys.columns AS c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
        WHERE dc.[parent_object_id] = OBJECT_ID(N'[Warehouse].[Product]') AND c.[name] = N'IsCatalogActive';
        IF @defaultConstraint IS NOT NULL
        BEGIN
            DECLARE @dropDefaultSql nvarchar(max) = N'ALTER TABLE [Warehouse].[Product] DROP CONSTRAINT ' + QUOTENAME(@defaultConstraint);
            EXEC sys.sp_executesql @dropDefaultSql;
        END;

        -- Unrecognized dependencies make DROP fail and roll back the entire migration.
        ALTER TABLE [Warehouse].[Product] DROP COLUMN [IsCatalogActive];

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'100-product-store-publication');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
