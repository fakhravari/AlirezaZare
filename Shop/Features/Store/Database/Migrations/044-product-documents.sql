SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'044-product-documents')
    BEGIN
        CREATE TABLE [Store].[ProductDocument]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_ProductDocument] PRIMARY KEY,
            [IdProduct] BIGINT NOT NULL,
            [Title] NVARCHAR(200) NOT NULL,
            [Path] NVARCHAR(450) NOT NULL,
            [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_ProductDocument_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [FK_Store_ProductDocument_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]),
            CONSTRAINT [CK_Store_ProductDocument_Title] CHECK (LEN(LTRIM(RTRIM([Title]))) > 0),
            CONSTRAINT [UQ_Store_ProductDocument_Path] UNIQUE ([Path])
        );
        CREATE INDEX [IX_Store_ProductDocument_Product] ON [Store].[ProductDocument] ([IdProduct]);
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'044-product-documents');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
