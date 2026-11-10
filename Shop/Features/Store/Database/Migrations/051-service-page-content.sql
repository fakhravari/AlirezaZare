SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'051-service-page-content')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[ContentPageSection]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Store].[ContentPageSection]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL,
            [IdContentPage] BIGINT NOT NULL,
            [SectionKey] NVARCHAR(100) NOT NULL,
            [Heading] NVARCHAR(300) NULL,
            [Body] NVARCHAR(MAX) NULL,
            [DataJson] NVARCHAR(MAX) NULL,
            [SortOrder] INT NOT NULL,
            [IsPublished] BIT NOT NULL CONSTRAINT [DF_Store_ContentPageSection_IsPublished] DEFAULT (1),
            CONSTRAINT [PK_Store_ContentPageSection] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_Store_ContentPageSection_Page] FOREIGN KEY ([IdContentPage]) REFERENCES [Store].[ContentPage] ([Id]) ON DELETE CASCADE,
            CONSTRAINT [UQ_Store_ContentPageSection_Page_SectionKey] UNIQUE ([IdContentPage], [SectionKey]),
            CONSTRAINT [CK_Store_ContentPageSection_DataJson] CHECK ([DataJson] IS NULL OR ISJSON([DataJson]) = 1)
        );
    END;

    UPDATE p SET [Description] = seed.[Description], [ContentSource] = N'Database'
    FROM [Store].[ContentPage] p
    INNER JOIN (VALUES
        (N'about', N'با فروشگاه ندا اولیائی، حوزهٔ فعالیت و شیوهٔ ارائهٔ اطلاعات محصول آشنا شوید.'),
        (N'contact', N'برای مشاورهٔ پیش از خرید، پیگیری سفارش و خدمات پس از فروش با مجموعه ارتباط بگیرید.'),
        (N'shipping', N'روش، هزینه و زمان تحویل بر اساس مقصد، موجودی و هماهنگی سفارش اعلام می‌شود.'),
        (N'warranty', N'مدارک خرید و کارت ضمانت را بررسی کنید؛ شرایط خدمات برای هر محصول متفاوت است.'),
        (N'tracking', N'برای بررسی وضعیت سفارش، کد سفارش و شمارهٔ همراه ثبت‌شده را آماده کنید.'),
        (N'terms', N'شرایط استفاده از وب‌سایت، اطلاعات محصولات، حساب کاربری و خرید را بخوانید.'),
        (N'returns', N'راهنمای درخواست انصراف، کالای معیوب یا مغایر و هماهنگی بازگشت کالا.'),
        (N'privacy', N'بخوانید چه اطلاعاتی نگهداری می‌شود و چگونه می‌توانید آن را مدیریت کنید.'),
        (N'faq', N'پاسخ پرسش‌های خرید، پیگیری سفارش و خدمات پس از فروش را اینجا بخوانید.')
    ) seed([PageKey], [Description]) ON seed.[PageKey] = p.[PageKey]
    WHERE p.[Description] IS NULL OR p.[Description] = N'';

    UPDATE p SET [ContentSource] = N'Database'
    FROM [Store].[ContentPage] p
    WHERE p.[PageKey] IN (N'about',N'contact',N'shipping',N'warranty',N'tracking',N'terms',N'returns',N'privacy',N'faq');

    INSERT INTO [Store].[ContentPageSection] ([IdContentPage],[SectionKey],[Heading],[Body],[SortOrder],[IsPublished])
    SELECT p.[Id], seed.[SectionKey], seed.[Heading], seed.[Body], seed.[SortOrder], 1
    FROM (VALUES
        (N'about',N'overview',N'ابزار و قطعات برای انتخاب آگاهانه',N'ندا اولیائی فروشگاهی در حوزهٔ ابزار برقی، تجهیزات اندازه‌گیری و قطعات فنی است. مشخصات و اطلاعات موجود هر محصول را بررسی کنید و برای جزئیات تکمیلی با مجموعه تماس بگیرید.',0),
        (N'about',N'service',N'همراهی از انتخاب تا پشتیبانی',N'قیمت، موجودی، ارسال و ضمانت ممکن است بر اساس کالا و سفارش تفاوت داشته باشد. اطلاعات قابل‌اعلام و راه‌های پیگیری در صفحه‌های مربوط درج می‌شوند.',1),
        (N'about',N'values',N'اصالت و شفافیت',N'اطلاعات کالا، مشخصات فنی و شرایط اعلام‌شده را پیش از تصمیم‌گیری بررسی کنید. انتخاب محصول را با کاربرد و نیاز واقعی خود هماهنگ کنید.',2),
        (N'contact',N'contact-purpose',N'راه‌های ارتباطی',N'برای مشاورهٔ پیش از خرید، پیگیری سفارش یا خدمات پس از فروش از تلفن، ایمیل یا واتساپ مجموعه استفاده کنید. برای پیگیری سفارش، شمارهٔ سفارش را آماده داشته باشید.',0),
        (N'contact',N'contact-hours',N'پیام به پشتیبانی',N'فرم پیام آنلاین در این نسخه به سامانهٔ دریافت پیام متصل نیست؛ برای دریافت پاسخ از راه‌های تماس درج‌شده استفاده کنید.',1),
        (N'shipping',N'prepare',N'هماهنگی سفارش',N'موجودی، نشانی مقصد و زمان آماده‌سازی را پیش از تأیید نهایی با فروشگاه بررسی کنید.',0),
        (N'shipping',N'method',N'روش و زمان ارسال',N'روش ارسال، هزینه و زمان تقریبی تحویل متناسب با مقصد و شرایط سفارش اعلام می‌شود.',1),
        (N'shipping',N'receive',N'بررسی هنگام دریافت',N'سلامت بسته و تطابق اقلام را بررسی کنید و هرگونه مغایرت یا آسیب را به پشتیبانی اطلاع دهید.',2),
        (N'warranty',N'documents',N'مدارک محصول را نگه دارید',N'هنگام تحویل، نام و مدل کالا، شمارهٔ سریال و اطلاعات کارت ضمانت را با فاکتور تطبیق دهید. مدت پوشش و شرایط سرویس برای هر محصول متفاوت است.',0),
        (N'warranty',N'request',N'درخواست خدمات',N'مدل کالا، تاریخ خرید و شرح کوتاهی از ایراد را آماده کنید. کارشناسان پس از بررسی اطلاعات، مراحل بعدی را هماهنگ می‌کنند.',1),
        (N'warranty',N'steps',N'مراحل پیگیری',N'فاکتور خرید و کارت ضمانت را در دسترس داشته باشید؛ شرح مشکل را به پشتیبانی بگویید و شرایط خدمات را بر اساس مدارک همان محصول هماهنگ کنید.',2),
        (N'tracking',N'information',N'اطلاعات لازم برای پیگیری',N'کد سفارش و شمارهٔ همراه ثبت‌شده را برای بررسی وضعیت آماده کنید. این نسخه به سامانهٔ رهگیری سفارش وصل نیست؛ برای پیگیری واقعی با پشتیبانی تماس بگیرید.',0),
        (N'tracking',N'support',N'پشتیبانی سفارش',N'کد سفارش را هنگام تماس در اختیار پشتیبانی بگذارید. پاسخ‌گویی و بررسی وضعیت در ساعات کاری انجام می‌شود.',1),
        (N'terms',N'products',N'اطلاعات محصولات و قیمت‌ها',N'مشخصات، تصاویر، قیمت و موجودی برای آشنایی نمایش داده می‌شوند و ممکن است نیاز به تأیید فروشگاه داشته باشند. پیش از خرید مستقیم، جزئیات نهایی را با فروشگاه بررسی کنید.',0),
        (N'terms',N'orders',N'سبد خرید و سفارش',N'سبد خرید، مقایسه و علاقه‌مندی‌ها ابزارهای کمکی وب‌سایت‌اند و تا تأیید رسمی فروشگاه سفارش قطعی نیستند. درگاه پرداخت واقعی در این نسخه فعال نیست.',1),
        (N'terms',N'responsible-use',N'استفادهٔ مسئولانه',N'از وب‌سایت به شیوه‌ای استفاده کنید که به عملکرد آن، حقوق دیگران یا امنیت اطلاعات آسیب نرساند. اطلاعات محرمانه و بانکی را از مسیرهای ناامن ارسال نکنید.',2),
        (N'terms',N'ownership',N'محتوا و مالکیت',N'متن‌ها، طراحی و محتوای اختصاصی وب‌سایت متعلق به صاحب وب‌سایت یا صاحبان مجوز آن است. حقوق قانونی اشخاص ثالث محفوظ است.',3),
        (N'returns',N'request',N'ثبت درخواست بازگشت',N'برای انصراف از خرید، کالای معیوب یا مغایر با سفارش، پیش از ارسال مجدد با فروشگاه هماهنگ کنید. شرایط هر درخواست بر اساس مقررات و اطلاعات همان خرید بررسی می‌شود.',0),
        (N'returns',N'evidence',N'اطلاعات موردنیاز',N'شمارهٔ سفارش یا فاکتور، مدل کالا و شرح مغایرت یا ایراد را آماده کنید. در صورت امکان، بسته‌بندی و مدارک خرید را نگه دارید.',1),
        (N'returns',N'follow-up',N'هماهنگی با پشتیبانی',N'برای دریافت راهنمای بسته‌بندی و نشانی بازگشت، پیش از ارسال کالا با پشتیبانی تماس بگیرید.',2),
        (N'privacy',N'browser',N'اطلاعات مرورگر',N'برای کارکرد فروشگاه، اقلام سبد خرید، علاقه‌مندی‌ها، فهرست مقایسه و بعضی تنظیمات رابط کاربری ممکن است در فضای مرورگر نگهداری شوند. پاک‌کردن داده‌های سایت آن‌ها را حذف می‌کند.',0),
        (N'privacy',N'forms',N'اطلاعات فرم‌ها',N'در نسخهٔ فعلی، فرم‌ها به سامانهٔ پرداخت بانکی یا ارسال ایمیل متصل نیستند. اطلاعات حساس بانکی یا رمز خود را در سایت وارد نکنید.',1),
        (N'privacy',N'protection',N'استفاده و حفاظت از اطلاعات',N'اطلاعات لازم برای پاسخ‌گویی و پشتیبانی فقط برای همان هدف استفاده می‌شود؛ مگر در مواردی که ارائهٔ اطلاعات به موجب قانون لازم باشد.',2),
        (N'privacy',N'manage',N'مدیریت اطلاعات',N'برای پاک‌کردن داده‌های مرورگر، اطلاعات این وب‌سایت را از تنظیمات مرورگر حذف کنید. برای پرسش دربارهٔ حریم خصوصی با فروشگاه تماس بگیرید.',3),
        (N'faq',N'choose',N'انتخاب کالا',N'برای راهنمای انتخاب، مشخصات محصول و پرسش‌های خرید را بررسی کنید.' ,0),
        (N'faq',N'order',N'سفارش و ارسال',N'هزینه و زمان تحویل پس از بررسی مقصد و موجودی هماهنگ می‌شود.' ,1),
        (N'faq',N'support',N'پشتیبانی',N'برای راهنمایی بیشتر از راه‌های تماس درج‌شده در صفحهٔ تماس با ما استفاده کنید.' ,2)
    ) seed([PageKey],[SectionKey],[Heading],[Body],[SortOrder])
    INNER JOIN [Store].[ContentPage] p ON p.[PageKey] = seed.[PageKey]
    WHERE NOT EXISTS (SELECT 1 FROM [Store].[ContentPageSection] existing WHERE existing.[IdContentPage] = p.[Id] AND existing.[SectionKey] = seed.[SectionKey]);

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'051-service-page-content');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
