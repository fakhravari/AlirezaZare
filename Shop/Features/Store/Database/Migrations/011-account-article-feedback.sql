SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS(SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'011-account-article-feedback') BEGIN COMMIT; RETURN; END;
 CREATE TABLE [Account].[ArticleFeedback](
  [Id] BIGINT IDENTITY(1,1) NOT NULL,[IdProfile] BIGINT NOT NULL,[ArticleSlug] NVARCHAR(160) NOT NULL,[DisplayName] NVARCHAR(80) NOT NULL,[FeedbackText] NVARCHAR(1000) NOT NULL,[Rating] TINYINT NULL,[Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Account_ArticleFeedback_Status] DEFAULT(N'pending'),[CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Account_ArticleFeedback_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [PK_Account_ArticleFeedback] PRIMARY KEY([Id]),CONSTRAINT [FK_Account_ArticleFeedback_Profile] FOREIGN KEY([IdProfile]) REFERENCES [Account].[Profile]([Id]) ON DELETE CASCADE,CONSTRAINT [CK_Account_ArticleFeedback_Rating] CHECK([Rating] IS NULL OR [Rating] BETWEEN 1 AND 5),CONSTRAINT [CK_Account_ArticleFeedback_Status] CHECK([Status] IN(N'pending',N'approved',N'rejected')));
 INSERT INTO [Store].[SchemaMigration]([MigrationKey]) VALUES(N'011-account-article-feedback');
 COMMIT;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
