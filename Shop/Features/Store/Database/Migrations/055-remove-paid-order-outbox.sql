SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'055-remove-paid-order-outbox')
    BEGIN
        IF OBJECT_ID(N'[Store].[PaidOrderOutbox]', N'U') IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM [Store].[PaidOrderOutbox] WHERE [ProcessedAtUtc] IS NULL)
                THROW 51055, 'Resolve pending paid-order outbox rows before applying this migration.', 1;
            DROP TABLE [Store].[PaidOrderOutbox];
        END;
        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'055-remove-paid-order-outbox');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
