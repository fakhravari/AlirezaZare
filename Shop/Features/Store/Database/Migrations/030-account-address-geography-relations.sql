SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'030-account-address-geography-relations')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Account].[Address]', N'U') IS NULL
       OR OBJECT_ID(N'[Geo].[Country]', N'U') IS NULL
       OR OBJECT_ID(N'[Geo].[Province]', N'U') IS NULL
       OR OBJECT_ID(N'[Geo].[City]', N'U') IS NULL
        THROW 51030, 'Account address and geography tables are required.', 1;

    ALTER TABLE [Account].[Address] ADD
        [IdCountry] BIGINT NULL,
        [IdProvince] BIGINT NULL,
        [IdCity] BIGINT NULL;

    EXEC(N'
        UPDATE a
        SET [IdCountry] = country.[Id]
        FROM [Account].[Address] a
        CROSS APPLY
        (
            SELECT TOP (1) c.[Id]
            FROM [Geo].[Country] c
            WHERE c.[Id] = TRY_CONVERT(BIGINT, a.[CountryId])
               OR c.[CountryCode] = a.[CountryId]
               OR c.[NameFa] = a.[Country]
               OR c.[NameEn] = a.[Country]
            ORDER BY CASE WHEN c.[Id] = TRY_CONVERT(BIGINT, a.[CountryId]) THEN 0
                          WHEN c.[CountryCode] = a.[CountryId] THEN 1 ELSE 2 END
        ) country;

        UPDATE a
        SET [IdProvince] = province.[Id]
        FROM [Account].[Address] a
        CROSS APPLY
        (
            SELECT TOP (1) p.[Id]
            FROM [Geo].[Province] p
            WHERE p.[IdCountry] = a.[IdCountry]
              AND (p.[Id] = TRY_CONVERT(BIGINT, a.[ProvinceId]) OR p.[NameFa] = a.[Province] OR p.[NameEn] = a.[Province])
            ORDER BY CASE WHEN p.[Id] = TRY_CONVERT(BIGINT, a.[ProvinceId]) THEN 0 ELSE 1 END
        ) province;

        UPDATE a
        SET [IdCity] = city.[Id]
        FROM [Account].[Address] a
        CROSS APPLY
        (
            SELECT TOP (1) c.[Id]
            FROM [Geo].[City] c
            WHERE c.[IdCountry] = a.[IdCountry]
              AND c.[IdProvince] = a.[IdProvince]
              AND (c.[Id] = TRY_CONVERT(BIGINT, a.[CityId]) OR c.[NameFa] = a.[City] OR c.[NameEn] = a.[City])
            ORDER BY CASE WHEN c.[Id] = TRY_CONVERT(BIGINT, a.[CityId]) THEN 0 ELSE 1 END
        ) city;
    ');

    DECLARE @UnmappedAddressCount BIGINT;
    EXEC sys.sp_executesql
        N'SELECT @Count = COUNT_BIG(*) FROM [Account].[Address] WHERE [IdCountry] IS NULL OR [IdProvince] IS NULL OR [IdCity] IS NULL;',
        N'@Count BIGINT OUTPUT', @Count = @UnmappedAddressCount OUTPUT;

    IF @UnmappedAddressCount > 0
        THROW 51031, 'An existing account address could not be matched to country, province, and city records.', 1;

    ALTER TABLE [Geo].[Province] ADD CONSTRAINT [UQ_Geo_Province_Id_Country] UNIQUE ([Id], [IdCountry]);
    ALTER TABLE [Geo].[City] ADD CONSTRAINT [UQ_Geo_City_Id_Province_Country] UNIQUE ([Id], [IdProvince], [IdCountry]);

    EXEC(N'
        ALTER TABLE [Account].[Address] ALTER COLUMN [IdCountry] BIGINT NOT NULL;
        ALTER TABLE [Account].[Address] ALTER COLUMN [IdProvince] BIGINT NOT NULL;
        ALTER TABLE [Account].[Address] ALTER COLUMN [IdCity] BIGINT NOT NULL;

        ALTER TABLE [Account].[Address] WITH CHECK ADD CONSTRAINT [FK_Account_Address_Country]
            FOREIGN KEY ([IdCountry]) REFERENCES [Geo].[Country] ([Id]);
        ALTER TABLE [Account].[Address] WITH CHECK ADD CONSTRAINT [FK_Account_Address_Province]
            FOREIGN KEY ([IdProvince], [IdCountry]) REFERENCES [Geo].[Province] ([Id], [IdCountry]);
        ALTER TABLE [Account].[Address] WITH CHECK ADD CONSTRAINT [FK_Account_Address_City]
            FOREIGN KEY ([IdCity], [IdProvince], [IdCountry]) REFERENCES [Geo].[City] ([Id], [IdProvince], [IdCountry]);

        ALTER TABLE [Account].[Address] DROP COLUMN [Country], [CountryId], [Province], [ProvinceId], [City], [CityId];
    ');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'030-account-address-geography-relations');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
