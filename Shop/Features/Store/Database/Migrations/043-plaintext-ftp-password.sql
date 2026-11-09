SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'043-plaintext-ftp-password')
    BEGIN
        IF COL_LENGTH(N'Store.SiteSetting', N'FtpPasswordProtected') IS NULL
            THROW 51043, 'FTP settings migration 042 must run first.', 1;
        ALTER TABLE [Store].[SiteSetting] ADD [FtpPassword] NVARCHAR(1024) NULL;
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'043-plaintext-ftp-password');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
