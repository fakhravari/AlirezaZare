SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.category-content.read')
    INSERT [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [SortOrder])
    VALUES (N'store.category-content.read', N'مشاهده محتوای دسته', N'store.catalog.categories', N'Store', N'StoreCategoryContentApi', N'Get', 10);
IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey] = N'store.category-content.update')
    INSERT [Admin].[Permission] ([PermissionKey], [Name], [ParentPermissionKey], [Area], [Controller], [Action], [SortOrder])
    VALUES (N'store.category-content.update', N'ویرایش محتوای دسته', N'store.catalog.categories', N'Store', N'StoreCategoryContentApi', N'Update', 20);

INSERT [Admin].[RolePermission] ([IdRole], [IdPermission])
SELECT r.[Id], p.[Id] FROM [Admin].[Role] r CROSS JOIN [Admin].[Permission] p
WHERE r.[RoleKey] = N'system.full-access' AND p.[PermissionKey] IN (N'store.category-content.read', N'store.category-content.update')
  AND NOT EXISTS (SELECT 1 FROM [Admin].[RolePermission] rp WHERE rp.[IdRole] = r.[Id] AND rp.[IdPermission] = p.[Id]);
COMMIT TRANSACTION;
