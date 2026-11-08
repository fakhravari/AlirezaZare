SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'024-drop-unused-store-tables')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Store].[ContentPageSection]', N'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM [Store].[ContentPageSection])
        THROW 51040, 'Store.ContentPageSection contains data and was not removed.', 1;
    IF OBJECT_ID(N'[Store].[ContactMessage]', N'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM [Store].[ContactMessage])
        THROW 51041, 'Store.ContactMessage contains data and was not removed.', 1;
    IF OBJECT_ID(N'[Store].[ContactChannel]', N'U') IS NOT NULL
       AND EXISTS (SELECT 1 FROM [Store].[ContactChannel])
        THROW 51042, 'Store.ContactChannel contains data and was not removed.', 1;

    IF OBJECT_ID(N'[Store].[ContentPageSection]', N'U') IS NOT NULL
        DROP TABLE [Store].[ContentPageSection];
    IF OBJECT_ID(N'[Store].[ContactMessage]', N'U') IS NOT NULL
        DROP TABLE [Store].[ContactMessage];
    IF OBJECT_ID(N'[Store].[ContactChannel]', N'U') IS NOT NULL
        DROP TABLE [Store].[ContactChannel];

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
    VALUES (N'024-drop-unused-store-tables');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
