SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51840, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'184-remove-catalog-feature-parent-key')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'183-catalog-feature-tree')
            THROW 51841, 'Migration 183 must be applied first.', 1;

        ALTER TABLE [Store].[ProductCatalogFeatureValue]
            DROP CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Value];
        ALTER TABLE [Store].[VariantCatalogFeatureValue]
            DROP CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Value];
        ALTER TABLE [Store].[CatalogFeature]
            DROP CONSTRAINT [AK_Store_CatalogFeature_Id_Parent];
        ALTER TABLE [Store].[CatalogFeature]
            DROP COLUMN [IdParentKey];

        ALTER TABLE [Store].[ProductCatalogFeatureValue]
            ADD CONSTRAINT [FK_Store_ProductCatalogFeatureValue_Value]
            FOREIGN KEY ([IdFeatureValue]) REFERENCES [Store].[CatalogFeature] ([Id]);
        ALTER TABLE [Store].[VariantCatalogFeatureValue]
            ADD CONSTRAINT [FK_Store_VariantCatalogFeatureValue_Value]
            FOREIGN KEY ([IdFeatureValue]) REFERENCES [Store].[CatalogFeature] ([Id]);

        EXEC sys.sp_executesql N'
            CREATE OR ALTER TRIGGER [Store].[TR_ProductCatalogFeatureValue_ValidateTree]
            ON [Store].[ProductCatalogFeatureValue]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted a
                    LEFT JOIN [Store].[CatalogFeature] feature
                        ON feature.[Id] = a.[IdFeature] AND feature.[IdParent] IS NULL
                    LEFT JOIN [Store].[CatalogFeature] value
                        ON value.[Id] = a.[IdFeatureValue] AND value.[IdParent] = a.[IdFeature]
                    WHERE feature.[Id] IS NULL OR value.[Id] IS NULL)
                    THROW 51842, N''Product feature assignment must reference a root and its child value.'', 1;
            END;';

        EXEC sys.sp_executesql N'
            CREATE OR ALTER TRIGGER [Store].[TR_VariantCatalogFeatureValue_ValidateTree]
            ON [Store].[VariantCatalogFeatureValue]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (
                    SELECT 1
                    FROM inserted a
                    LEFT JOIN [Store].[CatalogFeature] feature
                        ON feature.[Id] = a.[IdFeature] AND feature.[IdParent] IS NULL
                    LEFT JOIN [Store].[CatalogFeature] value
                        ON value.[Id] = a.[IdFeatureValue] AND value.[IdParent] = a.[IdFeature]
                    WHERE feature.[Id] IS NULL OR value.[Id] IS NULL)
                    THROW 51843, N''Variant feature assignment must reference a root and its child value.'', 1;
            END;';

        EXEC sys.sp_executesql N'
            CREATE OR ALTER TRIGGER [Store].[TR_CatalogFeature_ValidateAssignedTree]
            ON [Store].[CatalogFeature]
            AFTER UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;
                /*
                  تغییر IdParent می‌تواند رابطهٔ درختی ویژگی‌ها را عوض کند.
                  اگر ویژگی ریشه‌ای که تخصیص مستقیم دارد فرزند شود، یا مقدار
                  تخصیص‌یافته زیر ویژگی دیگری قرار بگیرد، تخصیص‌های کالا و تنوع
                  دیگر به ترکیب ریشه و مقدار درست اشاره نمی‌کنند. این trigger
                  چنین تغییری را فقط وقتی رد می‌کند که تخصیص موجود را نامعتبر کند؛
                  تغییرات بدون تخصیص ناسازگار مجاز می‌مانند.
                */
                IF UPDATE([IdParent]) AND EXISTS (
                    SELECT 1 FROM inserted node
                    JOIN [Store].[ProductCatalogFeatureValue] assignment ON assignment.[IdFeature] = node.[Id]
                    WHERE node.[IdParent] IS NOT NULL
                    UNION ALL
                    SELECT 1 FROM inserted node
                    JOIN [Store].[VariantCatalogFeatureValue] assignment ON assignment.[IdFeature] = node.[Id]
                    WHERE node.[IdParent] IS NOT NULL
                    UNION ALL
                    SELECT 1 FROM inserted node
                    JOIN [Store].[ProductCatalogFeatureValue] assignment ON assignment.[IdFeatureValue] = node.[Id]
                    WHERE assignment.[IdFeature] <> node.[IdParent]
                    UNION ALL
                    SELECT 1 FROM inserted node
                    JOIN [Store].[VariantCatalogFeatureValue] assignment ON assignment.[IdFeatureValue] = node.[Id]
                    WHERE assignment.[IdFeature] <> node.[IdParent])
                    THROW 51844, N''Catalog feature parent cannot invalidate existing assignments.'', 1;
            END;';

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'184-remove-catalog-feature-parent-key');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
