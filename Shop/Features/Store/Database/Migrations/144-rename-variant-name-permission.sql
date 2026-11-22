SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51440, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'144-rename-variant-name-permission')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.variant-names' AND [IsActive] = 1)
            THROW 51441, 'Active variant name permission is missing.', 1;

        UPDATE [Admin].[Permission]
        SET [Name] = N'تنوع محصول'
        WHERE [PermissionKey] = N'warehouse.variant-names' AND [IsActive] = 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'144-rename-variant-name-permission');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
