SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'025-normalize-relational-values')
BEGIN
    COMMIT TRANSACTION;
    RETURN;
END;

IF OBJECT_ID(N'[Mag].[ArticleCategory]', N'U') IS NULL
BEGIN
    CREATE TABLE [Mag].[ArticleCategory]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Mag_ArticleCategory] PRIMARY KEY,
        [CategoryKey] NVARCHAR(300) NOT NULL,
        CONSTRAINT [UQ_Mag_ArticleCategory_Key] UNIQUE ([CategoryKey])
    );
END;
IF OBJECT_ID(N'[Mag].[ArticleBranch]', N'U') IS NULL
BEGIN
    CREATE TABLE [Mag].[ArticleBranch]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Mag_ArticleBranch] PRIMARY KEY,
        [BranchKey] NVARCHAR(160) NOT NULL,
        CONSTRAINT [UQ_Mag_ArticleBranch_Key] UNIQUE ([BranchKey])
    );
END;
IF OBJECT_ID(N'[Mag].[ArticleTopic]', N'U') IS NULL
BEGIN
    CREATE TABLE [Mag].[ArticleTopic]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Mag_ArticleTopic] PRIMARY KEY,
        [TopicKey] NVARCHAR(240) NOT NULL,
        CONSTRAINT [UQ_Mag_ArticleTopic_Key] UNIQUE ([TopicKey])
    );
END;
IF OBJECT_ID(N'[Mag].[ArticleArea]', N'U') IS NULL
BEGIN
    CREATE TABLE [Mag].[ArticleArea]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Mag_ArticleArea] PRIMARY KEY,
        [AreaKey] NVARCHAR(240) NOT NULL,
        CONSTRAINT [UQ_Mag_ArticleArea_Key] UNIQUE ([AreaKey])
    );
END;

EXEC(N'INSERT INTO [Mag].[ArticleCategory] ([CategoryKey]) SELECT DISTINCT [Category] FROM [Mag].[MagazineArticle] a WHERE NOT EXISTS (SELECT 1 FROM [Mag].[ArticleCategory] x WHERE x.[CategoryKey] = a.[Category]); INSERT INTO [Mag].[ArticleBranch] ([BranchKey]) SELECT DISTINCT [Branch] FROM [Mag].[MagazineArticle] a WHERE NOT EXISTS (SELECT 1 FROM [Mag].[ArticleBranch] x WHERE x.[BranchKey] = a.[Branch]); INSERT INTO [Mag].[ArticleTopic] ([TopicKey]) SELECT DISTINCT [Topic] FROM [Mag].[MagazineArticle] a WHERE NOT EXISTS (SELECT 1 FROM [Mag].[ArticleTopic] x WHERE x.[TopicKey] = a.[Topic]); INSERT INTO [Mag].[ArticleArea] ([AreaKey]) SELECT DISTINCT [Area] FROM [Mag].[MagazineArticle] a WHERE NOT EXISTS (SELECT 1 FROM [Mag].[ArticleArea] x WHERE x.[AreaKey] = a.[Area]);');

