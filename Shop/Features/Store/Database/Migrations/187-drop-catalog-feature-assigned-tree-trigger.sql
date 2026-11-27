SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51870, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                   WHERE [MigrationKey] = N'187-drop-catalog-feature-assigned-tree-trigger')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration]
                       WHERE [MigrationKey] = N'184-remove-catalog-feature-parent-key')
            THROW 51871, 'Migration 184 must be applied first.', 1;

        IF OBJECT_ID(N'[Store].[TR_CatalogFeature_ValidateAssignedTree]', N'TR') IS NOT NULL
            DROP TRIGGER [Store].[TR_CatalogFeature_ValidateAssignedTree];

        INSERT [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'187-drop-catalog-feature-assigned-tree-trigger');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
