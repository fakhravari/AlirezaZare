SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'058-bank-logo-path')
    BEGIN
        IF COL_LENGTH(N'Store.Bank', N'LogoPath') IS NULL
            ALTER TABLE [Store].[Bank] ADD [LogoPath] NVARCHAR(500) NULL;
        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'058-bank-logo-path');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
