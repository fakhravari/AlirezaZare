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
    THROW 51420, 'Store.SchemaMigration is required.', 1;
IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'142-group-variant-names-by-category')
BEGIN
    IF OBJECT_ID(N'[Warehouse].[VariantName]', N'U') IS NULL
       OR OBJECT_ID(N'[Warehouse].[ProductVariant]', N'U') IS NULL
       OR OBJECT_ID(N'[Warehouse].[Product]', N'U') IS NULL
       OR OBJECT_ID(N'[Store].[Category]', N'U') IS NULL
        THROW 51421, 'Variant name or product category tables are missing.', 1;

    DECLARE @rootId BIGINT;
    SELECT @rootId = [Id]
    FROM [Warehouse].[VariantName]
    WHERE [IdParent] IS NULL AND [Name] = N'نام‌های قبلی';

    IF @rootId IS NULL
        THROW 51422, 'The expected legacy variant-name root was not found.', 1;
    IF (SELECT COUNT(*) FROM [Warehouse].[VariantName] WHERE [IdParent] IS NULL AND [Name] = N'نام‌های قبلی') <> 1
        THROW 51423, 'The legacy variant-name root is ambiguous.', 1;

    DECLARE @categoryCount INT, @categoryName NVARCHAR(300);
    SELECT @categoryCount = COUNT(DISTINCT category.[Id]), @categoryName = MIN(category.[NameFa])
    FROM [Warehouse].[ProductVariant] variant
    JOIN [Warehouse].[VariantName] nameNode ON nameNode.[Id] = variant.[IdVariantName]
    JOIN [Warehouse].[Product] product ON product.[Id] = variant.[IdProduct]
    JOIN [Store].[Category] category ON category.[Id] = product.[IdCategory]
    WHERE nameNode.[IdParent] = @rootId;

    IF @categoryCount <> 1 OR NULLIF(LTRIM(RTRIM(@categoryName)), N'') IS NULL
        THROW 51424, 'Legacy variants do not map to exactly one valid product category; no data was changed.', 1;
    IF EXISTS (SELECT 1 FROM [Warehouse].[VariantName] WHERE [IdParent] IS NULL AND [Id] <> @rootId AND [Name] = @categoryName)
        THROW 51425, 'A root with this product-category name already exists.', 1;

    UPDATE [Warehouse].[VariantName]
    SET [Name] = @categoryName
    WHERE [Id] = @rootId;

    INSERT [Store].[SchemaMigration] ([MigrationKey])
    VALUES (N'142-group-variant-names-by-category');
END;

COMMIT TRANSACTION;
