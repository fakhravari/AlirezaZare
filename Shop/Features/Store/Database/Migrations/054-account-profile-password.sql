SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'054-account-profile-password')
    BEGIN
        IF OBJECT_ID(N'[Account].[Profile]', N'U') IS NULL
            THROW 51054, 'The account profile table is missing.', 1;

        IF COL_LENGTH(N'Account.Profile', N'PasswordHash') IS NULL
            ALTER TABLE [Account].[Profile] ADD [PasswordHash] NVARCHAR(500) NULL;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
            VALUES (N'054-account-profile-password');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
