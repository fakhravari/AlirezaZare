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
    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'004-document-external-reference')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF COL_LENGTH(N'Warehouse.WarehouseDocument', N'ExternalReference') IS NULL
        ALTER TABLE [Warehouse].[WarehouseDocument] ADD [ExternalReference] NVARCHAR(128) NULL;

    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'004-document-external-reference');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
