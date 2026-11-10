SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'053-plain-smtp-password-setting')
    BEGIN
        IF OBJECT_ID(N'[Store].[SiteSetting]', N'U') IS NULL
            THROW 51053, 'The site settings table is missing.', 1;

        IF COL_LENGTH(N'Store.SiteSetting', N'SmtpPassword') IS NULL
            ALTER TABLE [Store].[SiteSetting] ADD [SmtpPassword] NVARCHAR(1024) NULL;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
            VALUES (N'053-plain-smtp-password-setting');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
