SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'039-consolidate-site-identity')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF OBJECT_ID(N'[Store].[SiteSetting]', N'U') IS NULL OR OBJECT_ID(N'[Store].[SiteIdentity]', N'U') IS NULL
        THROW 51039, 'Site settings and site identity tables must exist.', 1;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SiteIdentity] WHERE [Id] = 1)
        THROW 51039, 'The site identity row is missing.', 1;

    ALTER TABLE [Store].[SiteSetting] ADD
        [DisplayName] NVARCHAR(160) NULL,
        [StoreName] NVARCHAR(240) NULL,
        [VisualBrandName] NVARCHAR(160) NULL,
        [SupportPhoneDisplay] NVARCHAR(32) NULL,
        [SupportPhoneUri] NVARCHAR(40) NULL,
        [WhatsAppNumber] NVARCHAR(32) NULL,
        [PublicEmail] NVARCHAR(254) NULL,
        [IdentityUpdatedAtUtc] DATETIME2(3) NULL;

    EXEC(N'IF NOT EXISTS (SELECT 1 FROM [Store].[SiteSetting] WHERE [Id] = 1)
        INSERT INTO [Store].[SiteSetting] ([Id]) VALUES (1);
        UPDATE setting SET [DisplayName] = identityRow.[DisplayName], [StoreName] = identityRow.[StoreName],
            [VisualBrandName] = identityRow.[VisualBrandName], [SupportPhoneDisplay] = identityRow.[SupportPhoneDisplay],
            [SupportPhoneUri] = identityRow.[SupportPhoneUri], [WhatsAppNumber] = identityRow.[WhatsAppNumber],
            [PublicEmail] = identityRow.[PublicEmail], [IdentityUpdatedAtUtc] = identityRow.[UpdatedAtUtc]
        FROM [Store].[SiteSetting] setting INNER JOIN [Store].[SiteIdentity] identityRow ON identityRow.[Id] = setting.[Id]
        WHERE setting.[Id] = 1;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [DisplayName] NVARCHAR(160) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [StoreName] NVARCHAR(240) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [VisualBrandName] NVARCHAR(160) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [SupportPhoneDisplay] NVARCHAR(32) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [SupportPhoneUri] NVARCHAR(40) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [WhatsAppNumber] NVARCHAR(32) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [PublicEmail] NVARCHAR(254) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ALTER COLUMN [IdentityUpdatedAtUtc] DATETIME2(3) NOT NULL;
        ALTER TABLE [Store].[SiteSetting] ADD
            CONSTRAINT [CK_Store_SiteSetting_DisplayName] CHECK (LEN(LTRIM(RTRIM([DisplayName]))) > 0),
            CONSTRAINT [CK_Store_SiteSetting_StoreName] CHECK (LEN(LTRIM(RTRIM([StoreName]))) > 0),
            CONSTRAINT [CK_Store_SiteSetting_VisualBrandName] CHECK (LEN(LTRIM(RTRIM([VisualBrandName]))) > 0);');

    DROP TABLE [Store].[SiteIdentity];
    -- Temporary compatibility for instances that have not yet restarted.
    EXEC(N'CREATE VIEW [Store].[SiteIdentity] AS
        SELECT [Id], [DisplayName], [StoreName], [VisualBrandName], [SupportPhoneDisplay],
               [SupportPhoneUri], [WhatsAppNumber], [PublicEmail], [IdentityUpdatedAtUtc] AS [UpdatedAtUtc]
        FROM [Store].[SiteSetting]');
    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'039-consolidate-site-identity');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
