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

    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'005-document-audit')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    CREATE TABLE [Warehouse].[DocumentAudit]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Warehouse_DocumentAudit] PRIMARY KEY,
        [DocumentId] BIGINT NOT NULL,
        [DocumentNumber] BIGINT NOT NULL,
        [WarehouseId] BIGINT NOT NULL,
        [Action] NVARCHAR(20) NOT NULL,
        [ActorUserKey] NVARCHAR(128) NOT NULL,
        [OccurredAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_DocumentAudit_OccurredAtUtc] DEFAULT SYSUTCDATETIME(),
        [Reason] NVARCHAR(1000) NULL,
        [BeforeJson] NVARCHAR(MAX) NULL,
        [AfterJson] NVARCHAR(MAX) NULL,
        CONSTRAINT [CK_Warehouse_DocumentAudit_Action] CHECK ([Action] IN (N'Created', N'Updated', N'Deleted')),
        CONSTRAINT [CK_Warehouse_DocumentAudit_Snapshots] CHECK
            (([BeforeJson] IS NULL OR ISJSON([BeforeJson]) = 1) AND ([AfterJson] IS NULL OR ISJSON([AfterJson]) = 1))
    );

    CREATE INDEX [IX_Warehouse_DocumentAudit_Document_Time]
        ON [Warehouse].[DocumentAudit] ([DocumentId], [OccurredAtUtc], [Id]);
    CREATE INDEX [IX_Warehouse_DocumentAudit_Warehouse_Time]
        ON [Warehouse].[DocumentAudit] ([WarehouseId], [OccurredAtUtc], [Id]);

    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'005-document-audit');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
