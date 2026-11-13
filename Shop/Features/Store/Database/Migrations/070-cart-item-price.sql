SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'070-cart-item-price')
    BEGIN
        IF COL_LENGTH(N'Account.CartItem', N'UnitAmount') IS NULL
            ALTER TABLE [Account].[CartItem] ADD [UnitAmount] DECIMAL(19,0) NOT NULL
                CONSTRAINT [DF_Account_CartItem_UnitAmount] DEFAULT (0) WITH VALUES;

        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'Account.CartItem') AND [name] = N'CK_Account_CartItem_UnitAmount')
            EXEC sys.sp_executesql N'ALTER TABLE [Account].[CartItem] ADD CONSTRAINT [CK_Account_CartItem_UnitAmount] CHECK ([UnitAmount] >= 0);';

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'070-cart-item-price');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
