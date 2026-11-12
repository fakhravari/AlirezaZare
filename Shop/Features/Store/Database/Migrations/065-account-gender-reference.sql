SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'065-account-gender-reference')
    BEGIN
        IF OBJECT_ID(N'[Account].[Profile]', N'U') IS NULL
            THROW 51065, 'The account profile table is missing.', 1;

        IF OBJECT_ID(N'[Account].[Gender]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Account].[Gender]
            (
                [Id] INT NOT NULL,
                [Code] NVARCHAR(20) NOT NULL,
                [NameFa] NVARCHAR(40) NOT NULL,
                CONSTRAINT [PK_Account_Gender] PRIMARY KEY ([Id]),
                CONSTRAINT [UQ_Account_Gender_Code] UNIQUE ([Code])
            );
        END;

        IF NOT EXISTS (SELECT 1 FROM [Account].[Gender] WHERE [Id] = 1)
            INSERT INTO [Account].[Gender] ([Id], [Code], [NameFa]) VALUES (1, N'male', N'آقا');
        IF NOT EXISTS (SELECT 1 FROM [Account].[Gender] WHERE [Id] = 2)
            INSERT INTO [Account].[Gender] ([Id], [Code], [NameFa]) VALUES (2, N'female', N'خانم');

        IF COL_LENGTH(N'Account.Profile', N'GenderId') IS NULL
            ALTER TABLE [Account].[Profile] ADD [GenderId] INT NULL;

        IF COL_LENGTH(N'Account.Profile', N'Gender') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM [Account].[Profile] WHERE [Gender] IS NOT NULL AND [Gender] NOT IN (N'male', N'female'))
                THROW 51066, 'Unexpected account gender values prevent migration.', 1;

            EXEC(N'UPDATE [Account].[Profile]
                     SET [GenderId] = CASE [Gender] WHEN N''male'' THEN 1 WHEN N''female'' THEN 2 END
                   WHERE [GenderId] IS NULL AND [Gender] IS NOT NULL;');

            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Account_Profile_Gender')
                ALTER TABLE [Account].[Profile] DROP CONSTRAINT [CK_Account_Profile_Gender];
            ALTER TABLE [Account].[Profile] DROP COLUMN [Gender];
        END;

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Account_Profile_Gender_GenderId')
            ALTER TABLE [Account].[Profile] ADD CONSTRAINT [FK_Account_Profile_Gender_GenderId]
                FOREIGN KEY ([GenderId]) REFERENCES [Account].[Gender] ([Id]);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Account_Profile_GenderId' AND [object_id] = OBJECT_ID(N'[Account].[Profile]'))
            CREATE INDEX [IX_Account_Profile_GenderId] ON [Account].[Profile] ([GenderId]);

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
            VALUES (N'065-account-gender-reference');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
