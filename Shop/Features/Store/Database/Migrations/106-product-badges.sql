SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'106-product-badges')
    BEGIN
        IF COL_LENGTH(N'Warehouse.Product', N'Badge') IS NULL
            THROW 51160, 'Legacy product badge column is missing.', 1;
        CREATE TABLE Store.ProductBadge (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductBadge PRIMARY KEY,
            Title nvarchar(100) NOT NULL CONSTRAINT UQ_ProductBadge_Title UNIQUE,
            CONSTRAINT CK_ProductBadge_Title_NotBlank CHECK (LEN(LTRIM(RTRIM(Title))) > 0)
        );
        ALTER TABLE Warehouse.Product ADD IdProductBadge bigint NULL;
        INSERT Store.ProductBadge (Title)
            SELECT DISTINCT LTRIM(RTRIM(Badge)) FROM Warehouse.Product WHERE NULLIF(LTRIM(RTRIM(Badge)), N'') IS NOT NULL;
        EXEC sys.sp_executesql N'UPDATE p SET IdProductBadge = b.Id FROM Warehouse.Product p JOIN Store.ProductBadge b ON b.Title = LTRIM(RTRIM(p.Badge));';
        DECLARE @unmatched int;
        EXEC sys.sp_executesql N'SELECT @count = COUNT(*) FROM Warehouse.Product WHERE NULLIF(LTRIM(RTRIM(Badge)), N'''') IS NOT NULL AND IdProductBadge IS NULL;', N'@count int OUTPUT', @count=@unmatched OUTPUT;
        IF @unmatched > 0
            THROW 51161, 'Some product badges could not be transferred.', 1;
        ALTER TABLE Warehouse.Product ADD CONSTRAINT FK_Product_ProductBadge FOREIGN KEY (IdProductBadge) REFERENCES Store.ProductBadge(Id);
        ALTER TABLE Warehouse.Product DROP COLUMN Badge;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'106-product-badges');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