IF COL_LENGTH(N'Mag.MagazineArticle', N'IdCategory') IS NULL ALTER TABLE [Mag].[MagazineArticle] ADD [IdCategory] BIGINT NULL;
IF COL_LENGTH(N'Mag.MagazineArticle', N'IdBranch') IS NULL ALTER TABLE [Mag].[MagazineArticle] ADD [IdBranch] BIGINT NULL;
IF COL_LENGTH(N'Mag.MagazineArticle', N'IdTopic') IS NULL ALTER TABLE [Mag].[MagazineArticle] ADD [IdTopic] BIGINT NULL;
IF COL_LENGTH(N'Mag.MagazineArticle', N'IdArea') IS NULL ALTER TABLE [Mag].[MagazineArticle] ADD [IdArea] BIGINT NULL;
EXEC(N'UPDATE a SET [IdCategory] = c.[Id] FROM [Mag].[MagazineArticle] a JOIN [Mag].[ArticleCategory] c ON c.[CategoryKey] = a.[Category]; UPDATE a SET [IdBranch] = c.[Id] FROM [Mag].[MagazineArticle] a JOIN [Mag].[ArticleBranch] c ON c.[BranchKey] = a.[Branch]; UPDATE a SET [IdTopic] = c.[Id] FROM [Mag].[MagazineArticle] a JOIN [Mag].[ArticleTopic] c ON c.[TopicKey] = a.[Topic]; UPDATE a SET [IdArea] = c.[Id] FROM [Mag].[MagazineArticle] a JOIN [Mag].[ArticleArea] c ON c.[AreaKey] = a.[Area];');
DECLARE @missingMagazine int;
EXEC sp_executesql N'SELECT @n = COUNT(*) FROM [Mag].[MagazineArticle] WHERE [IdCategory] IS NULL OR [IdBranch] IS NULL OR [IdTopic] IS NULL OR [IdArea] IS NULL', N'@n int OUTPUT', @n = @missingMagazine OUTPUT;
IF @missingMagazine > 0 THROW 51025, 'Magazine classification backfill failed.', 1;
EXEC(N'ALTER TABLE [Mag].[MagazineArticle] ALTER COLUMN [IdCategory] BIGINT NOT NULL; ALTER TABLE [Mag].[MagazineArticle] ALTER COLUMN [IdBranch] BIGINT NOT NULL; ALTER TABLE [Mag].[MagazineArticle] ALTER COLUMN [IdTopic] BIGINT NOT NULL; ALTER TABLE [Mag].[MagazineArticle] ALTER COLUMN [IdArea] BIGINT NOT NULL; ALTER TABLE [Mag].[MagazineArticle] ADD CONSTRAINT [FK_Mag_Article_Category] FOREIGN KEY ([IdCategory]) REFERENCES [Mag].[ArticleCategory]([Id]); ALTER TABLE [Mag].[MagazineArticle] ADD CONSTRAINT [FK_Mag_Article_Branch] FOREIGN KEY ([IdBranch]) REFERENCES [Mag].[ArticleBranch]([Id]); ALTER TABLE [Mag].[MagazineArticle] ADD CONSTRAINT [FK_Mag_Article_Topic] FOREIGN KEY ([IdTopic]) REFERENCES [Mag].[ArticleTopic]([Id]); ALTER TABLE [Mag].[MagazineArticle] ADD CONSTRAINT [FK_Mag_Article_Area] FOREIGN KEY ([IdArea]) REFERENCES [Mag].[ArticleArea]([Id]);');

IF COL_LENGTH(N'Mag.MagazineArticleRelated', N'IdRelatedArticle') IS NULL ALTER TABLE [Mag].[MagazineArticleRelated] ADD [IdRelatedArticle] BIGINT NULL;
EXEC(N'UPDATE r SET [IdRelatedArticle] = a.[Id] FROM [Mag].[MagazineArticleRelated] r JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = r.[ArticleSlug];');
DECLARE @missingRelated int;
EXEC sp_executesql N'SELECT @n = COUNT(*) FROM [Mag].[MagazineArticleRelated] WHERE [IdRelatedArticle] IS NULL', N'@n int OUTPUT', @n = @missingRelated OUTPUT;
IF @missingRelated > 0 THROW 51026, 'Related magazine article backfill failed.', 1;
EXEC(N'ALTER TABLE [Mag].[MagazineArticleRelated] ALTER COLUMN [IdRelatedArticle] BIGINT NOT NULL; ALTER TABLE [Mag].[MagazineArticleRelated] ADD CONSTRAINT [FK_Mag_Related_Article] FOREIGN KEY ([IdRelatedArticle]) REFERENCES [Mag].[MagazineArticle]([Id]);');

IF COL_LENGTH(N'Account.ArticleFeedback', N'IdMagazineArticle') IS NULL ALTER TABLE [Account].[ArticleFeedback] ADD [IdMagazineArticle] BIGINT NULL;
IF COL_LENGTH(N'Account.ArticleInteraction', N'IdMagazineArticle') IS NULL ALTER TABLE [Account].[ArticleInteraction] ADD [IdMagazineArticle] BIGINT NULL;
EXEC(N'UPDATE f SET [IdMagazineArticle] = a.[Id] FROM [Account].[ArticleFeedback] f JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = f.[ArticleSlug]; UPDATE i SET [IdMagazineArticle] = a.[Id] FROM [Account].[ArticleInteraction] i JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = i.[ArticleSlug];');
DECLARE @missingAccountArticles int;
EXEC sp_executesql N'SELECT @n = (SELECT COUNT(*) FROM [Account].[ArticleFeedback] WHERE [IdMagazineArticle] IS NULL) + (SELECT COUNT(*) FROM [Account].[ArticleInteraction] WHERE [IdMagazineArticle] IS NULL)', N'@n int OUTPUT', @n = @missingAccountArticles OUTPUT;
IF @missingAccountArticles > 0 THROW 51027, 'Account article relation backfill failed.', 1;
EXEC(N'ALTER TABLE [Account].[ArticleFeedback] ALTER COLUMN [IdMagazineArticle] BIGINT NOT NULL; ALTER TABLE [Account].[ArticleInteraction] ALTER COLUMN [IdMagazineArticle] BIGINT NOT NULL; ALTER TABLE [Account].[ArticleFeedback] ADD CONSTRAINT [FK_Account_ArticleFeedback_MagazineArticle] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Mag].[MagazineArticle]([Id]); ALTER TABLE [Account].[ArticleInteraction] ADD CONSTRAINT [FK_Account_ArticleInteraction_MagazineArticle] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Mag].[MagazineArticle]([Id]);');

