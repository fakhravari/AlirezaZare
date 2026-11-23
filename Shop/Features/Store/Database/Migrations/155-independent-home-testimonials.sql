SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51550, 'Store.SchemaMigration is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'155-independent-home-testimonials')
    BEGIN
        IF OBJECT_ID(N'Store.HomeTestimonial', N'U') IS NULL OR COL_LENGTH(N'Store.HomeTestimonial', N'IdFeedback') IS NULL
           OR OBJECT_ID(N'Store.HomeTestimonial_Independent', N'U') IS NOT NULL
           OR COL_LENGTH(N'Store.ProductFeedback', N'IsSample') IS NULL
            THROW 51551, 'Unexpected testimonial or feedback schema.', 1;
        IF EXISTS (SELECT 1 FROM Store.HomeTestimonial h LEFT JOIN Store.ProductFeedback f ON f.Id = h.IdFeedback WHERE f.Id IS NULL)
            THROW 51552, 'A testimonial references a missing feedback row.', 1;
        CREATE TABLE Store.HomeTestimonial_Independent (
            Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_HomeTestimonial_Independent PRIMARY KEY,
            IdProduct bigint NOT NULL,
            DisplayName nvarchar(80) NOT NULL,
            FeedbackText nvarchar(800) NOT NULL,
            Rating tinyint NOT NULL,
            CreatedAtUtc datetime2(3) NOT NULL,
            ImagePath nvarchar(500) NULL,
            SortOrder int NOT NULL,
            IsActive bit NOT NULL,
            CONSTRAINT FK_HomeTestimonial_Independent_Product FOREIGN KEY (IdProduct) REFERENCES Warehouse.Product(Id)
        );
        INSERT Store.HomeTestimonial_Independent (IdProduct, DisplayName, FeedbackText, Rating, CreatedAtUtc, ImagePath, SortOrder, IsActive)
        SELECT f.IdProduct, f.DisplayName, f.FeedbackText, COALESCE(f.Rating, 0), f.CreatedAtUtc, h.ImagePath, h.SortOrder,
               CASE WHEN f.FeedbackType = N'reviews' AND f.Status = N'approved' THEN h.IsActive ELSE 0 END
        FROM Store.HomeTestimonial h JOIN Store.ProductFeedback f ON f.Id = h.IdFeedback;
        IF (SELECT COUNT_BIG(*) FROM Store.HomeTestimonial_Independent) <> (SELECT COUNT_BIG(*) FROM Store.HomeTestimonial)
            THROW 51553, 'Testimonial transfer count mismatch.', 1;
        DROP TABLE Store.HomeTestimonial;
        EXEC sys.sp_rename N'Store.HomeTestimonial_Independent', N'HomeTestimonial';
        DECLARE @defaultName sysname;
        SELECT @defaultName = d.name FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
        WHERE d.parent_object_id = OBJECT_ID(N'Store.ProductFeedback') AND c.name = N'IsSample';
        IF @defaultName IS NOT NULL
        BEGIN
            DECLARE @dropDefault nvarchar(max) = N'ALTER TABLE Store.ProductFeedback DROP CONSTRAINT ' + QUOTENAME(@defaultName);
            EXEC sys.sp_executesql @dropDefault;
        END;
        ALTER TABLE Store.ProductFeedback DROP COLUMN IsSample;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'155-independent-home-testimonials');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
