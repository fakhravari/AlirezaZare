SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'033-related-article-reference')
BEGIN
    COMMIT TRANSACTION;
    RETURN;
END;

ALTER TABLE [Mag].[MagazineArticleRelated] DROP COLUMN [Category], [Title], [ImagePath];

INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'033-related-article-reference');
COMMIT TRANSACTION;
