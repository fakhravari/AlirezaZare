SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'080-footer-content-and-social-links')
    BEGIN
        IF OBJECT_ID(N'[Store].[SiteSetting]', N'U') IS NULL
            THROW 51080, 'Store.SiteSetting must exist before migration 080.', 1;

        IF COL_LENGTH(N'Store.SiteSetting', N'FooterDescription') IS NULL
            ALTER TABLE [Store].[SiteSetting] ADD [FooterDescription] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_Store_SiteSetting_FooterDescription] DEFAULT N'';
        IF COL_LENGTH(N'Store.SiteSetting', N'InstagramUrl') IS NULL
            ALTER TABLE [Store].[SiteSetting] ADD [InstagramUrl] NVARCHAR(500) NULL;
        IF COL_LENGTH(N'Store.SiteSetting', N'TelegramUrl') IS NULL
            ALTER TABLE [Store].[SiteSetting] ADD [TelegramUrl] NVARCHAR(500) NULL;
        IF COL_LENGTH(N'Store.SiteSetting', N'LinkedInUrl') IS NULL
            ALTER TABLE [Store].[SiteSetting] ADD [LinkedInUrl] NVARCHAR(500) NULL;

        EXEC(N'UPDATE [Store].[SiteSetting]
            SET [FooterDescription] = N''محصولاتی از ابزار برقی، تجهیزات اندازه‌گیری، قطعات الکترونیکی و روشنایی برای بررسی و مقایسه. مشخصات، قیمت و شرایط هر کالا را در صفحهٔ محصول ببینید و برای پرسش‌های بیشتر با مجموعه تماس بگیرید.''
            WHERE LEN(LTRIM(RTRIM([FooterDescription]))) = 0;');

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'080-footer-content-and-social-links');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
