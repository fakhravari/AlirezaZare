SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'087-assign-store-feature-permissions')
 BEGIN
  DECLARE @RoleId BIGINT = (SELECT [Id] FROM [Admin].[Role] WHERE [RoleKey]=N'system.full-access-store');
  IF @RoleId IS NULL THROW 51001, 'system.full-access-store role is missing.', 1;
  INSERT [Admin].[RolePermission] ([IdRole],[IdPermission])
  SELECT @RoleId,p.[Id] FROM [Admin].[Permission] p
  WHERE p.[PermissionKey] IN (N'store.contact-messages.reply',N'store.newsletter-subscribers.page',N'store.newsletter-subscribers.list',N'store.newsletter-subscribers.update')
    AND NOT EXISTS (SELECT 1 FROM [Admin].[RolePermission] rp WHERE rp.[IdRole]=@RoleId AND rp.[IdPermission]=p.[Id]);
  INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'087-assign-store-feature-permissions');
 END;
 COMMIT;
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
