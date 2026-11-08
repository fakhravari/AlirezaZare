SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'022-normalize-magazine-runtime')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    ALTER TABLE [Mag].[MagazineArticle] ADD
        [Branch] NVARCHAR(80) NULL,
        [Topic] NVARCHAR(120) NULL,
        [Area] NVARCHAR(120) NULL,
        [Kicker] NVARCHAR(300) NULL,
        [ContentImagePath] NVARCHAR(1000) NULL,
        [ReadTimeText] NVARCHAR(80) NULL;

    CREATE TABLE [Mag].[MagazineArticleTag]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdMagazineArticle] BIGINT NOT NULL,
        [Label] NVARCHAR(300) NOT NULL,
        [Filter] NVARCHAR(160) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Mag_MagazineArticleTag] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Mag_MagazineArticleTag_Article] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Mag].[MagazineArticle] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [Mag].[MagazineArticleRelated]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdMagazineArticle] BIGINT NOT NULL,
        [ArticleSlug] NVARCHAR(160) NOT NULL,
        [Category] NVARCHAR(150) NOT NULL,
        [Title] NVARCHAR(300) NOT NULL,
        [ImagePath] NVARCHAR(1000) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Mag_MagazineArticleRelated] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Mag_MagazineArticleRelated_Article] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Mag].[MagazineArticle] ([Id]) ON DELETE CASCADE
    );

    CREATE TABLE [Mag].[MagazineArticleMetric]
    (
        [IdMagazineArticle] BIGINT NOT NULL,
        [Views] INT NOT NULL,
        [Likes] INT NOT NULL,
        [AverageRating] DECIMAL(5,2) NOT NULL,
        [Comments] INT NOT NULL,
        CONSTRAINT [PK_Mag_MagazineArticleMetric] PRIMARY KEY ([IdMagazineArticle]),
        CONSTRAINT [FK_Mag_MagazineArticleMetric_Article] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Mag].[MagazineArticle] ([Id]) ON DELETE CASCADE
    );

    IF OBJECT_ID(N'[Store].[RuntimeDataset]', N'U') IS NOT NULL
    BEGIN
        EXEC(N'
            INSERT INTO [Mag].[MagazineArticle]
                ([ArticleKey], [Category], [Title], [Summary], [Author], [PublishedAt], [ReadTimeMinutes], [HeroImagePath], [ContentImagePath], [Lead], [Conclusion], [Branch], [Topic], [Area], [Kicker], [ReadTimeText])
            SELECT article.[slug], article.[category], article.[title], article.[summary], article.[author], NULL, source.[readMinutes], article.[heroImage], article.[contentImage], article.[lead], article.[conclusion], source.[branch], source.[topic], source.[area], article.[kicker], article.[readTime]
            FROM [Store].[RuntimeDataset] dataset
            CROSS APPLY OPENJSON(dataset.[PayloadJson], ''$.articles'') WITH
            (
                [branch] NVARCHAR(80) ''$.branch'',
                [topic] NVARCHAR(120) ''$.topic'',
                [area] NVARCHAR(120) ''$.area'',
                [readMinutes] INT ''$.readMinutes'',
                [article] NVARCHAR(MAX) ''$.article'' AS JSON
            ) source
            CROSS APPLY OPENJSON(source.[article]) WITH
            (
                [slug] NVARCHAR(160) ''$.slug'',
                [category] NVARCHAR(150) ''$.category'',
                [kicker] NVARCHAR(300) ''$.kicker'',
                [title] NVARCHAR(300) ''$.title'',
                [summary] NVARCHAR(1500) ''$.summary'',
                [author] NVARCHAR(200) ''$.author'',
                [readTime] NVARCHAR(80) ''$.readTime'',
                [heroImage] NVARCHAR(1000) ''$.heroImage'',
                [contentImage] NVARCHAR(1000) ''$.contentImage'',
                [lead] NVARCHAR(MAX) ''$.lead'',
                [conclusion] NVARCHAR(MAX) ''$.conclusion''
            ) article
            WHERE dataset.[DatasetKey] = N''magazine''
              AND NOT EXISTS (SELECT 1 FROM [Mag].[MagazineArticle] existing WHERE existing.[ArticleKey] = article.[slug]);');

        INSERT INTO [Mag].[MagazineArticleSection] ([IdMagazineArticle], [Heading], [Body], [SortOrder])
        SELECT a.[Id], section.[title], section.[content], CONVERT(INT, sectionRaw.[key])
        FROM [Store].[RuntimeDataset] dataset
        CROSS APPLY OPENJSON(dataset.[PayloadJson], '$.articles') WITH ([article] NVARCHAR(MAX) '$.article' AS JSON) source
        CROSS APPLY OPENJSON(source.[article]) WITH ([slug] NVARCHAR(160) '$.slug', [sections] NVARCHAR(MAX) '$.sections' AS JSON) article
        CROSS APPLY OPENJSON(article.[sections]) sectionRaw
        CROSS APPLY OPENJSON(sectionRaw.[value]) WITH ([title] NVARCHAR(300) '$.title', [content] NVARCHAR(MAX) '$.content') section
        INNER JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = article.[slug]
        WHERE dataset.[DatasetKey] = N'magazine'
          AND NOT EXISTS (SELECT 1 FROM [Mag].[MagazineArticleSection] existing WHERE existing.[IdMagazineArticle] = a.[Id]);

        INSERT INTO [Mag].[MagazineArticleSource] ([IdMagazineArticle], [Title], [Url], [SortOrder])
        SELECT a.[Id], sourceItem.[title], sourceItem.[url], CONVERT(INT, sourceRaw.[key])
        FROM [Store].[RuntimeDataset] dataset
        CROSS APPLY OPENJSON(dataset.[PayloadJson], '$.articles') WITH ([article] NVARCHAR(MAX) '$.article' AS JSON) source
        CROSS APPLY OPENJSON(source.[article]) WITH ([slug] NVARCHAR(160) '$.slug', [sources] NVARCHAR(MAX) '$.sources' AS JSON) article
        CROSS APPLY OPENJSON(article.[sources]) sourceRaw
        CROSS APPLY OPENJSON(sourceRaw.[value]) WITH ([title] NVARCHAR(300) '$.title', [url] NVARCHAR(2000) '$.url') sourceItem
        INNER JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = article.[slug]
        WHERE dataset.[DatasetKey] = N'magazine'
          AND NOT EXISTS (SELECT 1 FROM [Mag].[MagazineArticleSource] existing WHERE existing.[IdMagazineArticle] = a.[Id]);

        INSERT INTO [Mag].[MagazineArticleTag] ([IdMagazineArticle], [Label], [Filter], [SortOrder])
        SELECT a.[Id], tag.[label], tag.[filter], CONVERT(INT, tagRaw.[key])
        FROM [Store].[RuntimeDataset] dataset
        CROSS APPLY OPENJSON(dataset.[PayloadJson], '$.articles') WITH ([article] NVARCHAR(MAX) '$.article' AS JSON) source
        CROSS APPLY OPENJSON(source.[article]) WITH ([slug] NVARCHAR(160) '$.slug', [tags] NVARCHAR(MAX) '$.tags' AS JSON) article
        CROSS APPLY OPENJSON(article.[tags]) tagRaw
        CROSS APPLY OPENJSON(tagRaw.[value]) WITH ([label] NVARCHAR(300) '$.label', [filter] NVARCHAR(160) '$.filter') tag
        INNER JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = article.[slug]
        WHERE dataset.[DatasetKey] = N'magazine'
          AND NOT EXISTS (SELECT 1 FROM [Mag].[MagazineArticleTag] existing WHERE existing.[IdMagazineArticle] = a.[Id]);

        INSERT INTO [Mag].[MagazineArticleRelated] ([IdMagazineArticle], [ArticleSlug], [Category], [Title], [ImagePath], [SortOrder])
        SELECT a.[Id], related.[slug], related.[category], related.[title], related.[image], CONVERT(INT, relatedRaw.[key])
        FROM [Store].[RuntimeDataset] dataset
        CROSS APPLY OPENJSON(dataset.[PayloadJson], '$.articles') WITH ([article] NVARCHAR(MAX) '$.article' AS JSON) source
        CROSS APPLY OPENJSON(source.[article]) WITH ([slug] NVARCHAR(160) '$.slug', [relatedArticles] NVARCHAR(MAX) '$.relatedArticles' AS JSON) article
        CROSS APPLY OPENJSON(article.[relatedArticles]) relatedRaw
        CROSS APPLY OPENJSON(relatedRaw.[value]) WITH ([slug] NVARCHAR(160) '$.slug', [category] NVARCHAR(150) '$.category', [title] NVARCHAR(300) '$.title', [image] NVARCHAR(1000) '$.image') related
        INNER JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = article.[slug]
        WHERE dataset.[DatasetKey] = N'magazine'
          AND NOT EXISTS (SELECT 1 FROM [Mag].[MagazineArticleRelated] existing WHERE existing.[IdMagazineArticle] = a.[Id]);

        INSERT INTO [Mag].[MagazineArticleMetric] ([IdMagazineArticle], [Views], [Likes], [AverageRating], [Comments])
        SELECT a.[Id], metric.[views], metric.[likes], metric.[averageRating], metric.[comments]
        FROM [Store].[RuntimeDataset] dataset
        CROSS APPLY OPENJSON(dataset.[PayloadJson], '$.articles') WITH ([article] NVARCHAR(MAX) '$.article' AS JSON) source
        CROSS APPLY OPENJSON(source.[article]) WITH ([slug] NVARCHAR(160) '$.slug', [metrics] NVARCHAR(MAX) '$.metrics' AS JSON) article
        CROSS APPLY OPENJSON(article.[metrics]) WITH ([views] INT '$.views', [likes] INT '$.likes', [averageRating] DECIMAL(5,2) '$.averageRating', [comments] INT '$.comments') metric
        INNER JOIN [Mag].[MagazineArticle] a ON a.[ArticleKey] = article.[slug]
        WHERE dataset.[DatasetKey] = N'magazine'
          AND NOT EXISTS (SELECT 1 FROM [Mag].[MagazineArticleMetric] existing WHERE existing.[IdMagazineArticle] = a.[Id]);
    END;

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'022-normalize-magazine-runtime');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
