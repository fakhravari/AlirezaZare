SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'091-store-reports-permissions')
    BEGIN
        DECLARE @StoreManagerRoleId BIGINT = (SELECT [Id] FROM [Admin].[Role] WHERE [RoleKey] = N'system.full-access-store');
        IF @StoreManagerRoleId IS NULL
            THROW 51091, 'system.full-access-store role is missing.', 1;

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.manager')
            INSERT INTO [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (N'store.manager', N'مدیریت فروشگاه', NULL, N'', N'', N'', NULL, 1, 1);

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.features.reports')
            INSERT INTO [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (N'store.features.reports', N'گزارشات فروشگاه', N'store.manager', N'Store', N'Features', N'Index', N'reports', 50, 1);

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.reports.read')
            INSERT INTO [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (N'store.reports.read', N'مشاهدهٔ گزارشات فروشگاه', N'store.features.reports', N'Store', N'StoreReportsAdminApi', N'GetReport', NULL, 51, 1);

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.reports.export')
            INSERT INTO [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (N'store.reports.export', N'خروجی گزارشات فروشگاه', N'store.features.reports', N'Store', N'StoreReportsAdminApi', N'Export', NULL, 52, 1);

        UPDATE [Admin].[Permission]
        SET [ParentPermissionKey] = N'store.manager'
        WHERE [PermissionKey] = N'store.features.reports';

        INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT @StoreManagerRoleId, p.[Id]
        FROM [Admin].[Permission] p
        WHERE p.[PermissionKey] = N'store.manager'
          AND NOT EXISTS (SELECT 1 FROM [Admin].[RolePermission] rp WHERE rp.[IdRole] = @StoreManagerRoleId AND rp.[IdPermission] = p.[Id]);

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'091-store-reports-permissions');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
