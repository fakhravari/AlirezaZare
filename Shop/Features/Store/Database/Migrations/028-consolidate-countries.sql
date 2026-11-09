SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'028-consolidate-countries')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[Country]', N'U') IS NULL OR OBJECT_ID(N'[Geo].[Country]', N'U') IS NULL
        THROW 51028, 'Both country tables are required before consolidation.', 1;

    IF COL_LENGTH(N'Geo.Country', N'FlagPath') IS NULL
        ALTER TABLE [Geo].[Country] ADD [FlagPath] NVARCHAR(500) NULL;

    EXEC(N'
        IF EXISTS (
            SELECT 1 FROM [Store].[Country] s JOIN [Geo].[Country] g ON g.[CountryCode] = s.[CountryCode]
            WHERE s.[NameFa] <> g.[NameFa] OR s.[NameEn] <> g.[NameEn]
               OR (s.[FlagPath] IS NOT NULL AND g.[FlagPath] IS NOT NULL AND s.[FlagPath] <> g.[FlagPath]))
            THROW 51029, ''Conflicting country values require reconciliation before consolidation.'', 1;

        UPDATE g SET g.[FlagPath] = COALESCE(g.[FlagPath], s.[FlagPath])
        FROM [Geo].[Country] g JOIN [Store].[Country] s ON s.[CountryCode] = g.[CountryCode];

        DECLARE @maxId BIGINT = ISNULL((SELECT MAX([Id]) FROM [Geo].[Country]), 0);
        INSERT INTO [Geo].[Country] ([Id], [CountryCode], [NameFa], [NameEn], [FlagPath])
        SELECT @maxId + ROW_NUMBER() OVER (ORDER BY s.[Id]), s.[CountryCode], s.[NameFa], s.[NameEn], s.[FlagPath]
        FROM [Store].[Country] s
        WHERE NOT EXISTS (SELECT 1 FROM [Geo].[Country] g WHERE g.[CountryCode] = s.[CountryCode]);

        SELECT s.[Id] AS [OldId], g.[Id] AS [NewId] INTO #CountryMapping
        FROM [Store].[Country] s JOIN [Geo].[Country] g ON g.[CountryCode] = s.[CountryCode];
        IF EXISTS (SELECT 1 FROM [Store].[Brand] b LEFT JOIN #CountryMapping m ON m.[OldId] = b.[IdCountry]
                   WHERE b.[IdCountry] IS NOT NULL AND m.[NewId] IS NULL)
            THROW 51030, ''A brand country could not be mapped.'', 1;

        ALTER TABLE [Store].[Brand] DROP CONSTRAINT [FK_Store_Brand_Country];
        UPDATE b SET [IdCountry] = m.[NewId]
        FROM [Store].[Brand] b JOIN #CountryMapping m ON m.[OldId] = b.[IdCountry];
        ALTER TABLE [Store].[Brand] WITH CHECK ADD CONSTRAINT [FK_Store_Brand_Country]
            FOREIGN KEY ([IdCountry]) REFERENCES [Geo].[Country] ([Id]);
        DROP TABLE [Store].[Country];
    ');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'028-consolidate-countries');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
