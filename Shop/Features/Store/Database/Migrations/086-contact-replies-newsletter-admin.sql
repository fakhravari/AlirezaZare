SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'086-contact-replies-newsletter-admin')
 BEGIN
  IF COL_LENGTH(N'Store.NewsletterSubscriber', N'UnsubscribedAtUtc') IS NULL ALTER TABLE [Store].[NewsletterSubscriber] ADD [UnsubscribedAtUtc] DATETIME2(3) NULL;
  IF COL_LENGTH(N'Store.ContactFormMessage', N'Email') IS NULL ALTER TABLE [Store].[ContactFormMessage] ADD [Email] NVARCHAR(254) NULL;
  IF COL_LENGTH(N'Store.ContactFormMessage', N'AdminReply') IS NULL ALTER TABLE [Store].[ContactFormMessage] ADD [AdminReply] NVARCHAR(4000) NULL;
  IF COL_LENGTH(N'Store.ContactFormMessage', N'RepliedAtUtc') IS NULL ALTER TABLE [Store].[ContactFormMessage] ADD [RepliedAtUtc] DATETIME2(3) NULL;
  IF COL_LENGTH(N'Store.ContactFormMessage', N'ReplyEmailSentAtUtc') IS NULL ALTER TABLE [Store].[ContactFormMessage] ADD [ReplyEmailSentAtUtc] DATETIME2(3) NULL;
  IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey]=N'store.newsletter-subscribers.page')
   INSERT [Admin].[Permission] ([PermissionKey],[Name],[Area],[Controller],[Action],[FeatureKey],[SortOrder],[IsActive]) VALUES (N'store.newsletter-subscribers.page',N'اعضای خبرنامه',N'Store',N'Features',N'Index',N'newsletter-subscribers',42,1);
  IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey]=N'store.newsletter-subscribers.list')
   INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[SortOrder],[IsActive]) VALUES (N'store.newsletter-subscribers.list',N'فهرست اعضای خبرنامه',N'store.newsletter-subscribers.page',N'Store',N'StoreNewsletterSubscribersAdminApi',N'List',43,1);
  IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey]=N'store.newsletter-subscribers.update')
   INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[SortOrder],[IsActive]) VALUES (N'store.newsletter-subscribers.update',N'تغییر عضویت خبرنامه',N'store.newsletter-subscribers.page',N'Store',N'StoreNewsletterSubscribersAdminApi',N'Update',44,1);
  IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey]=N'store.contact-messages.reply')
   INSERT [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[SortOrder],[IsActive]) VALUES (N'store.contact-messages.reply',N'پاسخ به پیام تماس',N'store.contact-messages.page',N'Store',N'StoreContactMessagesAdminApi',N'Reply',45,1);
  INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'086-contact-replies-newsletter-admin');
 END;
 COMMIT;
END TRY
BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
