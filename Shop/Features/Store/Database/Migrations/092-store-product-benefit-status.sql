SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'092-store-product-benefit-status')
    BEGIN
        IF OBJECT_ID(N'[Store].[ProductBenefit]', N'U') IS NULL
            THROW 51092, 'Store.ProductBenefit must exist before migration 092.', 1;

        IF COL_LENGTH(N'Store.ProductBenefit', N'IsActive') IS NULL
            ALTER TABLE [Store].[ProductBenefit] ADD [IsActive] BIT NOT NULL
                CONSTRAINT [DF_Store_ProductBenefit_IsActive] DEFAULT (1) WITH VALUES;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'092-store-product-benefit-status');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
