SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'077-email-verification-code-lifetime')
    BEGIN
        IF OBJECT_ID(N'[Store].[SiteSetting]', N'U') IS NULL
            THROW 51077, 'Store.SiteSetting must exist before migration 077.', 1;

        IF COL_LENGTH(N'Store.SiteSetting', N'EmailVerificationCodeLifetimeMinutes') IS NULL
            ALTER TABLE [Store].[SiteSetting] ADD [EmailVerificationCodeLifetimeMinutes] INT NOT NULL
                CONSTRAINT [DF_Store_SiteSetting_EmailVerificationCodeLifetimeMinutes] DEFAULT (15) WITH VALUES;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'077-email-verification-code-lifetime');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
