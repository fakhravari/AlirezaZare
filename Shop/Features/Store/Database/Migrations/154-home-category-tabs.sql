SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51540, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'154-home-category-tabs')
    BEGIN
        IF OBJECT_ID(N'Store.HomeCategoryTab', N'U') IS NOT NULL
            THROW 51541, 'Store.HomeCategoryTab already exists; inspect before migration.', 1;
        CREATE TABLE Store.HomeCategoryTab (
            Id bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_HomeCategoryTab PRIMARY KEY,
            IdCategory bigint NOT NULL,
            SortOrder int NOT NULL,
            IsActive bit NOT NULL CONSTRAINT DF_HomeCategoryTab_IsActive DEFAULT (1),
            CONSTRAINT FK_HomeCategoryTab_Category FOREIGN KEY (IdCategory) REFERENCES Store.Category(Id),
            CONSTRAINT UQ_HomeCategoryTab_Category UNIQUE (IdCategory)
        );
        INSERT Store.HomeCategoryTab (IdCategory, SortOrder, IsActive)
        SELECT category.Id, source.SortOrder, 1
        FROM (VALUES (N'ابزار برقی', 10), (N'ابزار اندازه‌گیری', 20), (N'لحیم‌کاری و تعمیر برد', 30), (N'روشنایی و لامپ', 40)) AS source(NameFa, SortOrder)
        CROSS APPLY (SELECT TOP (1) Id FROM Store.Category WHERE NameFa = source.NameFa AND IsActive = 1 ORDER BY Id) AS category;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'154-home-category-tabs');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
