SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'057-payment-institutions')
    BEGIN
        IF COL_LENGTH(N'Store.Bank', N'InstitutionType') IS NOT NULL
        BEGIN
            ALTER TABLE [Store].[Bank] DROP CONSTRAINT [CK_Store_Bank_Type];
            ALTER TABLE [Store].[Bank] DROP COLUMN [InstitutionType];
        END;

        INSERT INTO [Store].[Bank] ([Code], [NameFa])
        SELECT source.[Code], source.[NameFa]
        FROM (VALUES
            ('zarinpal', N'زرین‌پال'), ('idpay', N'آیدی‌پی'), ('jibit', N'جیبیت'), ('jibimo', N'جیبی‌مو'),
            ('payping', N'پی‌پینگ'), ('zibal', N'زیبال'), ('vandar', N'وندار'), ('iran-dargah', N'ایران‌درگاه'),
            ('paystar', N'پی‌استار'), ('nextpay', N'نکست‌پی'), ('sepal', N'سپال'), ('parspal', N'پارس‌پال'),
            ('panapal', N'پاناپال'), ('novinopay', N'نوینوپی'), ('hoorpay', N'هورپی'), ('tabapay', N'تاباپی'),
            ('gooyapay', N'گویاپی'), ('poolam', N'پولام'), ('sizpay', N'سیزپی'), ('seporde', N'سپرده'),
            ('packpay', N'پک‌پی'), ('rayanpay', N'رایان‌پی'), ('shaparak', N'شپا'), ('aghaye-pardakht', N'آقای پرداخت')
        ) AS source([Code], [NameFa])
        WHERE NOT EXISTS (SELECT 1 FROM [Store].[Bank] bank WHERE bank.[Code] = source.[Code]);

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'057-payment-institutions');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
