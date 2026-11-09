SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'038-site-smtp-settings')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF OBJECT_ID(N'[Store].[SiteSetting]', N'U') IS NULL
        THROW 51038, 'Site settings migration 037 must run first.', 1;

    ALTER TABLE [Store].[SiteSetting] ADD
        [SmtpHost] NVARCHAR(255) NULL,
        [SmtpPort] INT NULL,
        [SmtpEnableSsl] BIT NULL,
        [SmtpUsername] NVARCHAR(320) NULL,
        [SmtpFrom] NVARCHAR(320) NULL;
    EXEC(N'IF NOT EXISTS (SELECT 1 FROM [Store].[SiteSetting] WHERE [Id] = 1)
        INSERT INTO [Store].[SiteSetting] ([Id]) VALUES (1);
        UPDATE [Store].[SiteSetting] SET [SmtpHost] = N''31.25.91.7'', [SmtpPort] = 587,
            [SmtpEnableSsl] = 1, [SmtpUsername] = N''info@bs-shop.ir'', [SmtpFrom] = N''info@bs-shop.ir''
        WHERE [Id] = 1;');
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'038-site-smtp-settings');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
