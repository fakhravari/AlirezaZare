SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'007-unit-catalog')
    BEGIN
        INSERT INTO [Warehouse].[Unit] ([Code], [Name], [IsActive])
        SELECT v.[Code], v.[Name], 1 FROM (VALUES
        (N'CAT001', N'نفر'),
        (N'CAT002', N'جفت'),
        (N'CAT003', N'نخ'),
        (N'CAT004', N'قالب'),
        (N'CAT005', N'دانه'),
        (N'CAT006', N'عدد'),
        (N'CAT007', N'فقره'),
        (N'CAT008', N'باب'),
        (N'CAT009', N'دست'),
        (N'CAT010', N'جین'),
        (N'CAT011', N'اصله'),
        (N'CAT012', N'توپ'),
        (N'CAT013', N'جلد'),
        (N'CAT014', N'شاخه'),
        (N'CAT015', N'دستگاه'),
        (N'CAT016', N'عدل'),
        (N'CAT017', N'کلاف'),
        (N'CAT018', N'ورق'),
        (N'CAT019', N'رول'),
        (N'CAT020', N'بسته'),
        (N'CAT021', N'کارتن'),
        (N'CAT022', N'ست'),
        (N'CAT023', N'کیسه'),
        (N'CAT024', N'جعبه'),
        (N'CAT025', N'حلقه'),
        (N'CAT026', N'گرم'),
        (N'CAT027', N'کیلوگرم'),
        (N'CAT028', N'تن'),
        (N'CAT029', N'متر'),
        (N'CAT030', N'سانتی متر'),
        (N'CAT031', N'کیلومتر'),
        (N'CAT032', N'اینچ'),
        (N'CAT033', N'میلی متر'),
        (N'CAT034', N'میکرو متر'),
        (N'CAT035', N'متر مربع'),
        (N'CAT036', N'وات'),
        (N'CAT037', N'میلی وات'),
        (N'CAT038', N'کیلو وات'),
        (N'CAT039', N'مگا وات'),
        (N'CAT040', N'گیگا وات'),
        (N'CAT041', N'ترا وات'),
        (N'CAT042', N'اسب بخار'),
        (N'CAT043', N'تن (Refrigeration)'),
        (N'CAT044', N'ثانیه'),
        (N'CAT045', N'دقیقه'),
        (N'CAT046', N'ساعت'),
        (N'CAT047', N'روز'),
        (N'CAT048', N'هفته'),
        (N'CAT049', N'دو هفته'),
        (N'CAT050', N'ماه'),
        (N'CAT051', N'سال'),
        (N'CAT052', N'گرم بر سانتی متر مکعب'),
        (N'CAT053', N'سانتی متر مکعب'),
        (N'CAT054', N'لیتر'),
        (N'CAT055', N'سلسیوس'),
        (N'CAT056', N'فارنهایت'),
        (N'CAT057', N'بار'),
        (N'CAT058', N'باکس'),
        (N'CAT059', N'سانتی متر مربع'),
        (N'CAT060', N'حلب'),
        (N'CAT061', N'شیرینگ'),
        (N'CAT062', N'شل'),
        (N'CAT063', N'گرم بر مترمربع'),
        (N'CAT064', N'سانتی گراد'),
        (N'CAT065', N'CPS'),
        (N'CAT066', N'پالت'),
        (N'CAT067', N'گرم بر گرم'),
        (N'CAT068', N'قوطی'),
        (N'CAT069', N'درصد'),
        (N'CAT070', N'تست ها'),
        (N'CAT071', N'تخته'),
        (N'CAT072', N'برگ'),
        (N'CAT073', N'میلی لیتر')
        ) AS v([Code], [Name])
        WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[Unit] u WHERE u.[Name] = v.[Name] OR u.[Code] = v.[Code]);
        INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'007-unit-catalog');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
