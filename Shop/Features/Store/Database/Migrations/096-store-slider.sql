SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'096-store-slider')
BEGIN
    IF OBJECT_ID(N'[Store].[Slider]', N'U') IS NULL THROW 51096, 'Slider table is missing.', 1;
    COMMIT TRANSACTION;
    RETURN;
END;
IF OBJECT_ID(N'[Store].[Slider]', N'U') IS NOT NULL
    THROW 51096, 'Slider table already exists without migration record.', 1;
BEGIN
    CREATE TABLE [Store].[Slider] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_Slider] PRIMARY KEY,
        [Title] NVARCHAR(200) NOT NULL,
        [Url] NVARCHAR(1000) NOT NULL,
        [ImagePath] NVARCHAR(1000) NOT NULL,
        [ShowOnMobile] BIT NOT NULL,
        [ShowOnDesktop] BIT NOT NULL
    );
    INSERT INTO [Store].[Slider] ([Title], [Url], [ImagePath], [ShowOnMobile], [ShowOnDesktop])
    SELECT v.Title, N'/', N'content/sliders/' + v.FileName + N'.webp', 0, 1 FROM (VALUES (N'ابزار دقیق', N'electrical-tools-banner'), (N'قطعات', N'electrical-components-banner'), (N'روشنایی', N'electrical-lighting-banner'), (N'ابزار حرفه‌ای', N'electrical-tools-pro-banner'), (N'قطعات حرفه‌ای', N'electrical-components-pro-banner'), (N'اندازه‌گیری', N'electrical-measurement-banner')) v(Title, FileName)
    UNION ALL
    SELECT v.Title, N'/', N'content/sliders/mobile/' + v.FileName + N'.webp', 1, 0 FROM (VALUES (N'ابزار دقیق', N'electrical-tools-banner'), (N'قطعات', N'electrical-components-banner'), (N'روشنایی', N'electrical-lighting-banner'), (N'ابزار حرفه‌ای', N'electrical-tools-pro-banner'), (N'قطعات حرفه‌ای', N'electrical-components-pro-banner'), (N'اندازه‌گیری', N'electrical-measurement-banner')) v(Title, FileName);
END;
INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'096-store-slider');
COMMIT TRANSACTION;
