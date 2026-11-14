SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'088-fix-admin-permission-unicode')
 BEGIN
  UPDATE [Admin].[Permission] SET [Name]=N'پاسخ به پیام تماس' WHERE [PermissionKey]=N'store.contact-messages.reply';
  UPDATE [Admin].[Permission] SET [Name]=N'اعضای خبرنامه' WHERE [PermissionKey]=N'store.newsletter-subscribers.page';
  UPDATE [Admin].[Permission] SET [Name]=N'فهرست اعضای خبرنامه' WHERE [PermissionKey]=N'store.newsletter-subscribers.list';
  UPDATE [Admin].[Permission] SET [Name]=N'تغییر عضویت خبرنامه' WHERE [PermissionKey]=N'store.newsletter-subscribers.update';
  INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'088-fix-admin-permission-unicode');
 END;
 COMMIT;
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
