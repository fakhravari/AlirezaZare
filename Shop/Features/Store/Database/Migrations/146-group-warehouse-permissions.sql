SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51460, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'146-group-warehouse-permissions')
    BEGIN
        DECLARE @DefinitionsGroup NVARCHAR(160) = N'system.definitions.group';
        DECLARE @WarehouseGroup NVARCHAR(160) = N'warehouse.definitions.group';

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @DefinitionsGroup AND [IsActive] = 1)
            THROW 51461, 'Active system definitions permission group is missing.', 1;

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

        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @WarehouseGroup)
            UPDATE [Admin].[Permission]
            SET [Name] = N'مدیریت انبار', [ParentPermissionKey] = @DefinitionsGroup,
                [Area] = N'', [Controller] = N'', [Action] = N'', [FeatureKey] = NULL,
                [SortOrder] = 60, [IsActive] = 1
            WHERE [PermissionKey] = @WarehouseGroup;
        ELSE
            INSERT [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (@WarehouseGroup, N'مدیریت انبار', @DefinitionsGroup, N'', N'', N'', NULL, 60, 1);

        UPDATE [Admin].[Permission]
        SET [ParentPermissionKey] = @WarehouseGroup
        WHERE [ParentPermissionKey] = @DefinitionsGroup AND [Area] = N'Warehouse' AND [IsActive] = 1;

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
           OR EXISTS (SELECT [IdRole], [IdPermission] FROM @After EXCEPT SELECT [IdRole], [IdPermission] FROM @Before)
            THROW 51462, 'Grouping warehouse permissions changed effective role access.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'146-group-warehouse-permissions');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
