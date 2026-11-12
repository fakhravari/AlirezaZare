SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'064-account-profile-password-column')
    BEGIN
        IF OBJECT_ID(N'[Account].[Profile]', N'U') IS NULL
            THROW 51064, 'The account profile table is missing.', 1;

        IF COL_LENGTH(N'Account.Profile', N'Password') IS NULL
           AND COL_LENGTH(N'Account.Profile', N'PasswordHash') IS NOT NULL
            EXEC sys.sp_rename N'[Account].[Profile].[PasswordHash]', N'Password', N'COLUMN';
        ELSE IF COL_LENGTH(N'Account.Profile', N'Password') IS NULL
            ALTER TABLE [Account].[Profile] ADD [Password] NVARCHAR(500) NULL;

        IF COL_LENGTH(N'Account.Profile', N'PasswordHash') IS NOT NULL
            ALTER TABLE [Account].[Profile] DROP COLUMN [PasswordHash];

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
            VALUES (N'064-account-profile-password-column');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
