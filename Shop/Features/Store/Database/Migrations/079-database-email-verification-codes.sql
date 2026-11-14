SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'079-database-email-verification-codes')
    BEGIN
        IF OBJECT_ID(N'[Account].[Profile]', N'U') IS NULL
            THROW 51079, 'Account.Profile must exist before migration 079.', 1;

        IF OBJECT_ID(N'[Account].[EmailVerificationCode]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Account].[EmailVerificationCode]
            (
                [Email] NVARCHAR(254) NOT NULL CONSTRAINT [PK_Account_EmailVerificationCode] PRIMARY KEY,
                [CodeHash] VARBINARY(32) NOT NULL,
                [SentAtUtc] DATETIME2(3) NOT NULL,
                [ExpiresAtUtc] DATETIME2(3) NOT NULL,
                [LifetimeMinutes] INT NOT NULL,
                [Attempts] INT NOT NULL,
                CONSTRAINT [CK_Account_EmailVerificationCode_LifetimeMinutes] CHECK ([LifetimeMinutes] BETWEEN 1 AND 1440),
                CONSTRAINT [CK_Account_EmailVerificationCode_Attempts] CHECK ([Attempts] BETWEEN 0 AND 5)
            );
            CREATE INDEX [IX_Account_EmailVerificationCode_ExpiresAtUtc]
                ON [Account].[EmailVerificationCode] ([ExpiresAtUtc]);
        END;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'079-database-email-verification-codes');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
