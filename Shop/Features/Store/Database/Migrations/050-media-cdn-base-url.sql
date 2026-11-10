SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'050-media-cdn-base-url')
BEGIN
    IF COL_LENGTH(N'Store.SiteSetting', N'MediaCdnBaseUrl') IS NULL
        THROW 51050, 'Media CDN migration is recorded but incomplete.', 1;
    COMMIT TRANSACTION;
    RETURN;
END;

IF COL_LENGTH(N'Store.SiteSetting', N'MediaCdnBaseUrl') IS NULL
    ALTER TABLE [Store].[SiteSetting] ADD [MediaCdnBaseUrl] nvarchar(500) NOT NULL
        CONSTRAINT [DF_Store_SiteSetting_MediaCdnBaseUrl] DEFAULT (N'https://cdn.bs-shop.ir/') WITH VALUES;
ELSE
    EXEC sys.sp_executesql N'
        UPDATE [Store].[SiteSetting] SET [MediaCdnBaseUrl] = N''https://cdn.bs-shop.ir/''
        WHERE [MediaCdnBaseUrl] IS NULL OR LTRIM(RTRIM([MediaCdnBaseUrl])) = N'''';';

INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'050-media-cdn-base-url');

COMMIT TRANSACTION;
