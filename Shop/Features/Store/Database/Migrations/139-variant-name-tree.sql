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
    THROW 51390, 'Store.SchemaMigration is required.', 1;
IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'139-variant-name-tree')
BEGIN
    IF OBJECT_ID(N'[Warehouse].[ProductVariant]', N'U') IS NULL OR COL_LENGTH(N'Warehouse.ProductVariant', N'Code') IS NULL OR COL_LENGTH(N'Warehouse.ProductVariant', N'Name') IS NULL
        THROW 51391, 'Expected legacy variant schema is missing.', 1;
    IF OBJECT_ID(N'[Warehouse].[VariantName]', N'U') IS NOT NULL OR OBJECT_ID(N'[Warehouse].[ProductVariantNameArchive139]', N'U') IS NOT NULL
        THROW 51392, 'Unexpected variant name tables already exist.', 1;
    IF EXISTS (SELECT 1 FROM [Warehouse].[ProductVariant] WHERE NULLIF(LTRIM(RTRIM([Name])), N'') IS NULL)
        THROW 51393, 'Some variants have no name; migration stopped.', 1;
    IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID(N'Warehouse.ProductVariant') AND c.name IN (N'Name',N'Code') AND i.name <> N'UX_Warehouse_ProductVariant_Product_Code')
        THROW 51394, 'Unexpected index uses a legacy variant name or code.', 1;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'Warehouse.ProductVariant') AND (definition LIKE N'%Name%' OR definition LIKE N'%Code%') AND name <> N'CK_Warehouse_ProductVariant_Name_NotEmpty')
        THROW 51397, 'Unexpected check constraint uses a legacy variant column.', 1;
    IF EXISTS (SELECT 1 FROM sys.sql_modules WHERE definition LIKE N'%ProductVariant%' AND (definition LIKE N'%Code%' OR definition LIKE N'%Name%'))
        THROW 51395, 'Database module may depend on a legacy variant column.', 1;

    CREATE TABLE [Warehouse].[VariantName] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse_VariantName] PRIMARY KEY,
        [IdParent] BIGINT NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_VariantName_IsActive] DEFAULT(1),
        CONSTRAINT [FK_Warehouse_VariantName_Parent] FOREIGN KEY ([IdParent]) REFERENCES [Warehouse].[VariantName] ([Id]),
        CONSTRAINT [CK_Warehouse_VariantName_Name] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );
    CREATE INDEX [IX_Warehouse_VariantName_Parent] ON [Warehouse].[VariantName] ([IdParent]);
    CREATE TABLE [Warehouse].[ProductVariantNameArchive139] (
        [IdVariant] BIGINT NOT NULL CONSTRAINT [PK_ProductVariantNameArchive139] PRIMARY KEY,
        [Name] NVARCHAR(300) NOT NULL
    );
    INSERT [Warehouse].[ProductVariantNameArchive139] ([IdVariant],[Name]) SELECT [Id],[Name] FROM [Warehouse].[ProductVariant];
    DECLARE @root BIGINT;
    INSERT [Warehouse].[VariantName] ([IdParent],[Name],[IsActive]) VALUES (NULL,N'نام‌های قبلی',1);
    SET @root = SCOPE_IDENTITY();
    INSERT [Warehouse].[VariantName] ([IdParent],[Name],[IsActive])
        SELECT @root, [Name] COLLATE Latin1_General_100_BIN2, 1 FROM [Warehouse].[ProductVariant]
        GROUP BY [Name] COLLATE Latin1_General_100_BIN2;
    ALTER TABLE [Warehouse].[ProductVariant] ADD [IdVariantName] BIGINT NULL;
    EXEC sp_executesql N'UPDATE v SET IdVariantName=n.Id FROM Warehouse.ProductVariant v JOIN Warehouse.VariantName n ON n.IdParent=@root AND n.Name COLLATE Latin1_General_100_BIN2 = v.Name COLLATE Latin1_General_100_BIN2', N'@root bigint', @root;
    EXEC(N'IF EXISTS (SELECT 1 FROM [Warehouse].[ProductVariant] WHERE [IdVariantName] IS NULL) THROW 51396, ''Some existing variants could not be linked.'', 1;');
    ALTER TABLE [Warehouse].[ProductVariant] ALTER COLUMN [IdVariantName] BIGINT NOT NULL;
    ALTER TABLE [Warehouse].[ProductVariant] ADD CONSTRAINT [FK_Warehouse_ProductVariant_VariantName] FOREIGN KEY ([IdVariantName]) REFERENCES [Warehouse].[VariantName] ([Id]);
    CREATE INDEX [IX_Warehouse_ProductVariant_VariantName] ON [Warehouse].[ProductVariant] ([IdVariantName]);
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'Warehouse.ProductVariant') AND name=N'UX_Warehouse_ProductVariant_Product_Code')
        DROP INDEX [UX_Warehouse_ProductVariant_Product_Code] ON [Warehouse].[ProductVariant];
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'Warehouse.ProductVariant') AND name=N'CK_Warehouse_ProductVariant_Name_NotEmpty')
        ALTER TABLE [Warehouse].[ProductVariant] DROP CONSTRAINT [CK_Warehouse_ProductVariant_Name_NotEmpty];
    ALTER TABLE [Warehouse].[ProductVariant] DROP COLUMN [Code], [Name];
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'139-variant-name-tree');
END;
COMMIT TRANSACTION;
