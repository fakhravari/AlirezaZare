SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'027-store-site-identity')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[SiteIdentity]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Store].[SiteIdentity]
        (
            [Id] TINYINT NOT NULL,
            [DisplayName] NVARCHAR(160) NOT NULL,
            [StoreName] NVARCHAR(240) NOT NULL,
            [VisualBrandName] NVARCHAR(160) NOT NULL,
            [SupportPhoneDisplay] NVARCHAR(32) NOT NULL,
            [SupportPhoneUri] NVARCHAR(40) NOT NULL,
            [WhatsAppNumber] NVARCHAR(32) NOT NULL,
            [PublicEmail] NVARCHAR(254) NOT NULL,
            [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_SiteIdentity_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [PK_Store_SiteIdentity] PRIMARY KEY ([Id]),
            CONSTRAINT [CK_Store_SiteIdentity_Singleton] CHECK ([Id] = 1),
            CONSTRAINT [CK_Store_SiteIdentity_DisplayName] CHECK (LEN(LTRIM(RTRIM([DisplayName]))) > 0),
            CONSTRAINT [CK_Store_SiteIdentity_StoreName] CHECK (LEN(LTRIM(RTRIM([StoreName]))) > 0),
            CONSTRAINT [CK_Store_SiteIdentity_VisualBrandName] CHECK (LEN(LTRIM(RTRIM([VisualBrandName]))) > 0)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SiteIdentity] WHERE [Id] = 1)
        INSERT INTO [Store].[SiteIdentity]
            ([Id], [DisplayName], [StoreName], [VisualBrandName], [SupportPhoneDisplay], [SupportPhoneUri], [WhatsAppNumber], [PublicEmail])
        VALUES
            (1, N'ندا اولیائی', N'فروشگاه برق و ابزار ندا اولیائی', N'برق شیراز', N'09369455457', N'+989369455457', N'989369455457', N'neda.oliyai67@gmail.com');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'027-store-site-identity');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
