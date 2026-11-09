SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'Admin') IS NULL EXEC(N'CREATE SCHEMA [Admin]');

    IF OBJECT_ID(N'[Admin].[SchemaMigration]', N'U') IS NULL
        CREATE TABLE [Admin].[SchemaMigration]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Admin_SchemaMigration] PRIMARY KEY,
            [MigrationKey] NVARCHAR(100) NOT NULL,
            [AppliedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Admin_SchemaMigration_AppliedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [UQ_Admin_SchemaMigration_MigrationKey] UNIQUE ([MigrationKey])
        );

    IF EXISTS (SELECT 1 FROM [Admin].[SchemaMigration] WHERE [MigrationKey] = N'001-system-users-and-permissions')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    CREATE TABLE [Admin].[SystemUser]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Admin_SystemUser] PRIMARY KEY,
        [UserName] NVARCHAR(100) NOT NULL,
        [NormalizedUserName] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(160) NOT NULL,
        [PasswordHash] NVARCHAR(500) NOT NULL,
        [SecurityStamp] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Admin_SystemUser_SecurityStamp] DEFAULT NEWID(),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Admin_SystemUser_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Admin_SystemUser_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [LastLoginAtUtc] DATETIME2(3) NULL,
        [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Admin_SystemUser_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [UQ_Admin_SystemUser_NormalizedUserName] UNIQUE ([NormalizedUserName]),
        CONSTRAINT [CK_Admin_SystemUser_UserName_NotEmpty] CHECK (LEN(LTRIM(RTRIM([UserName]))) > 0),
        CONSTRAINT [CK_Admin_SystemUser_DisplayName_NotEmpty] CHECK (LEN(LTRIM(RTRIM([DisplayName]))) > 0),
        CONSTRAINT [CK_Admin_SystemUser_PasswordHash_NotEmpty] CHECK (LEN([PasswordHash]) > 0)
    );

    CREATE TABLE [Admin].[Role]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Admin_Role] PRIMARY KEY,
        [RoleKey] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(160) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Admin_Role_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Admin_Role_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [UQ_Admin_Role_RoleKey] UNIQUE ([RoleKey]),
        CONSTRAINT [CK_Admin_Role_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Admin].[Permission]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Admin_Permission] PRIMARY KEY,
        [PermissionKey] NVARCHAR(160) NOT NULL,
        [Name] NVARCHAR(160) NOT NULL,
        [ParentPermissionKey] NVARCHAR(160) NULL,
        [Area] NVARCHAR(80) NOT NULL CONSTRAINT [DF_Admin_Permission_Area] DEFAULT (N''),
        [Controller] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Admin_Permission_Controller] DEFAULT (N''),
        [Action] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Admin_Permission_Action] DEFAULT (N''),
        [FeatureKey] NVARCHAR(100) NULL,
        [SortOrder] INT NOT NULL CONSTRAINT [DF_Admin_Permission_SortOrder] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Admin_Permission_IsActive] DEFAULT (1),
        CONSTRAINT [UQ_Admin_Permission_Key] UNIQUE ([PermissionKey]),
        CONSTRAINT [FK_Admin_Permission_Parent] FOREIGN KEY ([ParentPermissionKey]) REFERENCES [Admin].[Permission] ([PermissionKey]),
        CONSTRAINT [CK_Admin_Permission_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Admin].[SystemUserRole]
    (
        [IdUser] BIGINT NOT NULL,
        [IdRole] BIGINT NOT NULL,
        CONSTRAINT [PK_Admin_SystemUserRole] PRIMARY KEY ([IdUser], [IdRole]),
        CONSTRAINT [FK_Admin_SystemUserRole_User] FOREIGN KEY ([IdUser]) REFERENCES [Admin].[SystemUser] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Admin_SystemUserRole_Role] FOREIGN KEY ([IdRole]) REFERENCES [Admin].[Role] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [Admin].[RolePermission]
    (
        [IdRole] BIGINT NOT NULL,
        [IdPermission] BIGINT NOT NULL,
        CONSTRAINT [PK_Admin_RolePermission] PRIMARY KEY ([IdRole], [IdPermission]),
        CONSTRAINT [FK_Admin_RolePermission_Role] FOREIGN KEY ([IdRole]) REFERENCES [Admin].[Role] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Admin_RolePermission_Permission] FOREIGN KEY ([IdPermission]) REFERENCES [Admin].[Permission] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [Admin].[SystemUserWarehouseAccess]
    (
        [IdUser] BIGINT NOT NULL,
        [IdWarehouse] BIGINT NOT NULL,
        [GrantedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Admin_SystemUserWarehouseAccess_GrantedAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Admin_SystemUserWarehouseAccess] PRIMARY KEY ([IdUser], [IdWarehouse]),
        CONSTRAINT [FK_Admin_SystemUserWarehouseAccess_User] FOREIGN KEY ([IdUser]) REFERENCES [Admin].[SystemUser] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Admin_SystemUserWarehouseAccess_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id])
    );

    INSERT INTO [Admin].[Role] ([RoleKey], [Name]) VALUES (N'system.full-access', N'مدیر کل سامانه');

    INSERT INTO [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [SortOrder]) VALUES
        (N'admin.root', N'پیشخوان سامانه', NULL, N'Admin', N'Home', N'Index', 10),
        (N'store.root', N'مدیریت فروشگاه', NULL, N'Store', N'', N'', 20),
        (N'store.dashboard', N'پیشخوان فروشگاه', N'store.root', N'Store', N'Dashboard', N'Index', 10),
        (N'store.catalog', N'کاتالوگ', N'store.root', N'Store', N'Catalog', N'', 20),
        (N'store.catalog.products', N'محصولات', N'store.catalog', N'Store', N'Catalog', N'Products', 10),
        (N'store.catalog.categories', N'دسته‌بندی‌ها', N'store.catalog', N'Store', N'Catalog', N'Categories', 20),
        (N'store.catalog.brands', N'برندها', N'store.catalog', N'Store', N'Catalog', N'Brands', 30),
        (N'store.catalog.attributes', N'ویژگی‌ها', N'store.catalog', N'Store', N'Catalog', N'Attributes', 40),
        (N'store.orders', N'سفارش‌ها', N'store.root', N'Store', N'Features', N'Index', 30),
        (N'store.promotions', N'طرح‌های فروش', N'store.root', N'Store', N'Features', N'Index', 40),
        (N'store.customers', N'مشتریان', N'store.root', N'Store', N'Features', N'Index', 50),
        (N'store.reviews', N'دیدگاه‌های محصول', N'store.root', N'Store', N'Features', N'Index', 60),
        (N'store.questions', N'پرسش‌های محصول', N'store.root', N'Store', N'Features', N'Index', 65),
        (N'store.content', N'محتوا', N'store.root', N'Store', N'Features', N'Index', 70),
        (N'warehouse.root', N'مدیریت انبار', NULL, N'Warehouse', N'', N'', 30),
        (N'warehouse.dashboard', N'پیشخوان انبار', N'warehouse.root', N'Warehouse', N'Dashboard', N'Index', 10),
        (N'warehouse.warehouses', N'انبارها', N'warehouse.root', N'Warehouse', N'Warehouses', N'Index', 20),
        (N'warehouse.stock', N'موجودی کالا', N'warehouse.root', N'Warehouse', N'Stock', N'Index', 30),
        (N'warehouse.documents', N'سندهای انبار', N'warehouse.root', N'', N'', N'', 40),
        (N'warehouse.orders', N'تأیید سفارش‌ها', N'warehouse.root', N'Warehouse', N'Features', N'Index', 50),
        (N'warehouse.definitions', N'تعاریف انبار', N'warehouse.root', N'', N'', N'', 60),
        (N'admin.users', N'کاربران و نقش‌های سامانه', N'admin.root', N'Admin', N'Users', N'Index', 20),
        (N'admin.users.read', N'مشاهدهٔ کاربران سیستم', N'admin.users', N'Admin', N'UsersApi', N'GetUsers', 10),
        (N'admin.users.write', N'مدیریت کاربران و دسترسی‌ها', N'admin.users', N'Admin', N'UsersApi', N'SaveUser', 20),
        (N'admin.roles.write', N'مدیریت نقش‌ها و مجوزها', N'admin.users', N'Admin', N'UsersApi', N'SaveRole', 30),
        (N'admin.roles.create', N'ایجاد نقش', N'admin.users', N'Admin', N'UsersApi', N'CreateRole', 35),
        (N'admin.password.reset', N'بازنشانی گذرواژهٔ کاربران', N'admin.users', N'Admin', N'UsersApi', N'ResetPassword', 40),
        (N'store.catalog.api', N'خواندن دادهٔ کاتالوگ', N'store.catalog', N'Store', N'StoreCatalogAdminApi', N'GetSection', 50),
        (N'warehouse.api.warehouses', N'خواندن انبارها', N'warehouse.root', N'Warehouse', N'WarehouseAdminApi', N'GetWarehouses', 70),
        (N'warehouse.api.stock', N'خواندن موجودی', N'warehouse.root', N'Warehouse', N'WarehouseAdminApi', N'GetStock', 80),
        (N'warehouse.api.minimum', N'تنظیم حداقل موجودی طرح عادی', N'warehouse.root', N'Warehouse', N'WarehouseAdminApi', N'SetMinimum', 85),
        (N'warehouse.documents.read', N'مشاهدهٔ سندهای انبار', N'warehouse.documents', N'Warehouse', N'DocumentsApi', N'List', 10),
        (N'warehouse.documents.detail', N'جزئیات و داده‌های فرم سند', N'warehouse.documents', N'Warehouse', N'DocumentsApi', N'Detail', 20),
        (N'warehouse.documents.lookups', N'گزینه‌های فرم سند انبار', N'warehouse.documents', N'Warehouse', N'DocumentsApi', N'Lookups', 30),
        (N'warehouse.documents.create', N'ثبت سند انبار', N'warehouse.documents', N'Warehouse', N'DocumentsApi', N'Create', 40),
        (N'warehouse.documents.update', N'ویرایش سند انبار', N'warehouse.documents', N'Warehouse', N'DocumentsApi', N'Update', 50),
        (N'warehouse.documents.delete', N'حذف سند انبار', N'warehouse.documents', N'Warehouse', N'DocumentsApi', N'Delete', 60),
        (N'warehouse.movements.read', N'مشاهدهٔ گردش انبار', N'warehouse.root', N'Warehouse', N'WarehouseMovementsApi', N'Get', 55),
        (N'warehouse.documents.receipts', N'صفحهٔ رسید کالا', N'warehouse.documents', N'Warehouse', N'Features', N'Index', 70),
        (N'warehouse.documents.dispatches', N'صفحهٔ حوالهٔ خروج', N'warehouse.documents', N'Warehouse', N'Features', N'Index', 71),
        (N'warehouse.documents.transfers', N'صفحهٔ انتقال بین انبارها', N'warehouse.documents', N'Warehouse', N'Features', N'Index', 72),
        (N'warehouse.documents.returns', N'صفحهٔ مرجوعی', N'warehouse.documents', N'Warehouse', N'Features', N'Index', 73),
        (N'warehouse.documents.stocktake', N'صفحهٔ انبارگردانی', N'warehouse.documents', N'Warehouse', N'Features', N'Index', 74),
        (N'warehouse.documents.adjustments', N'صفحهٔ اصلاح موجودی', N'warehouse.documents', N'Warehouse', N'Features', N'Index', 75),
        (N'warehouse.definitions.companies', N'صفحهٔ شرکت‌ها', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 80),
        (N'warehouse.definitions.references', N'صفحهٔ مراجع انبار', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 81),
        (N'warehouse.definitions.units', N'صفحهٔ واحدها', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 82),
        (N'warehouse.definitions.product-units', N'صفحهٔ واحدهای کالا', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 83),
        (N'warehouse.definitions.variants', N'صفحهٔ تنوع کالا', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 84),
        (N'warehouse.definitions.document-types', N'صفحهٔ انواع اسناد', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 84),
        (N'warehouse.definitions.sales-plan-mapping', N'صفحهٔ اتصال طرح فروش', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 85),
        (N'warehouse.definitions.stock-owners', N'صفحهٔ مالک کالا', N'warehouse.definitions', N'Warehouse', N'Features', N'Index', 86),
        (N'warehouse.movements', N'صفحهٔ گردش کالا', N'warehouse.root', N'Warehouse', N'Features', N'Index', 86),
        (N'warehouse.definitions.read', N'مشاهدهٔ تعاریف انبار', N'warehouse.definitions', N'Warehouse', N'DefinitionsApi', N'Get', 10),
        (N'warehouse.definitions.create', N'ساخت تعاریف انبار', N'warehouse.definitions', N'Warehouse', N'DefinitionsApi', N'Create', 20),
        (N'warehouse.definitions.update', N'ویرایش تعاریف انبار', N'warehouse.definitions', N'Warehouse', N'DefinitionsApi', N'Save', 30),
        (N'warehouse.definitions.deactivate', N'غیرفعال‌سازی تعاریف انبار', N'warehouse.definitions', N'Warehouse', N'DefinitionsApi', N'Deactivate', 40),
        (N'warehouse.definitions.mapping', N'تنظیم انبار فروش سایت', N'warehouse.definitions', N'Warehouse', N'DefinitionsApi', N'SaveMapping', 50);

    UPDATE [Admin].[Permission] SET [FeatureKey] = CASE [PermissionKey]
        WHEN N'store.orders' THEN N'orders' WHEN N'store.promotions' THEN N'promotions'
        WHEN N'store.customers' THEN N'customers' WHEN N'store.reviews' THEN N'reviews'
        WHEN N'store.questions' THEN N'questions' WHEN N'store.content' THEN N'content'
        WHEN N'warehouse.orders' THEN N'order-confirmation'
        WHEN N'warehouse.documents.receipts' THEN N'receipts' WHEN N'warehouse.documents.dispatches' THEN N'dispatches'
        WHEN N'warehouse.documents.transfers' THEN N'transfers' WHEN N'warehouse.documents.returns' THEN N'returns'
        WHEN N'warehouse.documents.stocktake' THEN N'stocktake' WHEN N'warehouse.documents.adjustments' THEN N'adjustments'
        WHEN N'warehouse.definitions.companies' THEN N'companies' WHEN N'warehouse.definitions.references' THEN N'references'
        WHEN N'warehouse.definitions.units' THEN N'units' WHEN N'warehouse.definitions.product-units' THEN N'product-units'
        WHEN N'warehouse.definitions.variants' THEN N'variants' WHEN N'warehouse.definitions.document-types' THEN N'document-types'
        WHEN N'warehouse.definitions.sales-plan-mapping' THEN N'sales-plan-mapping'
        WHEN N'warehouse.definitions.stock-owners' THEN N'stock-owners'
        WHEN N'warehouse.movements' THEN N'movements' END
    WHERE [PermissionKey] IN (N'store.orders',N'store.promotions',N'store.customers',N'store.reviews',N'store.questions',N'store.content',
        N'warehouse.orders',N'warehouse.documents.receipts',N'warehouse.documents.dispatches',N'warehouse.documents.transfers',
        N'warehouse.documents.returns',N'warehouse.documents.stocktake',N'warehouse.documents.adjustments',N'warehouse.definitions.companies',
        N'warehouse.definitions.references',N'warehouse.definitions.units',N'warehouse.definitions.product-units',N'warehouse.definitions.variants',N'warehouse.definitions.document-types',
        N'warehouse.definitions.sales-plan-mapping',N'warehouse.definitions.stock-owners',N'warehouse.movements');

    UPDATE [Admin].[Permission]
    SET [ParentPermissionKey] = N'warehouse.movements'
    WHERE [PermissionKey] = N'warehouse.movements.read';

    INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
    SELECT role.[Id], permission.[Id]
    FROM [Admin].[Role] AS role CROSS JOIN [Admin].[Permission] AS permission
    WHERE role.[RoleKey] = N'system.full-access';

    INSERT INTO [Admin].[SchemaMigration] ([MigrationKey]) VALUES (N'001-system-users-and-permissions');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
