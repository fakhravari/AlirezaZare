SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'108-product-badge-permission')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.manager')
            THROW 51180, 'Store manager permission is missing.', 1;
        IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.catalog.badges')
            INSERT [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [FeatureKey], [SortOrder], [IsActive])
            VALUES (N'store.catalog.badges', N'مدیریت و تخصیص نشان کالا', N'store.manager', N'Store', N'Catalog', N'Badges', NULL, 25, 1);
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'108-product-badge-permission');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
