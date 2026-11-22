SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
        THROW 51450, 'Store.SchemaMigration is required.', 1;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'145-rename-variant-definition-permission')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'warehouse.variants' AND [IsActive] = 1)
            THROW 51451, 'Active variant definition permission is missing.', 1;

        UPDATE [Admin].[Permission]
        SET [Name] = N'اتصال تنوع به محصول'
        WHERE [PermissionKey] = N'warehouse.variants' AND [IsActive] = 1;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'145-rename-variant-definition-permission');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
