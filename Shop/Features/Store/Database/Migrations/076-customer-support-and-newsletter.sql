SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'076-customer-support-and-newsletter')
    BEGIN
        IF OBJECT_ID(N'[Store].[NewsletterSubscriber]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Store].[NewsletterSubscriber]
            (
                [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_NewsletterSubscriber] PRIMARY KEY,
                [Email] NVARCHAR(254) NOT NULL,
                [SubscribedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_NewsletterSubscriber_SubscribedAtUtc] DEFAULT SYSUTCDATETIME()
            );
            CREATE UNIQUE INDEX [IX_Store_NewsletterSubscriber_Email]
                ON [Store].[NewsletterSubscriber] ([Email]);
        END;

        IF OBJECT_ID(N'[Store].[ContentPageSection]', N'U') IS NOT NULL
        BEGIN
            UPDATE section
            SET [Body] = N'پیام این فرم از طریق ایمیل برای پشتیبانی فروشگاه ارسال می‌شود. برای پیگیری سفارش، کد سفارش را در متن پیام بنویسید.'
            FROM [Store].[ContentPageSection] section
            INNER JOIN [Store].[ContentPage] page ON page.[Id] = section.[IdContentPage]
            WHERE page.[PageKey] = N'contact'
              AND section.[SectionKey] = N'contact-hours'
              AND section.[Body] = N'فرم پیام آنلاین در این نسخه به سامانهٔ دریافت پیام متصل نیست؛ برای دریافت پاسخ از راه‌های تماس درج‌شده استفاده کنید.';

            UPDATE section
            SET [Body] = N'کد سفارش و شمارهٔ همراه ثبت‌شده را در فرم پیگیری وارد کنید. وضعیت سفارش و اطلاعات ثبت‌شدهٔ ارسال نمایش داده می‌شود؛ اگر کد رهگیری هنوز ثبت نشده، با پشتیبانی تماس بگیرید.'
            FROM [Store].[ContentPageSection] section
            INNER JOIN [Store].[ContentPage] page ON page.[Id] = section.[IdContentPage]
            WHERE page.[PageKey] = N'tracking'
              AND section.[SectionKey] = N'information'
              AND section.[Body] = N'کد سفارش و شمارهٔ همراه ثبت‌شده را برای بررسی وضعیت آماده کنید. این نسخه به سامانهٔ رهگیری سفارش وصل نیست؛ برای پیگیری واقعی با پشتیبانی تماس بگیرید.';
        END;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'076-customer-support-and-newsletter');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
