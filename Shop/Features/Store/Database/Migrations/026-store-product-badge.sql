SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'026-store-product-badge')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[Product]', N'U') IS NULL
        THROW 51062, 'Store.Product must exist before product badges can be added.', 1;

    IF COL_LENGTH(N'Store.Product', N'Badge') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE [Store].[Product] ADD [Badge] NVARCHAR(100) NULL;';

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'026-store-product-badge');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
