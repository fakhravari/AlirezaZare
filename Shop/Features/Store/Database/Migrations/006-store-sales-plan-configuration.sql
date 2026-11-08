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
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'006-store-sales-plan-configuration')
    BEGIN COMMIT TRANSACTION; RETURN; END;

    ALTER TABLE [Store].[ProductSalesPlanOfferAccessory]
        ADD [IsAllowed] BIT NOT NULL CONSTRAINT [DF_Store_ProductSalesPlanOfferAccessory_IsAllowed] DEFAULT (1);
    INSERT INTO [Store].[ProductSalesPlanOfferAccessory] ([IdProductSalesPlanOffer], [IdAccessory], [PriceAmount])
    SELECT o.[Id], pa.[IdAccessory], pa.[PriceAmount]
    FROM [Store].[ProductSalesPlanOffer] o
    INNER JOIN [Store].[ProductAccessory] pa ON pa.[IdProduct] = o.[IdProduct]
    WHERE NOT EXISTS
        (SELECT 1 FROM [Store].[ProductSalesPlanOfferAccessory] oa
         WHERE oa.[IdProductSalesPlanOffer] = o.[Id] AND oa.[IdAccessory] = pa.[IdAccessory]);

    EXEC sys.sp_executesql N'UPDATE oa SET [IsAllowed] = CASE
        WHEN p.[StoreProductKey] = N''1'' AND sp.[SalesPlanKey] = N''digi-time'' AND a.[AccessoryKey] IN (N''shipping-insurance'', N''sds-bit-set'') THEN 1
        WHEN p.[StoreProductKey] = N''1'' AND sp.[SalesPlanKey] = N''digi-af'' AND a.[AccessoryKey] IN (N''shipping-insurance'', N''drill-case'') THEN 1
        WHEN p.[StoreProductKey] = N''6'' AND sp.[SalesPlanKey] = N''digi-time'' AND a.[AccessoryKey] IN (N''meter-case'', N''probe-set'') THEN 1
        WHEN p.[StoreProductKey] = N''6'' AND sp.[SalesPlanKey] = N''digi-af'' AND a.[AccessoryKey] IN (N''meter-case'', N''meter-test-lead'') THEN 1
        WHEN p.[StoreProductKey] = N''11'' AND sp.[SalesPlanKey] = N''digi-time'' AND a.[AccessoryKey] IN (N''solder-tips'', N''solder-wire'') THEN 1
        WHEN p.[StoreProductKey] = N''11'' AND sp.[SalesPlanKey] = N''digi-af'' AND a.[AccessoryKey] IN (N''solder-stand-kit'', N''solder-wire'') THEN 1
        WHEN p.[StoreProductKey] IN (N''1'',N''6'',N''11'') THEN 0 ELSE 1 END
    FROM [Store].[ProductSalesPlanOfferAccessory] oa
    INNER JOIN [Store].[ProductSalesPlanOffer] o ON o.[Id] = oa.[IdProductSalesPlanOffer]
    INNER JOIN [Store].[Product] p ON p.[Id] = o.[IdProduct]
    INNER JOIN [Store].[SalesPlan] sp ON sp.[Id] = o.[IdSalesPlan]
    INNER JOIN [Store].[Accessory] a ON a.[Id] = oa.[IdAccessory];';

    CREATE TABLE [Store].[ProductSalesPlanOfferSpecificationOverride]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProductSalesPlanOffer] BIGINT NOT NULL,
        [SpecificationKey] NVARCHAR(100) NOT NULL,
        [Value] NVARCHAR(1000) NOT NULL,
        CONSTRAINT [PK_Store_ProductSalesPlanOfferSpecificationOverride] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductSalesPlanOfferSpecificationOverride_Offer] FOREIGN KEY ([IdProductSalesPlanOffer]) REFERENCES [Store].[ProductSalesPlanOffer] ([Id]),
        CONSTRAINT [UQ_Store_ProductSalesPlanOfferSpecificationOverride_Offer_Key] UNIQUE ([IdProductSalesPlanOffer], [SpecificationKey])
    );

    INSERT INTO [Store].[ProductSalesPlanOfferSpecificationOverride] ([IdProductSalesPlanOffer], [SpecificationKey], [Value])
    SELECT o.[Id], v.[SpecificationKey], v.[Value]
    FROM (VALUES
        (N'1',N'digi-time',N'نوع سه‌نظام',N'اتوماتیک ۱۰ میلی‌متر'),
        (N'1',N'digi-time',N'نوع سوراخ‌کاری',N'چکشی دوحالته'),
        (N'6',N'digi-time',N'نوع نمایش',N'LCD با نور پس‌زمینه'),
        (N'6',N'digi-time',N'کار اندازه‌گیری',N'ولتاژ AC/DC، مقاومت و پیوستگی'),
        (N'11',N'digi-time',N'کنترل فرایند',N'کنترل دیجیتال دما با بازیابی سریع حرارت'),
        (N'11',N'digi-time',N'لوازم پیشنهادی',N'نوک یدکی، سیم لحیم و اسفنج تمیزکننده'),
        (N'1',N'digi-af',N'نوع سه‌نظام',N'کلیدی ۱۳ میلی‌متر'),
        (N'1',N'digi-af',N'نوع سوراخ‌کاری',N'چکشی و سوراخ‌کاری'),
        (N'6',N'digi-af',N'نوع نمایش',N'LCD بزرگ با نور پس‌زمینه'),
        (N'6',N'digi-af',N'کار اندازه‌گیری',N'اندازه‌گیری AC/DC، دما و تست دیود'),
        (N'6',N'digi-af',N'نکتهٔ ایمنی',N'ردهٔ CAT III برای کارهای تأسیساتی'),
        (N'11',N'digi-af',N'کنترل فرایند',N'کنترل دمای دقیق برای مونتاژ ظریف'),
        (N'11',N'digi-af',N'لوازم پیشنهادی',N'پایه هویه، اسفنج تمیزکننده و سیم لحیم')) v([ProductKey], [SalesPlanKey], [SpecificationKey], [Value])
    INNER JOIN [Store].[Product] p ON p.[StoreProductKey] = v.[ProductKey]
    INNER JOIN [Store].[SalesPlan] sp ON sp.[SalesPlanKey] = v.[SalesPlanKey]
    INNER JOIN [Store].[ProductSalesPlanOffer] o ON o.[IdProduct] = p.[Id] AND o.[IdSalesPlan] = sp.[Id];

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'006-store-sales-plan-configuration');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
