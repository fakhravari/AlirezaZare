SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'056-admin-user-page-permissions')
BEGIN

IF OBJECT_ID(N'[Admin].[Permission]', N'U') IS NULL
    THROW 51056, 'Admin.Permission must exist before applying migration 056.', 1;
IF OBJECT_ID(N'[Admin].[RolePermission]', N'U') IS NULL
    THROW 51056, 'Admin.RolePermission must exist before applying migration 056.', 1;

DECLARE @LegacyPermissionId BIGINT = NULL;
SELECT TOP (1) @LegacyPermissionId = [Id]
FROM [Admin].[Permission]
WHERE [Area] = N'Admin' AND [Controller] = N'Users' AND [Action] = N'Index'
  AND NULLIF([FeatureKey], N'') IS NULL
ORDER BY [Id];

IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'admin.users.management')
    INSERT INTO [Admin].[Permission]
        ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
    VALUES
        (N'admin.users.management', N'کاربران سیستم و دسترسی‌ها', NULL, N'', N'', N'', NULL, 10, 1);

IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'admin.users.page.users')
    INSERT INTO [Admin].[Permission]
        ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
    VALUES
        (N'admin.users.page.users', N'کاربران پنل', N'admin.users.management', N'Admin', N'Users', N'Index', N'users', 11, 1);

IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'admin.users.page.roles')
    INSERT INTO [Admin].[Permission]
        ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
    VALUES
        (N'admin.users.page.roles', N'نقش‌ها', N'admin.users.management', N'Admin', N'Users', N'Index', N'roles', 12, 1);

IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'admin.users.page.permissions')
    INSERT INTO [Admin].[Permission]
        ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
    VALUES
        (N'admin.users.page.permissions', N'درخت مجوز نقش‌ها', N'admin.users.management', N'Admin', N'Users', N'Index', N'permissions', 13, 1);

IF @LegacyPermissionId IS NOT NULL
BEGIN
    UPDATE users
    SET [SecurityStamp] = NEWID(), [UpdatedAtUtc] = SYSUTCDATETIME()
    FROM [Admin].[SystemUser] users
    INNER JOIN [Admin].[SystemUserRole] userRoles ON userRoles.[IdUser] = users.[Id]
    INNER JOIN [Admin].[RolePermission] rolePermissions ON rolePermissions.[IdRole] = userRoles.[IdRole]
    WHERE rolePermissions.[IdPermission] = @LegacyPermissionId;

    INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
    SELECT legacy.[IdRole], page.[Id]
    FROM [Admin].[RolePermission] legacy
    CROSS JOIN [Admin].[Permission] page
    WHERE legacy.[IdPermission] = @LegacyPermissionId
      AND page.[PermissionKey] IN (N'admin.users.page.users', N'admin.users.page.roles', N'admin.users.page.permissions')
      AND NOT EXISTS
          (SELECT 1 FROM [Admin].[RolePermission] existing
           WHERE existing.[IdRole] = legacy.[IdRole] AND existing.[IdPermission] = page.[Id]);

    DELETE FROM [Admin].[RolePermission] WHERE [IdPermission] = @LegacyPermissionId;
    UPDATE [Admin].[Permission] SET [IsActive] = 0 WHERE [Id] = @LegacyPermissionId;
END;

INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
    VALUES (N'056-admin-user-page-permissions');
END;

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
