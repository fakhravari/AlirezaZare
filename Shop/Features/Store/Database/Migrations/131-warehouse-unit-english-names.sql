SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
    THROW 51310, 'Store.SchemaMigration is required before migration 131.', 1;

IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'131-warehouse-unit-english-names')
BEGIN
    UPDATE [Warehouse].[Unit]
    SET [NameEn] = CASE [Id]
        WHEN 1 THEN N'Count' WHEN 2 THEN N'Person' WHEN 3 THEN N'Pair' WHEN 4 THEN N'Thread'
        WHEN 5 THEN N'Mold' WHEN 6 THEN N'Piece' WHEN 7 THEN N'Item' WHEN 8 THEN N'Article'
        WHEN 9 THEN N'Hand' WHEN 10 THEN N'Dozen' WHEN 11 THEN N'Strand' WHEN 12 THEN N'Ball'
        WHEN 13 THEN N'Volume' WHEN 14 THEN N'Branch' WHEN 15 THEN N'Device' WHEN 16 THEN N'Bale'
        WHEN 17 THEN N'Skein' WHEN 18 THEN N'Sheet' WHEN 19 THEN N'Roll' WHEN 20 THEN N'Pack'
        WHEN 21 THEN N'Carton' WHEN 22 THEN N'Set' WHEN 23 THEN N'Bag' WHEN 24 THEN N'Box'
        WHEN 25 THEN N'Ring' WHEN 26 THEN N'Gram' WHEN 27 THEN N'Kilogram' WHEN 28 THEN N'Tonne'
        WHEN 29 THEN N'Meter' WHEN 30 THEN N'Centimeter' WHEN 31 THEN N'Kilometer' WHEN 32 THEN N'Inch'
        WHEN 33 THEN N'Millimeter' WHEN 34 THEN N'Micrometer' WHEN 35 THEN N'Square Meter'
        WHEN 36 THEN N'Watt' WHEN 37 THEN N'Milliwatt' WHEN 38 THEN N'Kilowatt' WHEN 39 THEN N'Megawatt'
        WHEN 40 THEN N'Gigawatt' WHEN 41 THEN N'Terawatt' WHEN 42 THEN N'Horsepower'
        WHEN 43 THEN N'Refrigeration Ton' WHEN 44 THEN N'Second' WHEN 45 THEN N'Minute' WHEN 46 THEN N'Hour'
        WHEN 47 THEN N'Day' WHEN 48 THEN N'Week' WHEN 49 THEN N'Two Weeks' WHEN 50 THEN N'Month'
        WHEN 51 THEN N'Year' WHEN 52 THEN N'Gram per Cubic Centimeter' WHEN 53 THEN N'Cubic Centimeter'
        WHEN 54 THEN N'Liter' WHEN 55 THEN N'Celsius' WHEN 56 THEN N'Fahrenheit' WHEN 57 THEN N'Bar'
        WHEN 58 THEN N'Box' WHEN 59 THEN N'Square Centimeter' WHEN 60 THEN N'Tin'
        WHEN 61 THEN N'Shrink Wrap' WHEN 62 THEN N'Bundle' WHEN 63 THEN N'Gram per Square Meter'
        WHEN 64 THEN N'Degree Celsius' WHEN 65 THEN N'Centipoise (CPS)' WHEN 66 THEN N'Pallet'
        WHEN 67 THEN N'Gram per Gram' WHEN 68 THEN N'Can' WHEN 69 THEN N'Percent' WHEN 70 THEN N'Test'
        WHEN 71 THEN N'Board' WHEN 72 THEN N'Leaf' WHEN 73 THEN N'Milliliter'
        ELSE [NameEn]
    END
    WHERE [Id] BETWEEN 1 AND 73 AND ([NameEn] IS NULL OR LTRIM(RTRIM([NameEn])) = N'');

    INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'131-warehouse-unit-english-names');
END;

COMMIT TRANSACTION;
