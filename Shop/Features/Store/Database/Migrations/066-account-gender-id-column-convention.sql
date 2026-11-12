SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'066-account-gender-id-column-convention')
    BEGIN
        IF COL_LENGTH(N'Account.Profile', N'GenderId') IS NOT NULL
           AND COL_LENGTH(N'Account.Profile', N'IdGender') IS NOT NULL
            THROW 51067, 'Both gender reference columns exist.', 1;

        IF COL_LENGTH(N'Account.Profile', N'GenderId') IS NOT NULL
            EXEC sys.sp_rename N'[Account].[Profile].[GenderId]', N'IdGender', N'COLUMN';
        ELSE IF COL_LENGTH(N'Account.Profile', N'IdGender') IS NULL
            THROW 51068, 'The account gender reference column is missing.', 1;

        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Account_Profile_Gender_GenderId')
            ALTER TABLE [Account].[Profile] DROP CONSTRAINT [FK_Account_Profile_Gender_GenderId];

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Account_Profile_Gender_IdGender')
            EXEC(N'ALTER TABLE [Account].[Profile] ADD CONSTRAINT [FK_Account_Profile_Gender_IdGender]
                  FOREIGN KEY ([IdGender]) REFERENCES [Account].[Gender] ([Id]);');

        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Account_Profile_GenderId' AND [object_id] = OBJECT_ID(N'[Account].[Profile]'))
           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Account_Profile_IdGender' AND [object_id] = OBJECT_ID(N'[Account].[Profile]'))
            EXEC sys.sp_rename N'[Account].[Profile].[IX_Account_Profile_GenderId]', N'IX_Account_Profile_IdGender', N'INDEX';

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
            VALUES (N'066-account-gender-id-column-convention');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
