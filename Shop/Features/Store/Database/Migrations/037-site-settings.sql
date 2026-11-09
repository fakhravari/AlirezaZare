SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'037-site-settings')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;
    IF OBJECT_ID(N'[Warehouse].[SiteWarehouseSetting]', N'U') IS NULL
        THROW 51037, 'The existing site warehouse setting is missing.', 1;

    CREATE TABLE [Store].[SiteSetting]
    (
        [Id] TINYINT NOT NULL,
        [IdWarehouse] BIGINT NULL,
        [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_SiteSetting_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedByUserKey] NVARCHAR(128) NULL,
        CONSTRAINT [PK_Store_SiteSetting] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Store_SiteSetting_Singleton] CHECK ([Id] = 1),
        CONSTRAINT [FK_Store_SiteSetting_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id])
    );
    INSERT INTO [Store].[SiteSetting] ([Id], [IdWarehouse], [UpdatedAtUtc], [UpdatedByUserKey])
        SELECT [Id], [IdWarehouse], [UpdatedAtUtc], [UpdatedByUserKey]
        FROM [Warehouse].[SiteWarehouseSetting] WHERE [Id] = 1;
    DROP TABLE [Warehouse].[SiteWarehouseSetting];
    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'037-site-settings');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
