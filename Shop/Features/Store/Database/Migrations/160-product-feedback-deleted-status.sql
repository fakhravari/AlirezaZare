SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51600, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'160-product-feedback-deleted-status')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'159-status-reference-tables')
            THROW 51601, 'Migration 159 is required first.', 1;
        IF OBJECT_ID(N'Store.ProductFeedbackStatus', N'U') IS NULL
            OR COL_LENGTH(N'Store.ProductFeedbackStatus', N'NameEn') IS NULL
            THROW 51602, 'The product feedback status reference table is incomplete.', 1;
        IF EXISTS (SELECT 1 FROM Store.ProductFeedbackStatus WHERE Id = 4)
            THROW 51603, 'Product feedback status 4 already exists; inspect before migration.', 1;

        INSERT Store.ProductFeedbackStatus (Id, Name, NameEn)
        VALUES (4, N'حذف‌شده', N'Deleted');

        INSERT Store.SchemaMigration (MigrationKey)
        VALUES (N'160-product-feedback-deleted-status');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
