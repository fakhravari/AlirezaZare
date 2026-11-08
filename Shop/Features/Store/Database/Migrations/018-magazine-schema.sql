SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'018-magazine-schema')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF SCHEMA_ID(N'Mag') IS NULL EXEC(N'CREATE SCHEMA [Mag]');

    IF OBJECT_ID(N'[Store].[MagazineArticle]', N'U') IS NOT NULL
        ALTER SCHEMA [Mag] TRANSFER [Store].[MagazineArticle];
    IF OBJECT_ID(N'[Store].[MagazineArticleSection]', N'U') IS NOT NULL
        ALTER SCHEMA [Mag] TRANSFER [Store].[MagazineArticleSection];
    IF OBJECT_ID(N'[Store].[MagazineArticleSource]', N'U') IS NOT NULL
        ALTER SCHEMA [Mag] TRANSFER [Store].[MagazineArticleSource];

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'018-magazine-schema');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
