SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'093-drop-unused-paid-order-inbox')
    BEGIN
        IF OBJECT_ID(N'[Warehouse].[PaidOrderInbox]', N'U') IS NULL
           OR OBJECT_ID(N'[Warehouse].[PaidOrderInboxLine]', N'U') IS NULL
           OR OBJECT_ID(N'[Warehouse].[PaidOrderInboxDocument]', N'U') IS NULL
            THROW 51093, 'The legacy paid-order inbox tables are not all present.', 1;

        IF EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInbox])
           OR EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxLine])
           OR EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxDocument])
            THROW 51093, 'Paid-order inbox tables contain data; archive and review it before removal.', 1;

        DROP TABLE [Warehouse].[PaidOrderInboxDocument];
        DROP TABLE [Warehouse].[PaidOrderInboxLine];
        DROP TABLE [Warehouse].[PaidOrderInbox];

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey])
        VALUES (N'093-drop-unused-paid-order-inbox');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
