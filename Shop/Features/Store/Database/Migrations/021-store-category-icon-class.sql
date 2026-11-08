SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'021-store-category-icon-class')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[Category]', N'U') IS NULL
        THROW 51060, 'Store.Category must exist before category icons can be added.', 1;

    IF COL_LENGTH(N'Store.Category', N'IconClass') IS NULL
        EXEC sys.sp_executesql N'
            ALTER TABLE [Store].[Category]
                ADD [IconClass] NVARCHAR(80) NOT NULL
                    CONSTRAINT [DF_Store_Category_IconClass] DEFAULT (N''bi-grid-3x3-gap'') WITH VALUES;';

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [name] = N'CK_Store_Category_IconClass')
        EXEC sys.sp_executesql N'
            ALTER TABLE [Store].[Category]
                ADD CONSTRAINT [CK_Store_Category_IconClass]
                CHECK ([IconClass] LIKE N''bi-%'' AND CHARINDEX(N'' '', [IconClass]) = 0);';

    EXEC sys.sp_executesql N'
    ;WITH CategoryIcons AS
    (
        SELECT
            category.[CategoryKey],
            category.[ParentCategoryKey],
            CONVERT(NVARCHAR(80), CASE category.[CategoryKey]
                WHEN N''tools-and-equipment'' THEN N''bi-tools''
                WHEN N''lighting-and-lamps'' THEN N''bi-lightbulb''
                WHEN N''power-tools'' THEN N''bi-lightning-charge''
                WHEN N''measurement-tools'' THEN N''bi-speedometer2''
                WHEN N''hand-tools'' THEN N''bi-hammer''
                WHEN N''switches-and-electrical-equipment'' THEN N''bi-toggle-on''
                WHEN N''safety-and-protection'' THEN N''bi-shield-check''
                WHEN N''electronic-components'' THEN N''bi-cpu''
                WHEN N''wires-cables-and-connectors'' THEN N''bi-plug''
                ELSE N''bi-grid-3x3-gap''
            END) AS [IconClass]
        FROM [Store].[Category] AS category
        WHERE category.[ParentCategoryKey] IS NULL

        UNION ALL

        SELECT
            category.[CategoryKey],
            category.[ParentCategoryKey],
            CONVERT(NVARCHAR(80), CASE
                WHEN category.[CategoryKey] LIKE N''%battery%'' OR category.[CategoryKey] LIKE N''%charger%'' THEN N''bi-battery-charging''
                WHEN category.[CategoryKey] LIKE N''%light%'' OR category.[CategoryKey] LIKE N''%lamp%'' OR category.[CategoryKey] LIKE N''%led%'' OR category.[CategoryKey] LIKE N''%floodlight%'' THEN N''bi-lightbulb''
                WHEN category.[CategoryKey] LIKE N''%wire%'' OR category.[CategoryKey] LIKE N''%cable%'' OR category.[CategoryKey] LIKE N''%connector%'' OR category.[CategoryKey] LIKE N''%plug%'' OR category.[CategoryKey] LIKE N''%socket%'' OR category.[CategoryKey] LIKE N''%outlet%'' THEN N''bi-plug''
                WHEN category.[CategoryKey] LIKE N''%safety%'' OR category.[CategoryKey] LIKE N''%protection%'' OR category.[CategoryKey] LIKE N''%ppe%'' OR category.[CategoryKey] LIKE N''%insulated%'' OR category.[CategoryKey] LIKE N''%glove%'' OR category.[CategoryKey] LIKE N''%helmet%'' OR category.[CategoryKey] LIKE N''%mask%'' THEN N''bi-shield-check''
                WHEN category.[CategoryKey] LIKE N''%solder%'' OR category.[CategoryKey] LIKE N''%hot-air%'' OR category.[CategoryKey] LIKE N''%rework%'' THEN N''bi-fire''
                WHEN category.[CategoryKey] LIKE N''%multimeter%'' OR category.[CategoryKey] LIKE N''%meter%'' OR category.[CategoryKey] LIKE N''%ammeter%'' OR category.[CategoryKey] LIKE N''%voltmeter%'' OR category.[CategoryKey] LIKE N''%tester%'' OR category.[CategoryKey] LIKE N''%calibration%'' OR category.[CategoryKey] LIKE N''%measurement%'' THEN N''bi-speedometer2''
                WHEN category.[CategoryKey] LIKE N''%switch%'' OR category.[CategoryKey] LIKE N''%relay%'' OR category.[CategoryKey] LIKE N''%contactor%'' OR category.[CategoryKey] LIKE N''%breaker%'' OR category.[CategoryKey] LIKE N''%control%'' THEN N''bi-toggle-on''
                WHEN category.[CategoryKey] LIKE N''%router%'' OR category.[CategoryKey] LIKE N''%network%'' OR category.[CategoryKey] LIKE N''%communication%'' OR category.[CategoryKey] LIKE N''%wireless%'' THEN N''bi-router''
                WHEN category.[CategoryKey] LIKE N''%board%'' OR category.[CategoryKey] LIKE N''%module%'' OR category.[CategoryKey] LIKE N''%electronic%'' OR category.[CategoryKey] LIKE N''%component%'' OR category.[CategoryKey] LIKE N''%semiconductor%'' OR category.[CategoryKey] LIKE N''%resistor%'' OR category.[CategoryKey] LIKE N''%capacitor%'' OR category.[CategoryKey] LIKE N''%diode%'' OR category.[CategoryKey] LIKE N''%transistor%'' THEN N''bi-cpu''
                WHEN category.[CategoryKey] LIKE N''%screwdriver%'' OR category.[CategoryKey] LIKE N''%wrench%'' OR category.[CategoryKey] LIKE N''%plier%'' OR category.[CategoryKey] LIKE N''%hammer%'' OR category.[CategoryKey] LIKE N''%drill%'' OR category.[CategoryKey] LIKE N''%saw%'' OR category.[CategoryKey] LIKE N''%cutting%'' OR category.[CategoryKey] LIKE N''%grinding%'' OR category.[CategoryKey] LIKE N''%tool%'' THEN N''bi-tools''
                WHEN category.[CategoryKey] LIKE N''%case%'' OR category.[CategoryKey] LIKE N''%box%'' OR category.[CategoryKey] LIKE N''%storage%'' OR category.[CategoryKey] LIKE N''%accessor%'' THEN N''bi-box-seam''
                ELSE parent.[IconClass]
            END) AS [IconClass]
        FROM [Store].[Category] AS category
        INNER JOIN CategoryIcons AS parent ON parent.[CategoryKey] = category.[ParentCategoryKey]
    )
    SELECT [CategoryKey], [IconClass]
    INTO #CategoryIcons
    FROM CategoryIcons
    OPTION (MAXRECURSION 100);

        UPDATE category
        SET category.[IconClass] = icons.[IconClass]
        FROM [Store].[Category] AS category
        INNER JOIN #CategoryIcons AS icons ON icons.[CategoryKey] = category.[CategoryKey];

';

    DECLARE @InvalidIconRows INT;
    EXEC sys.sp_executesql
        N'SELECT @InvalidRows = COUNT(*) FROM [Store].[Category] WHERE [IconClass] IS NULL OR [IconClass] NOT LIKE N''bi-%'';',
        N'@InvalidRows INT OUTPUT',
        @InvalidRows = @InvalidIconRows OUTPUT;

    IF @InvalidIconRows > 0
        THROW 51061, 'Every Store.Category row must have a Bootstrap Icons class.', 1;

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'021-store-category-icon-class');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
