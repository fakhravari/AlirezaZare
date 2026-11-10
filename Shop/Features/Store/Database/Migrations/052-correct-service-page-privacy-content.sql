SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'052-correct-service-page-privacy-content')
    BEGIN
        UPDATE section SET [Body] = N'در نسخهٔ فعلی، سبد خرید، علاقه‌مندی‌ها، مقایسه، نشانی‌ها و ترجیحات کاربر واردشده از API و حساب کاربری خوانده و ذخیره می‌شوند. کلیدهای قدیمی مرورگر فقط برای انتقال یک‌باره خوانده و سپس حذف می‌شوند؛ دادهٔ اجرایی در localStorage نگهداری نمی‌شود.'
        FROM [Store].[ContentPageSection] section
        INNER JOIN [Store].[ContentPage] page ON page.[Id] = section.[IdContentPage]
        WHERE page.[PageKey] = N'privacy' AND section.[SectionKey] = N'browser';

        UPDATE section SET [Body] = N'فرم تماس آنلاین در این نسخه به سامانهٔ دریافت پیام متصل نیست و درگاه بانکی واقعی فعال نشده است. سفارش‌های ثبت‌شده از API ذخیره می‌شوند. اطلاعات حساس بانکی یا رمز خود را در مسیرهای ناامن وارد نکنید.'
        FROM [Store].[ContentPageSection] section
        INNER JOIN [Store].[ContentPage] page ON page.[Id] = section.[IdContentPage]
        WHERE page.[PageKey] = N'privacy' AND section.[SectionKey] = N'forms';

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'052-correct-service-page-privacy-content');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
