SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'114-badge-permission-group')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Admin.Permission WHERE PermissionKey = N'store.catalog.group' AND IsActive = 1)
            THROW 51220, 'Active catalog permission group is missing.', 1;
        IF NOT EXISTS (SELECT 1 FROM Admin.Permission WHERE PermissionKey = N'store.catalog.badges' AND IsActive = 1)
            THROW 51221, 'Active badge permission is missing.', 1;
        UPDATE Admin.Permission SET ParentPermissionKey = N'store.catalog.group'
        WHERE PermissionKey = N'store.catalog.badges';
        INSERT Admin.RolePermission (IdRole, IdPermission)
        SELECT r.Id, p.Id FROM Admin.Role r CROSS JOIN Admin.Permission p
        WHERE r.RoleKey = N'system.full-access-store' AND r.IsActive = 1
          AND p.PermissionKey = N'store.catalog.badges'
          AND NOT EXISTS (SELECT 1 FROM Admin.RolePermission rp WHERE rp.IdRole = r.Id AND rp.IdPermission = p.Id);
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'114-badge-permission-group');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
