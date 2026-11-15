SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'090-newsletter-permission-store-manager')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.manager')
        BEGIN
            INSERT INTO [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (N'store.manager', N'مدیریت فروشگاه', NULL, N'', N'', N'', NULL, 1, 1);
        END;

        UPDATE [Admin].[Permission]
        SET [ParentPermissionKey] = N'store.manager'
        WHERE [PermissionKey] = N'store.newsletter-subscribers.page';

        INSERT INTO [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT r.[Id], p.[Id]
        FROM [Admin].[Role] r
        CROSS JOIN [Admin].[Permission] p
        WHERE r.[RoleKey] = N'system.full-access-store'
          AND p.[PermissionKey] = N'store.manager'
          AND NOT EXISTS
          (
              SELECT 1
              FROM [Admin].[RolePermission] existing
              WHERE existing.[IdRole] = r.[Id]
                AND existing.[IdPermission] = p.[Id]
          );

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'090-newsletter-permission-store-manager');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
