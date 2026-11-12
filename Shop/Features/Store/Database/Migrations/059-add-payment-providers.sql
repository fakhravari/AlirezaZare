SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'059-add-payment-providers')
    BEGIN
        INSERT INTO [Store].[Bank] ([Code], [NameFa])
        SELECT source.[Code], source.[NameFa]
        FROM (VALUES
            ('snapp-pay', N'اسنپ‌پی'),
            ('digipay', N'دیجی‌پی'),
            ('azki-vam', N'ازکی‌وام')
        ) AS source([Code], [NameFa])
        WHERE NOT EXISTS (SELECT 1 FROM [Store].[Bank] bank WHERE bank.[Code] = source.[Code]);

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'059-add-payment-providers');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
