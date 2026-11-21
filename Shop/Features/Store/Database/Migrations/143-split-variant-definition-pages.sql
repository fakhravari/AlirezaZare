SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51430, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'143-split-variant-definition-pages')
    BEGIN
        DECLARE @DefinitionsGroup NVARCHAR(160) = N'system.definitions.group';
        DECLARE @VariantKey NVARCHAR(160) = N'warehouse.variants';
        DECLARE @VariantNamesKey NVARCHAR(160) = N'warehouse.variant-names';
        DECLARE @VariantPermissionId BIGINT = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = @VariantKey AND [IsActive] = 1);
        IF @VariantPermissionId IS NULL
            THROW 51431, 'Active variant definition permission is missing.', 1;
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @DefinitionsGroup AND [IsActive] = 1)
            THROW 51432, 'Active system definitions permission group is missing.', 1;
        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @VariantKey AND ([Area] <> N'Warehouse' OR [Controller] <> N'Features' OR [Action] <> N'Index' OR [FeatureKey] <> N'variants'))
            THROW 51433, 'Variant definition permission route does not match the application.', 1;

        DECLARE @Before TABLE ([IdRole] BIGINT NOT NULL, [IdPermission] BIGINT NOT NULL, PRIMARY KEY ([IdRole], [IdPermission]));
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
        INSERT @Before ([IdRole], [IdPermission])
        SELECT DISTINCT effective.[IdRole], effective.[Id]
        FROM EffectivePermissions effective
        JOIN [Admin].[Permission] route ON route.[Id] = effective.[Id]
        WHERE route.[Area] <> N'' AND route.[Controller] <> N'' AND route.[Action] <> N''
        OPTION (MAXRECURSION 32767);

        DECLARE @VariantRoles TABLE ([IdRole] BIGINT NOT NULL PRIMARY KEY);
        INSERT @VariantRoles ([IdRole]) SELECT DISTINCT [IdRole] FROM @Before WHERE [IdPermission] = @VariantPermissionId;

        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @VariantNamesKey)
            UPDATE [Admin].[Permission]
            SET [Name] = N'مرجع درختی نام تنوع', [ParentPermissionKey] = @DefinitionsGroup,
                [Area] = N'Warehouse', [Controller] = N'Features', [Action] = N'Index',
                [FeatureKey] = N'variant-names', [SortOrder] = 64, [IsActive] = 1
            WHERE [PermissionKey] = @VariantNamesKey;
        ELSE
            INSERT [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (@VariantNamesKey, N'مرجع درختی نام تنوع', @DefinitionsGroup, N'Warehouse', N'Features', N'Index', N'variant-names', 64, 1);

        UPDATE [Admin].[Permission]
        SET [Name] = N'فهرست تعریف‌ها', [ParentPermissionKey] = @DefinitionsGroup,
            [Area] = N'Warehouse', [Controller] = N'Features', [Action] = N'Index',
            [FeatureKey] = N'variants', [IsActive] = 1
        WHERE [Id] = @VariantPermissionId;

        DECLARE @VariantNamesPermissionId BIGINT = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = @VariantNamesKey AND [IsActive] = 1);
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
        SELECT role.[IdRole], @VariantNamesPermissionId
        FROM @VariantRoles role
        WHERE NOT EXISTS
        (
            SELECT 1 FROM EffectivePermissions effective
            WHERE effective.[IdRole] = role.[IdRole] AND effective.[Id] = @VariantNamesPermissionId
        )
        OPTION (MAXRECURSION 32767);

        DECLARE @After TABLE ([IdRole] BIGINT NOT NULL, [IdPermission] BIGINT NOT NULL, PRIMARY KEY ([IdRole], [IdPermission]));
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
        INSERT @After ([IdRole], [IdPermission])
        SELECT DISTINCT effective.[IdRole], effective.[Id]
        FROM EffectivePermissions effective
        JOIN [Admin].[Permission] route ON route.[Id] = effective.[Id]
        WHERE route.[Area] <> N'' AND route.[Controller] <> N'' AND route.[Action] <> N''
        OPTION (MAXRECURSION 32767);

        IF EXISTS (SELECT [IdRole], [IdPermission] FROM @Before EXCEPT SELECT [IdRole], [IdPermission] FROM @After)
           OR EXISTS
           (
               SELECT [IdRole], [IdPermission] FROM @After WHERE [IdPermission] <> @VariantNamesPermissionId
               EXCEPT
               SELECT [IdRole], [IdPermission] FROM @Before
           )
           OR EXISTS
           (
               SELECT [IdRole] FROM @After WHERE [IdPermission] = @VariantNamesPermissionId
               EXCEPT
               SELECT [IdRole] FROM @VariantRoles
           )
           OR EXISTS
           (
               SELECT [IdRole] FROM @VariantRoles
               EXCEPT
               SELECT [IdRole] FROM @After WHERE [IdPermission] = @VariantNamesPermissionId
           )
            THROW 51434, 'Splitting variant definition pages changed unrelated effective route permissions.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'143-split-variant-definition-pages');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
