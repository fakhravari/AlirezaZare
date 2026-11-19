SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'126-assign-variant-page-permission')
    BEGIN
        DECLARE @LegacyPermissionId BIGINT = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.definitions.variants' AND [IsActive] = 1);
        DECLARE @VariantPermissionId BIGINT = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.variants' AND [IsActive] = 1);
        IF @LegacyPermissionId IS NULL OR @VariantPermissionId IS NULL
            THROW 51270, 'Legacy or relational variant page permission is missing.', 1;

        ;WITH EffectivePermissions AS
        (
            SELECT rp.[IdRole], permission.[Id], permission.[PermissionKey]
            FROM [Admin].[RolePermission] rp
            JOIN [Admin].[Permission] permission ON permission.[Id] = rp.[IdPermission]
            WHERE permission.[IsActive] = 1
            UNION ALL
            SELECT effective.[IdRole], child.[Id], child.[PermissionKey]
            FROM EffectivePermissions effective
            JOIN [Admin].[Permission] child ON child.[ParentPermissionKey] = effective.[PermissionKey]
            WHERE child.[IsActive] = 1
        )
        INSERT [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT DISTINCT effective.[IdRole], @VariantPermissionId
        FROM EffectivePermissions effective
        WHERE effective.[Id] = @LegacyPermissionId
          AND NOT EXISTS
          (
              SELECT 1 FROM [Admin].[RolePermission] existing
              WHERE existing.[IdRole] = effective.[IdRole]
                AND existing.[IdPermission] = @VariantPermissionId
          )
        OPTION (MAXRECURSION 32767);

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'126-assign-variant-page-permission');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
