SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'023-drop-runtime-dataset')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[RuntimeDataset]', N'U') IS NOT NULL
        DROP TABLE [Store].[RuntimeDataset];

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
    VALUES (N'023-drop-runtime-dataset');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
