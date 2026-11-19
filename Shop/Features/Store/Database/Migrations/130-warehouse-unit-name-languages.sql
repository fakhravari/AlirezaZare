SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
    THROW 51300, 'Store.SchemaMigration is required before migration 130.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'130-warehouse-unit-name-languages')
BEGIN
    IF COL_LENGTH(N'Warehouse.Unit', N'NameFa') IS NULL AND COL_LENGTH(N'Warehouse.Unit', N'Name') IS NOT NULL
    BEGIN
        DECLARE @nameCheck sysname, @dropNameCheck nvarchar(max);
        DECLARE unit_checks CURSOR LOCAL FAST_FORWARD FOR
            SELECT cc.name FROM sys.check_constraints cc
            WHERE cc.parent_object_id = OBJECT_ID(N'[Warehouse].[Unit]');
        OPEN unit_checks;
        FETCH NEXT FROM unit_checks INTO @nameCheck;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @dropNameCheck = N'ALTER TABLE [Warehouse].[Unit] DROP CONSTRAINT ' + QUOTENAME(@nameCheck) + N';';
            EXEC sys.sp_executesql @dropNameCheck;
            FETCH NEXT FROM unit_checks INTO @nameCheck;
        END;
        CLOSE unit_checks;
        DEALLOCATE unit_checks;
        EXEC sys.sp_rename N'Warehouse.Unit.Name', N'NameFa', N'COLUMN';
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ADD CONSTRAINT [CK_Warehouse_Unit_NameFa_NotEmpty] CHECK (LEN(LTRIM(RTRIM([NameFa]))) > 0);';
    END;

    IF COL_LENGTH(N'Warehouse.Unit', N'NameFa') IS NULL
        THROW 51301, 'Warehouse.Unit.Name or NameFa must exist.', 1;

    IF COL_LENGTH(N'Warehouse.Unit', N'NameEn') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] ADD [NameEn] NVARCHAR(100) NULL;';

    DECLARE @index sysname, @sql nvarchar(max);
    DECLARE code_constraints CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT kc.name
        FROM sys.key_constraints kc
        JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE kc.parent_object_id = OBJECT_ID(N'[Warehouse].[Unit]') AND c.name = N'Code';
    OPEN code_constraints;
    FETCH NEXT FROM code_constraints INTO @index;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'ALTER TABLE [Warehouse].[Unit] DROP CONSTRAINT ' + QUOTENAME(@index) + N';';
        EXEC sys.sp_executesql @sql;
        FETCH NEXT FROM code_constraints INTO @index;
    END;
    CLOSE code_constraints;
    DEALLOCATE code_constraints;

    DECLARE code_indexes CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT i.name
        FROM sys.indexes i
        JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'[Warehouse].[Unit]') AND c.name = N'Code' AND i.is_primary_key = 0 AND i.is_unique_constraint = 0;
    OPEN code_indexes;
    FETCH NEXT FROM code_indexes INTO @index;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'DROP INDEX ' + QUOTENAME(@index) + N' ON [Warehouse].[Unit];';
        EXEC sys.sp_executesql @sql;
        FETCH NEXT FROM code_indexes INTO @index;
    END;
    CLOSE code_indexes;
    DEALLOCATE code_indexes;

    IF COL_LENGTH(N'Warehouse.Unit', N'Code') IS NOT NULL
        EXEC sys.sp_executesql N'ALTER TABLE [Warehouse].[Unit] DROP COLUMN [Code];';

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'130-warehouse-unit-name-languages');
END;

COMMIT TRANSACTION;
