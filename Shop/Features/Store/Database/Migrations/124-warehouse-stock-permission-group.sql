SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'124-warehouse-stock-permission-group')
    BEGIN
        DECLARE @StockId BIGINT = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.stock' AND [IsActive] = 1 AND [Area] = N'Warehouse' AND [Controller] = N'Stock' AND [Action] = N'Index');
        DECLARE @ReportsId BIGINT = (SELECT [Id] FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.reports.group' AND [IsActive] = 1 AND [Area] = N'' AND [Controller] = N'' AND [Action] = N'');
        IF @StockId IS NULL OR @ReportsId IS NULL OR NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.structure.group' AND [IsActive] = 1)
            THROW 51240, 'Required warehouse permissions are missing or invalid.', 1;

        DECLARE @Before TABLE ([IdRole] BIGINT NOT NULL, [IdPermission] BIGINT NOT NULL, PRIMARY KEY ([IdRole], [IdPermission]));
        ;WITH PermissionTree AS
        (
            SELECT rp.[IdRole], p.[Id], p.[PermissionKey]
            FROM [Admin].[RolePermission] rp JOIN [Admin].[Permission] p ON p.[Id] = rp.[IdPermission]
            WHERE p.[IsActive] = 1
            UNION ALL
            SELECT tree.[IdRole], child.[Id], child.[PermissionKey]
            FROM PermissionTree tree JOIN [Admin].[Permission] child ON child.[ParentPermissionKey] = tree.[PermissionKey]
            WHERE child.[IsActive] = 1
        )
        INSERT @Before SELECT DISTINCT [IdRole], [Id] FROM PermissionTree OPTION (MAXRECURSION 32767);

        -- A direct reports-group grant would otherwise acquire stock access. Expand
        -- the role's old grants, then remove that group grant for affected roles.
        INSERT [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT b.[IdRole], b.[IdPermission] FROM @Before b
        WHERE b.[IdPermission] <> @ReportsId
          AND EXISTS (SELECT 1 FROM [Admin].[RolePermission] rp WHERE rp.[IdRole] = b.[IdRole] AND rp.[IdPermission] = @ReportsId)
          AND NOT EXISTS (SELECT 1 FROM @Before stock WHERE stock.[IdRole] = b.[IdRole] AND stock.[IdPermission] = @StockId)
          AND NOT EXISTS (SELECT 1 FROM [Admin].[RolePermission] rp WHERE rp.[IdRole] = b.[IdRole] AND rp.[IdPermission] = b.[IdPermission]);
        DELETE rp FROM [Admin].[RolePermission] rp
        WHERE rp.[IdPermission] = @ReportsId
          AND NOT EXISTS (SELECT 1 FROM @Before b WHERE b.[IdRole] = rp.[IdRole] AND b.[IdPermission] = @StockId);

        -- Roles that inherited stock through the old group retain it explicitly.
        INSERT [Admin].[RolePermission] ([IdRole], [IdPermission])
        SELECT b.[IdRole], @StockId FROM @Before b
        WHERE b.[IdPermission] = @StockId
          AND NOT EXISTS (SELECT 1 FROM [Admin].[RolePermission] rp WHERE rp.[IdRole] = b.[IdRole] AND rp.[IdPermission] = @StockId);

        UPDATE [Admin].[Permission] SET [ParentPermissionKey] = N'warehouse.reports.group' WHERE [Id] = @StockId;
        UPDATE [Admin].[Permission] SET [Name] = N'تعریف شرکت و انبار' WHERE [PermissionKey] = N'warehouse.structure.group';

        DECLARE @After TABLE ([IdRole] BIGINT NOT NULL, [IdPermission] BIGINT NOT NULL, PRIMARY KEY ([IdRole], [IdPermission]));
        ;WITH PermissionTree AS
        (
            SELECT rp.[IdRole], p.[Id], p.[PermissionKey]
            FROM [Admin].[RolePermission] rp JOIN [Admin].[Permission] p ON p.[Id] = rp.[IdPermission]
            WHERE p.[IsActive] = 1
            UNION ALL
            SELECT tree.[IdRole], child.[Id], child.[PermissionKey]
            FROM PermissionTree tree JOIN [Admin].[Permission] child ON child.[ParentPermissionKey] = tree.[PermissionKey]
            WHERE child.[IsActive] = 1
        )
        INSERT @After SELECT DISTINCT [IdRole], [Id] FROM PermissionTree OPTION (MAXRECURSION 32767);
        IF EXISTS
        (
            SELECT b.[IdRole], b.[IdPermission] FROM @Before b JOIN [Admin].[Permission] p ON p.[Id] = b.[IdPermission]
            WHERE p.[Area] <> N'' AND p.[Controller] <> N'' AND p.[Action] <> N''
            EXCEPT
            SELECT a.[IdRole], a.[IdPermission] FROM @After a
        ) OR EXISTS
        (
            SELECT a.[IdRole], a.[IdPermission] FROM @After a JOIN [Admin].[Permission] p ON p.[Id] = a.[IdPermission]
            WHERE p.[Area] <> N'' AND p.[Controller] <> N'' AND p.[Action] <> N''
            EXCEPT
            SELECT b.[IdRole], b.[IdPermission] FROM @Before b
        )
            THROW 51241, 'Warehouse permission migration would change effective route permissions.', 1;
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'124-warehouse-stock-permission-group');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
