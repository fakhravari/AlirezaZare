SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'005-store-api-runtime-data')
    BEGIN COMMIT TRANSACTION; RETURN; END;

    IF COL_LENGTH(N'Store.ProductAccessory', N'PriceAmount') IS NULL
        ALTER TABLE [Store].[ProductAccessory] ADD [PriceAmount] DECIMAL(19,0) NULL;

    EXEC sys.sp_executesql N'UPDATE pa SET pa.[PriceAmount] = source.[PriceAmount]
    FROM [Store].[ProductAccessory] pa
    INNER JOIN [Store].[Product] p ON p.[Id] = pa.[IdProduct]
    INNER JOIN [Store].[Accessory] a ON a.[Id] = pa.[IdAccessory]
    INNER JOIN (VALUES
        (N''1'',N''shipping-insurance'',350000),
        (N''1'',N''sds-bit-set'',1250000),
        (N''1'',N''drill-case'',900000),
        (N''2'',N''battery-pack'',1800000),
        (N''2'',N''driver-bit-set'',550000),
        (N''3'',N''sds-bit-set'',1450000),
        (N''3'',N''drill-case'',900000),
        (N''4'',N''battery-charger-kit'',2400000),
        (N''4'',N''driver-bit-set'',550000),
        (N''6'',N''meter-case'',650000),
        (N''6'',N''probe-set'',450000),
        (N''6'',N''meter-test-lead'',720000),
        (N''11'',N''solder-tips'',850000),
        (N''11'',N''solder-wire'',380000),
        (N''11'',N''solder-stand-kit'',520000),
        (N''16'',N''worklight-tripod'',1150000),
        (N''16'',N''worklight-battery'',1350000)
    ) source([StoreProductKey], [AccessoryKey], [PriceAmount])
        ON source.[StoreProductKey] = p.[StoreProductKey] AND source.[AccessoryKey] = a.[AccessoryKey];';

    EXEC sys.sp_executesql N'IF EXISTS (SELECT 1 FROM [Store].[ProductAccessory] WHERE [PriceAmount] IS NULL) THROW 51041, ''Store accessory price fixtures are incomplete.'', 1;';
    EXEC sys.sp_executesql N'ALTER TABLE [Store].[ProductAccessory] ALTER COLUMN [PriceAmount] DECIMAL(19,0) NOT NULL;';

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'005-store-api-runtime-data');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;