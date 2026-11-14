SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'081-admin-page-permissions')
    BEGIN
        IF OBJECT_ID(N'[Admin].[Permission]', N'U') IS NULL
            THROW 51081, 'Admin.Permission must exist before applying migration 081.', 1;

        DECLARE @PagePermissions TABLE
        (
            [PermissionKey] NVARCHAR(160) NOT NULL,
            [Name] NVARCHAR(200) NOT NULL,
            [Area] NVARCHAR(80) NOT NULL,
            [Controller] NVARCHAR(100) NOT NULL,
            [Action] NVARCHAR(100) NOT NULL,
            [FeatureKey] NVARCHAR(100) NULL,
            [SortOrder] INT NOT NULL
        );

        INSERT INTO @PagePermissions ([PermissionKey], [Name], [Area], [Controller], [Action], [FeatureKey], [SortOrder])
        VALUES
            (N'admin.dashboard', N'پیشخوان کل', N'Admin', N'Home', N'Index', NULL, 1),
            (N'store.dashboard', N'پیشخوان فروشگاه', N'Store', N'Dashboard', N'Index', NULL, 2),
            (N'warehouse.dashboard', N'نمای کلی انبار', N'Warehouse', N'Dashboard', N'Index', NULL, 3),
            (N'store.catalog.products', N'محصولات', N'Store', N'Catalog', N'Products', NULL, 10),
            (N'store.catalog.categories', N'دسته‌بندی‌ها', N'Store', N'Catalog', N'Categories', NULL, 11),
            (N'store.catalog.brands', N'برندها', N'Store', N'Catalog', N'Brands', NULL, 12),
            (N'store.catalog.attributes', N'ویژگی‌ها', N'Store', N'Catalog', N'Attributes', NULL, 13),
            (N'store.catalog.payment-institutions', N'درگاه‌های پرداخت', N'Store', N'Catalog', N'PaymentInstitutions', NULL, 14),
            (N'store.features.geography', N'مناطق جغرافیایی', N'Store', N'Features', N'Index', N'geography', 20),
            (N'store.features.paid-orders-dispatch', N'حواله سفارش‌های پرداخت‌شده', N'Store', N'Features', N'Index', N'paid-orders-dispatch', 21),
            (N'store.features.orders', N'سفارش‌ها', N'Store', N'Features', N'Index', N'orders', 22),
            (N'store.features.promotions', N'طرح‌های فروش و تخفیف', N'Store', N'Features', N'Index', N'promotions', 23),
            (N'store.features.customers', N'مشتریان', N'Store', N'Features', N'Index', N'customers', 24),
            (N'store.features.contact-messages', N'پیام‌های فرم تماس', N'Store', N'Features', N'Index', N'contact-messages', 25),
            (N'store.features.account-messages', N'پیام‌های حساب مشتریان', N'Store', N'Features', N'Index', N'account-messages', 26),
            (N'store.features.reviews', N'دیدگاه‌های محصول', N'Store', N'Features', N'Index', N'reviews', 27),
            (N'store.features.questions', N'پرسش‌های محصول', N'Store', N'Features', N'Index', N'questions', 28),
            (N'store.features.content', N'محتوا و صفحات', N'Store', N'Features', N'Index', N'content', 29),
            (N'admin.site-settings.index', N'تنظیمات عمومی سایت', N'Admin', N'SiteSettings', N'Index', NULL, 30),
            (N'admin.site-settings.slider', N'تنظیم اسلایدر', N'Admin', N'SiteSettings', N'Slider', NULL, 31),
            (N'warehouse.warehouses.index', N'انبارها', N'Warehouse', N'Warehouses', N'Index', NULL, 40),
            (N'warehouse.stock.index', N'موجودی کالا', N'Warehouse', N'Stock', N'Index', NULL, 41),
            (N'warehouse.features.order-confirmation', N'تأیید سفارش‌های پرداخت‌شده', N'Warehouse', N'Features', N'Index', N'order-confirmation', 42),
            (N'warehouse.features.companies', N'شرکت‌ها و تأمین‌کنندگان', N'Warehouse', N'Features', N'Index', N'companies', 43),
            (N'warehouse.features.movements', N'گردش کالا', N'Warehouse', N'Features', N'Index', N'movements', 44),
            (N'warehouse.features.receipts', N'رسید کالا', N'Warehouse', N'Features', N'Index', N'receipts', 45),
            (N'warehouse.features.dispatches', N'حواله خروج', N'Warehouse', N'Features', N'Index', N'dispatches', 46),
            (N'warehouse.features.transfers', N'انتقال بین انبارها', N'Warehouse', N'Features', N'Index', N'transfers', 47),
            (N'warehouse.features.returns', N'مرجوعی کالا', N'Warehouse', N'Features', N'Index', N'returns', 48),
            (N'warehouse.features.stocktake', N'انبارگردانی دستی', N'Warehouse', N'Features', N'Index', N'stocktake', 49),
            (N'warehouse.features.adjustments', N'اصلاح موجودی', N'Warehouse', N'Features', N'Index', N'adjustments', 50),
            (N'warehouse.features.references', N'مراجع گردش انبار', N'Warehouse', N'Features', N'Index', N'references', 51),
            (N'warehouse.features.units', N'واحدها و تبدیل کالا', N'Warehouse', N'Features', N'Index', N'units', 52),
            (N'warehouse.features.product-units', N'واحد و ضریب تبدیل کالا', N'Warehouse', N'Features', N'Index', N'product-units', 53),
            (N'warehouse.features.variants', N'تنوع کالا', N'Warehouse', N'Features', N'Index', N'variants', 54),
            (N'warehouse.features.document-types', N'انواع اسناد', N'Warehouse', N'Features', N'Index', N'document-types', 55),
            (N'warehouse.features.sales-plan-mapping', N'اتصال مالک به انبارها', N'Warehouse', N'Features', N'Index', N'sales-plan-mapping', 56),
            (N'warehouse.features.stock-owners', N'مالک کالا و طرح فروش', N'Warehouse', N'Features', N'Index', N'stock-owners', 57);

        INSERT INTO [Admin].[Permission]
            ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
        SELECT page.[PermissionKey], page.[Name], NULL, page.[Area], page.[Controller], page.[Action],
               page.[FeatureKey], page.[SortOrder], 1
        FROM @PagePermissions page
        WHERE NOT EXISTS
        (
            SELECT 1 FROM [Admin].[Permission] existing
            WHERE existing.[PermissionKey] = page.[PermissionKey]
               OR (existing.[Area] = page.[Area]
                   AND existing.[Controller] = page.[Controller]
                   AND existing.[Action] = page.[Action]
                   AND ISNULL(existing.[FeatureKey], N'') = ISNULL(page.[FeatureKey], N''))
        );

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'081-admin-page-permissions');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