IF COL_LENGTH(N'Account.CartItem', N'IdSalesPlan') IS NULL ALTER TABLE [Account].[CartItem] ADD [IdSalesPlan] BIGINT NULL;
IF COL_LENGTH(N'Account.CustomerOrderItem', N'IdSalesPlan') IS NULL ALTER TABLE [Account].[CustomerOrderItem] ADD [IdSalesPlan] BIGINT NULL;
EXEC(N'UPDATE i SET [IdSalesPlan] = p.[Id] FROM [Account].[CartItem] i JOIN [Store].[SalesPlan] p ON p.[SalesPlanKey] = i.[SalesPlanKey]; UPDATE i SET [IdSalesPlan] = p.[Id] FROM [Account].[CustomerOrderItem] i JOIN [Store].[SalesPlan] p ON p.[SalesPlanKey] = i.[SalesPlanKey];');
DECLARE @missingSalesPlans int;
EXEC sp_executesql N'SELECT @n = (SELECT COUNT(*) FROM [Account].[CartItem] WHERE [IdSalesPlan] IS NULL) + (SELECT COUNT(*) FROM [Account].[CustomerOrderItem] WHERE [IdSalesPlan] IS NULL)', N'@n int OUTPUT', @n = @missingSalesPlans OUTPUT;
IF @missingSalesPlans > 0 THROW 51028, 'Sales plan backfill failed.', 1;
DROP INDEX [UX_Account_CartItem_Configuration_Variant] ON [Account].[CartItem];
EXEC(N'ALTER TABLE [Account].[CartItem] ALTER COLUMN [IdSalesPlan] BIGINT NOT NULL; ALTER TABLE [Account].[CustomerOrderItem] ALTER COLUMN [IdSalesPlan] BIGINT NOT NULL; ALTER TABLE [Account].[CartItem] ADD CONSTRAINT [FK_Account_CartItem_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Store].[SalesPlan]([Id]); ALTER TABLE [Account].[CustomerOrderItem] ADD CONSTRAINT [FK_Account_OrderItem_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Store].[SalesPlan]([Id]); ALTER TABLE [Account].[CartItem] ADD CONSTRAINT [UQ_Account_CartItem_Configuration] UNIQUE ([IdCart], [IdProduct], [IdSalesPlan], [IdProductVariant]);');

IF COL_LENGTH(N'Store.Category', N'IdParentCategory') IS NULL ALTER TABLE [Store].[Category] ADD [IdParentCategory] BIGINT NULL;
EXEC(N'UPDATE c SET [IdParentCategory] = p.[Id] FROM [Store].[Category] c JOIN [Store].[Category] p ON p.[CategoryKey] = c.[ParentCategoryKey];');
ALTER TABLE [Store].[Category] DROP CONSTRAINT [FK_Store_Category_Parent];
EXEC(N'ALTER TABLE [Store].[Category] ADD CONSTRAINT [FK_Store_Category_Parent] FOREIGN KEY ([IdParentCategory]) REFERENCES [Store].[Category]([Id]);');

ALTER TABLE [Mag].[MagazineArticleRelated] DROP COLUMN [ArticleSlug];
ALTER TABLE [Account].[ArticleFeedback] DROP COLUMN [ArticleSlug];
ALTER TABLE [Account].[ArticleInteraction] DROP CONSTRAINT [UQ_Account_ArticleInteraction];
ALTER TABLE [Account].[ArticleInteraction] DROP COLUMN [ArticleSlug];
ALTER TABLE [Account].[ArticleInteraction] ADD CONSTRAINT [UQ_Account_ArticleInteraction] UNIQUE ([IdProfile], [IdMagazineArticle], [InteractionType]);
ALTER TABLE [Account].[CartItem] DROP COLUMN [SalesPlanKey];
ALTER TABLE [Account].[CustomerOrderItem] DROP COLUMN [SalesPlanKey];
ALTER TABLE [Store].[Category] DROP COLUMN [ParentCategoryKey];
ALTER TABLE [Mag].[MagazineArticle] DROP COLUMN [Category], [Branch], [Topic], [Area];

INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'025-normalize-relational-values');
COMMIT TRANSACTION;
