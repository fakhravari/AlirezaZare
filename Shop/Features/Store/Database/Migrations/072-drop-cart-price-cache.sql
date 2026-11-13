SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'072-drop-cart-price-cache')
    BEGIN
        IF COL_LENGTH(N'Account.CartItem', N'UnitAmount') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'Account.CartItem') AND [name] = N'CK_Account_CartItem_UnitAmount')
                EXEC sys.sp_executesql N'ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [CK_Account_CartItem_UnitAmount];';

            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE [parent_object_id] = OBJECT_ID(N'Account.CartItem') AND [name] = N'DF_Account_CartItem_UnitAmount')
                EXEC sys.sp_executesql N'ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [DF_Account_CartItem_UnitAmount];';

            ALTER TABLE [Account].[CartItem] DROP COLUMN [UnitAmount];
        END;

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'072-drop-cart-price-cache');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
