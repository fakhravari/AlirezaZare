SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51470, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'147-restore-warehouse-permission-placement')
    BEGIN
        DECLARE @DefinitionsGroup NVARCHAR(160) = N'system.definitions.group';
        DECLARE @WarehouseGroup NVARCHAR(160) = N'warehouse.definitions.group';

        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @DefinitionsGroup AND [IsActive] = 1)
            THROW 51471, 'Active system definitions permission group is missing.', 1;
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @WarehouseGroup AND [IsActive] = 1)
            THROW 51472, 'Active warehouse definition group is missing.', 1;
        IF EXISTS
        (
            SELECT 1 FROM [Admin].[RolePermission] rp
            JOIN [Admin].[Permission] permission ON permission.[Id] = rp.[IdPermission]
            WHERE permission.[PermissionKey] = @WarehouseGroup
        )
            THROW 51473, 'Warehouse definition group has direct role grants.', 1;

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

        UPDATE [Admin].[Permission]
        SET [ParentPermissionKey] = @DefinitionsGroup
        WHERE [ParentPermissionKey] = @WarehouseGroup AND [Area] = N'Warehouse' AND [IsActive] = 1;

        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [ParentPermissionKey] = @WarehouseGroup AND [IsActive] = 1)
            THROW 51474, 'Warehouse definition group still has active children.', 1;

        UPDATE [Admin].[Permission] SET [IsActive] = 0 WHERE [PermissionKey] = @WarehouseGroup;

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
            THROW 51475, 'Restoring warehouse permission placement changed effective role access.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'147-restore-warehouse-permission-placement');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
