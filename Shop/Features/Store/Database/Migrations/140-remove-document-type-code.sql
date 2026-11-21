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
    THROW 51400, 'Store.SchemaMigration is required.', 1;
IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'140-remove-document-type-code')
BEGIN
    IF OBJECT_ID(N'[Warehouse].[DocumentType]', N'U') IS NULL OR COL_LENGTH(N'Warehouse.DocumentType', N'Code') IS NULL
        THROW 51401, 'Expected legacy document type code is missing.', 1;
    IF COL_LENGTH(N'Warehouse.DocumentType', N'IsReturn') IS NOT NULL OR COL_LENGTH(N'Warehouse.DocumentType', N'IsSystemSalesDispatch') IS NOT NULL
        THROW 51402, 'Unexpected document type classification columns already exist.', 1;
    EXEC(N'IF (SELECT COUNT(*) FROM [Warehouse].[DocumentType] WHERE UPPER([Code]) = N''SALES-DISPATCH'') > 1 THROW 51403, ''More than one system sales dispatch type exists.'', 1;');
    IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(N'Warehouse.DocumentType') AND c.name=N'Code' AND i.name <> N'UQ_Warehouse_DocumentType_Code')
        THROW 51404, 'Unexpected index depends on the document type code.', 1;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'Warehouse.DocumentType') AND definition LIKE N'%Code%' AND name <> N'CK_Warehouse_DocumentType_Code_NotEmpty')
        THROW 51405, 'Unexpected check constraint depends on the document type code.', 1;
    IF EXISTS (SELECT 1 FROM sys.sql_modules WHERE definition LIKE N'%DocumentType%' AND definition LIKE N'%Code%')
        THROW 51406, 'A database module may depend on the document type code.', 1;

    ALTER TABLE [Warehouse].[DocumentType] ADD
        [IsReturn] BIT NOT NULL CONSTRAINT [DF_Warehouse_DocumentType_IsReturn] DEFAULT (0),
        [IsSystemSalesDispatch] BIT NOT NULL CONSTRAINT [DF_Warehouse_DocumentType_IsSystemSalesDispatch] DEFAULT (0);
    EXEC(N'UPDATE [Warehouse].[DocumentType] SET [IsReturn] = CASE WHEN LOWER([Code]) LIKE N''%return%'' OR [Name] LIKE N''%مرجوع%'' THEN 1 ELSE 0 END, [IsSystemSalesDispatch] = CASE WHEN UPPER([Code]) = N''SALES-DISPATCH'' THEN 1 ELSE 0 END;');
    EXEC(N'CREATE UNIQUE INDEX [UX_Warehouse_DocumentType_SystemSalesDispatch] ON [Warehouse].[DocumentType] ([IsSystemSalesDispatch]) WHERE [IsSystemSalesDispatch] = 1;');
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'Warehouse.DocumentType') AND name=N'UQ_Warehouse_DocumentType_Code')
        ALTER TABLE [Warehouse].[DocumentType] DROP CONSTRAINT [UQ_Warehouse_DocumentType_Code];
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'Warehouse.DocumentType') AND name=N'CK_Warehouse_DocumentType_Code_NotEmpty')
        ALTER TABLE [Warehouse].[DocumentType] DROP CONSTRAINT [CK_Warehouse_DocumentType_Code_NotEmpty];
    ALTER TABLE [Warehouse].[DocumentType] DROP COLUMN [Code];
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'140-remove-document-type-code');
END;
COMMIT TRANSACTION;
