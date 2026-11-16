SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'010-company-contact-details')
    BEGIN COMMIT TRANSACTION; RETURN; END;
    IF NOT EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'009-aggregated-sales-dispatch')
        THROW 51010, 'Warehouse migration 009 is required.', 1;

    IF COL_LENGTH(N'Warehouse.Company', N'Phone') IS NULL
        ALTER TABLE [Warehouse].[Company] ADD [Phone] NVARCHAR(50) NULL;
    IF COL_LENGTH(N'Warehouse.Company', N'Address') IS NULL
        ALTER TABLE [Warehouse].[Company] ADD [Address] NVARCHAR(500) NULL;
    IF COL_LENGTH(N'Warehouse.Company', N'Description') IS NULL
        ALTER TABLE [Warehouse].[Company] ADD [Description] NVARCHAR(2000) NULL;

    INSERT [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'010-company-contact-details');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
