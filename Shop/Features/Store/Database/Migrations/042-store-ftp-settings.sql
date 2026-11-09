SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'042-store-ftp-settings')
    BEGIN
        ALTER TABLE [Store].[SiteSetting] ADD [FtpHost] NVARCHAR(255) NULL, [FtpPort] INT NULL,
            [FtpUsername] NVARCHAR(320) NULL, [FtpPasswordProtected] NVARCHAR(MAX) NULL;
        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'042-store-ftp-settings');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
