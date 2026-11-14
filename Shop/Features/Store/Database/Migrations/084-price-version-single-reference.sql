SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[PriceVersion]', N'U') IS NULL
        THROW 51084, 'Store.PriceVersion must exist before migration 084.', 1;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'084-price-version-single-reference')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Store_PriceVersion_SingleTypedReference' AND parent_object_id=OBJECT_ID(N'[Store].[PriceVersion]'))
            ALTER TABLE [Store].[PriceVersion] WITH CHECK ADD CONSTRAINT [CK_Store_PriceVersion_SingleTypedReference] CHECK (
                (CASE WHEN [IdProductVariant] IS NOT NULL THEN 1 ELSE 0 END +
                 CASE WHEN [IdOptionValue] IS NOT NULL THEN 1 ELSE 0 END +
                 CASE WHEN [IdAccessory] IS NOT NULL THEN 1 ELSE 0 END +
                 CASE WHEN [IdOrderItem] IS NOT NULL THEN 1 ELSE 0 END) <= 1
            );
        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'084-price-version-single-reference');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
