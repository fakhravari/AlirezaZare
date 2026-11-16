SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'098-admin-menu-permission-groups')
    BEGIN
        IF OBJECT_ID(N'[Admin].[Permission]', N'U') IS NULL OR OBJECT_ID(N'[Admin].[RolePermission]', N'U') IS NULL
            THROW 51098, 'Admin permission tables must exist before applying migration 098.', 1;

        DECLARE @EffectiveRolePermissions TABLE
        (
            [IdRole] BIGINT NOT NULL,
            [IdPermission] BIGINT NOT NULL,
            PRIMARY KEY ([IdRole], [IdPermission])
        );

        ;WITH PermissionTree AS
        (
            SELECT rolePermission.[IdRole], permission.[Id], permission.[PermissionKey]
            FROM [Admin].[RolePermission] rolePermission
            INNER JOIN [Admin].[Role] role ON role.[Id] = rolePermission.[IdRole]
            INNER JOIN [Admin].[Permission] permission ON permission.[Id] = rolePermission.[IdPermission]
            WHERE role.[RoleKey] <> N'system.full-access' AND permission.[IsActive] = 1
            UNION ALL
            SELECT tree.[IdRole], child.[Id], child.[PermissionKey]
            FROM PermissionTree tree
            INNER JOIN [Admin].[Permission] child ON child.[ParentPermissionKey] = tree.[PermissionKey]
            WHERE child.[IsActive] = 1
        )
        INSERT INTO @EffectiveRolePermissions ([IdRole], [IdPermission])
        SELECT DISTINCT [IdRole], [Id]
        FROM PermissionTree
        WHERE [PermissionKey] <> N'store.manager'
        OPTION (MAXRECURSION 32767);

        DECLARE @Groups TABLE
        (
            [PermissionKey] NVARCHAR(160) NOT NULL PRIMARY KEY,
            [Name] NVARCHAR(200) NOT NULL,
            [SortOrder] INT NOT NULL
        );
        INSERT INTO @Groups ([PermissionKey], [Name], [SortOrder])
        VALUES
            (N'admin.overview.group', N'نمای کلی', 1),
            (N'store.manager', N'مدیریت فروشگاه', 10),
            (N'store.catalog.group', N'کاتالوگ', 11),
            (N'store.sales.group', N'فروش و سفارش', 12),
            (N'store.pricing.group', N'طرح‌های فروش و تخفیف', 13),
            (N'store.customers.group', N'مشتریان و پشتیبانی', 14),
            (N'store.reports.group', N'گزارش‌های فروشگاه', 15),
            (N'store.setup.group', N'محتوا و تنظیمات فروشگاه', 16),
            (N'store.content.group', N'مدیریت محتوا', 17),
            (N'store.settings.group', N'تنظیمات فروشگاه', 18),
            (N'warehouse.management.group', N'مدیریت انبار', 20),
            (N'warehouse.operations.group', N'عملیات انبار', 21),
            (N'warehouse.structure.group', N'انبار و موجودی', 22),
            (N'warehouse.reports.group', N'گزارش‌های انبار', 23),
            (N'warehouse.definitions', N'تنظیمات و تعاریف', 24);

        INSERT INTO [Admin].[Permission]
            ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
        SELECT groups.[PermissionKey], groups.[Name], NULL, N'', N'', N'', NULL, groups.[SortOrder], 1
        FROM @Groups groups
        WHERE NOT EXISTS (SELECT 1 FROM [Admin].[Permission] existing WHERE existing.[PermissionKey] = groups.[PermissionKey]);

        UPDATE permission
        SET permission.[Name] = groups.[Name], permission.[SortOrder] = groups.[SortOrder], permission.[IsActive] = 1
        FROM [Admin].[Permission] permission
        INNER JOIN @Groups groups ON groups.[PermissionKey] = permission.[PermissionKey];

        UPDATE permission
        SET permission.[ParentPermissionKey] = CASE groups.[PermissionKey]
                WHEN N'store.catalog.group' THEN N'store.manager'
                WHEN N'store.sales.group' THEN N'store.manager'
                WHEN N'store.pricing.group' THEN N'store.manager'
                WHEN N'store.customers.group' THEN N'store.manager'
                WHEN N'store.reports.group' THEN N'store.manager'
                WHEN N'store.setup.group' THEN N'store.manager'
                WHEN N'store.content.group' THEN N'store.setup.group'
                WHEN N'store.settings.group' THEN N'store.setup.group'
                WHEN N'warehouse.operations.group' THEN N'warehouse.management.group'
                WHEN N'warehouse.structure.group' THEN N'warehouse.management.group'
                WHEN N'warehouse.reports.group' THEN N'warehouse.management.group'
                WHEN N'warehouse.definitions' THEN N'warehouse.management.group'
                ELSE NULL END
        FROM [Admin].[Permission] permission
        INNER JOIN @Groups groups ON groups.[PermissionKey] = permission.[PermissionKey];

        UPDATE permission
        SET permission.[ParentPermissionKey] = CASE
            WHEN permission.[Area] = N'Admin' AND permission.[Controller] = N'Home' THEN N'admin.overview.group'
            WHEN permission.[Area] = N'Admin' AND permission.[Controller] = N'SiteSettings' THEN N'store.settings.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Dashboard' THEN N'store.manager'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Catalog' THEN N'store.catalog.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] IN (N'orders', N'paid-orders-dispatch') THEN N'store.sales.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] = N'promotions' THEN N'store.pricing.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] IN (N'customers', N'reviews', N'questions', N'contact-messages', N'account-messages', N'newsletter-subscribers') THEN N'store.customers.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] = N'reports' THEN N'store.reports.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] = N'content' THEN N'store.content.group'
            WHEN permission.[Area] = N'Store' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] = N'geography' THEN N'store.settings.group'
            WHEN permission.[Area] = N'Warehouse' AND permission.[Controller] = N'Dashboard' THEN N'warehouse.management.group'
            WHEN permission.[Area] = N'Warehouse' AND permission.[Controller] IN (N'Warehouses', N'Stock') THEN N'warehouse.structure.group'
            WHEN permission.[Area] = N'Warehouse' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] = N'companies' THEN N'warehouse.structure.group'
            WHEN permission.[Area] = N'Warehouse' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] = N'movements' THEN N'warehouse.reports.group'
            WHEN permission.[Area] = N'Warehouse' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] IN (N'order-confirmation', N'receipts', N'dispatches', N'transfers', N'returns', N'stocktake', N'adjustments') THEN N'warehouse.operations.group'
            WHEN permission.[Area] = N'Warehouse' AND permission.[Controller] = N'Features' AND permission.[FeatureKey] IN (N'references', N'units', N'product-units', N'variants', N'document-types', N'sales-plan-mapping', N'stock-owners') THEN N'warehouse.definitions'
            ELSE permission.[ParentPermissionKey]
        END
        FROM [Admin].[Permission] permission
        WHERE permission.[IsActive] = 1 AND permission.[Area] <> N'' AND permission.[Controller] <> N'' AND permission.[Action] <> N'';

        UPDATE [Admin].[Permission]
        SET [Name] = N'شرکت‌ها'
        WHERE [Area] = N'Warehouse' AND [Controller] = N'Features' AND [FeatureKey] = N'companies';

        DELETE FROM [Admin].[RolePermission]
        WHERE [IdPermission] = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.manager');

        DELETE rolePermission
        FROM [Admin].[RolePermission] rolePermission
        INNER JOIN @EffectiveRolePermissions effective
            ON effective.[IdRole] = rolePermission.[IdRole] AND effective.[IdPermission] = rolePermission.[IdPermission];
        INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT effective.[IdRole], effective.[IdPermission]
        FROM @EffectiveRolePermissions effective
        WHERE NOT EXISTS
        (
            SELECT 1 FROM [Admin].[RolePermission] existing
            WHERE existing.[IdRole] = effective.[IdRole] AND existing.[IdPermission] = effective.[IdPermission]
        );

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'098-admin-menu-permission-groups');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
