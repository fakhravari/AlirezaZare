SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51860, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'186-document-catalog-feature-assigned-tree-trigger')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'184-remove-catalog-feature-parent-key')
            THROW 51861, 'Migration 184 must be applied first.', 1;

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
        VALUES (N'186-document-catalog-feature-assigned-tree-trigger');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
