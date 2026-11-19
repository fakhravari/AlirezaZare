SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'127-move-warehouse-definitions-to-system')
    BEGIN
        DECLARE @GroupKey NVARCHAR(160) = N'system.definitions.group';
        IF EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = @GroupKey)
            UPDATE [Admin].[Permission]
            SET [Name] = N'تعاریف و دسترسی سامانه', [ParentPermissionKey] = NULL,
                [Area] = N'', [Controller] = N'', [Action] = N'', [FeatureKey] = NULL, [IsActive] = 1
            WHERE [PermissionKey] = @GroupKey;
        ELSE
            INSERT [Admin].[Permission]
                ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES
                (@GroupKey, N'تعاریف و دسترسی سامانه', NULL, N'', N'', N'', NULL, 30, 1);

        DECLARE @Targets TABLE ([Id] BIGINT NOT NULL PRIMARY KEY);
        INSERT @Targets ([Id])
        SELECT [Id]
        FROM [Admin].[Permission]
        WHERE [IsActive] = 1 AND [Area] = N'Warehouse'
          AND
          (
              ([Controller] = N'Features' AND [Action] = N'Index' AND [FeatureKey] IN
                  (N'companies', N'references', N'units', N'product-units', N'variants', N'document-types', N'sales-plan-mapping', N'stock-owners'))
              OR ([Controller] = N'Warehouses' AND [Action] = N'Index')
              OR [Controller] = N'DefinitionsApi'
          );

        IF NOT EXISTS (SELECT 1 FROM @Targets)
            THROW 51270, 'No warehouse definition permissions were found.', 1;

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
        INSERT @Before SELECT DISTINCT effective.[IdRole], effective.[Id]
        FROM EffectivePermissions effective
        JOIN [Admin].[Permission] route ON route.[Id] = effective.[Id]
        WHERE route.[Area] <> N'' AND route.[Controller] <> N'' AND route.[Action] <> N''
        OPTION (MAXRECURSION 32767);

        -- Preserve each role's current effective access before moving these permissions.
        INSERT [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT beforeMove.[IdRole], beforeMove.[IdPermission]
        FROM @Before beforeMove JOIN @Targets target ON target.[Id] = beforeMove.[IdPermission]
        WHERE NOT EXISTS
        (
            SELECT 1 FROM [Admin].[RolePermission] existing
            WHERE existing.[IdRole] = beforeMove.[IdRole] AND existing.[IdPermission] = beforeMove.[IdPermission]
        );

        UPDATE permission
        SET [ParentPermissionKey] = @GroupKey
        FROM [Admin].[Permission] permission JOIN @Targets target ON target.[Id] = permission.[Id];

        IF EXISTS
        (
            SELECT 1 FROM [Admin].[Permission] legacy
            JOIN [Admin].[Permission] child ON child.[ParentPermissionKey] = legacy.[PermissionKey] AND child.[IsActive] = 1
            WHERE legacy.[PermissionKey] = N'warehouse.structure.group' AND legacy.[IsActive] = 1
        )
            THROW 51272, 'Legacy warehouse definition permission group still has active children.', 1;
        UPDATE [Admin].[Permission] SET [IsActive] = 0 WHERE [PermissionKey] = N'warehouse.structure.group' AND [IsActive] = 1;

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
        INSERT @After SELECT DISTINCT effective.[IdRole], effective.[Id]
        FROM EffectivePermissions effective
        JOIN [Admin].[Permission] route ON route.[Id] = effective.[Id]
        WHERE route.[Area] <> N'' AND route.[Controller] <> N'' AND route.[Action] <> N''
        OPTION (MAXRECURSION 32767);

        IF EXISTS (SELECT [IdRole], [IdPermission] FROM @Before EXCEPT SELECT [IdRole], [IdPermission] FROM @After)
           OR EXISTS (SELECT [IdRole], [IdPermission] FROM @After EXCEPT SELECT [IdRole], [IdPermission] FROM @Before)
            THROW 51271, 'Moving warehouse definitions would change effective role permissions.', 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'127-move-warehouse-definitions-to-system');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
