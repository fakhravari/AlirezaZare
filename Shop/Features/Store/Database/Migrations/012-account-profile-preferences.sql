SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS(SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'012-account-profile-preferences') BEGIN COMMIT; RETURN; END;
 ALTER TABLE [Account].[Profile] ADD [Gender] NVARCHAR(10) NULL,[NewsletterEnabled] BIT NOT NULL CONSTRAINT [DF_Account_Profile_NewsletterEnabled] DEFAULT(0);
 EXEC(N'ALTER TABLE [Account].[Profile] ADD CONSTRAINT [CK_Account_Profile_Gender] CHECK([Gender] IS NULL OR [Gender] IN(N''male'',N''female''));');
 INSERT INTO [Store].[SchemaMigration]([MigrationKey]) VALUES(N'012-account-profile-preferences');
 COMMIT;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
