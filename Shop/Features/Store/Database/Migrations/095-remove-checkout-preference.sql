SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'095-remove-checkout-preference')
    BEGIN
        IF OBJECT_ID(N'[Account].[CheckoutPreference]', N'U') IS NOT NULL
            DROP TABLE [Account].[CheckoutPreference];

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'095-remove-checkout-preference');
    END
    ELSE IF OBJECT_ID(N'[Account].[CheckoutPreference]', N'U') IS NOT NULL
        THROW 51095, 'Checkout preference migration is recorded but the table exists.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
