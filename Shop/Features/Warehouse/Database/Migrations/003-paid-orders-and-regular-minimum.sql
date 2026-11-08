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

    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'003-paid-orders-and-regular-minimum')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF COL_LENGTH(N'Warehouse.PaidOrderInboxLine', N'IdSalesPlanReference') IS NULL
        ALTER TABLE [Warehouse].[PaidOrderInboxLine] ADD [IdSalesPlanReference] BIGINT NULL;

    UPDATE line
    SET line.[IdSalesPlanReference] = inbox.[IdSalesPlanReference]
    FROM [Warehouse].[PaidOrderInboxLine] AS line
    INNER JOIN [Warehouse].[PaidOrderInbox] AS inbox ON inbox.[Id] = line.[IdPaidOrderInbox]
    WHERE line.[IdSalesPlanReference] IS NULL;

    IF EXISTS (SELECT 1 FROM [Warehouse].[PaidOrderInboxLine] WHERE [IdSalesPlanReference] IS NULL)
        THROW 51030, 'Paid order line could not be assigned to a sales plan.', 1;

    ALTER TABLE [Warehouse].[PaidOrderInboxLine] ALTER COLUMN [IdSalesPlanReference] BIGINT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_PaidOrderInboxLine_SalesPlanReference')
        ALTER TABLE [Warehouse].[PaidOrderInboxLine]
            ADD CONSTRAINT [FK_Warehouse_PaidOrderInboxLine_SalesPlanReference]
            FOREIGN KEY ([IdSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]);

    IF OBJECT_ID(N'[Warehouse].[PaidOrderInboxDocument]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Warehouse].[PaidOrderInboxDocument]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL,
            [IdPaidOrderInbox] BIGINT NOT NULL,
            [IdWarehouseDocument] BIGINT NOT NULL,
            CONSTRAINT [PK_Warehouse_PaidOrderInboxDocument] PRIMARY KEY ([Id]),
            CONSTRAINT [FK_Warehouse_PaidOrderInboxDocument_Inbox] FOREIGN KEY ([IdPaidOrderInbox]) REFERENCES [Warehouse].[PaidOrderInbox] ([Id]),
            CONSTRAINT [FK_Warehouse_PaidOrderInboxDocument_Document] FOREIGN KEY ([IdWarehouseDocument]) REFERENCES [Warehouse].[WarehouseDocument] ([Id]),
            CONSTRAINT [UQ_Warehouse_PaidOrderInboxDocument_Inbox_Document] UNIQUE ([IdPaidOrderInbox], [IdWarehouseDocument]),
            CONSTRAINT [UQ_Warehouse_PaidOrderInboxDocument_Document] UNIQUE ([IdWarehouseDocument])
        );
    END;

    IF COL_LENGTH(N'Warehouse.PaidOrderInbox', N'IdWarehouseDocument') IS NOT NULL
    BEGIN
        INSERT INTO [Warehouse].[PaidOrderInboxDocument] ([IdPaidOrderInbox], [IdWarehouseDocument])
        SELECT inbox.[Id], inbox.[IdWarehouseDocument]
        FROM [Warehouse].[PaidOrderInbox] AS inbox
        WHERE inbox.[IdWarehouseDocument] IS NOT NULL
          AND NOT EXISTS
          (
              SELECT 1 FROM [Warehouse].[PaidOrderInboxDocument] AS link
              WHERE link.[IdWarehouseDocument] = inbox.[IdWarehouseDocument]
          );

        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_PaidOrderInbox_WarehouseDocument')
            ALTER TABLE [Warehouse].[PaidOrderInbox] DROP CONSTRAINT [FK_Warehouse_PaidOrderInbox_WarehouseDocument];
        ALTER TABLE [Warehouse].[PaidOrderInbox] DROP COLUMN [IdWarehouseDocument];
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[WarehouseDocument]') AND [name] = N'UX_Warehouse_WarehouseDocument_CustomerOrder')
        DROP INDEX [UX_Warehouse_WarehouseDocument_CustomerOrder] ON [Warehouse].[WarehouseDocument];

    IF EXISTS
    (
        SELECT [CustomerOrderKey], [IdSalesPlanReference]
        FROM [Warehouse].[WarehouseDocument]
        WHERE [CustomerOrderKey] IS NOT NULL
        GROUP BY [CustomerOrderKey], [IdSalesPlanReference]
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51031, 'Duplicate warehouse documents exist for the same order and sales plan.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[WarehouseDocument]') AND [name] = N'UX_Warehouse_WarehouseDocument_Order_Plan')
        CREATE UNIQUE INDEX [UX_Warehouse_WarehouseDocument_Order_Plan]
            ON [Warehouse].[WarehouseDocument] ([CustomerOrderKey], [IdSalesPlanReference])
            WHERE [CustomerOrderKey] IS NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_PaidOrderInbox_SalesPlanReference')
        ALTER TABLE [Warehouse].[PaidOrderInbox] DROP CONSTRAINT [FK_Warehouse_PaidOrderInbox_SalesPlanReference];
    IF COL_LENGTH(N'Warehouse.PaidOrderInbox', N'IdSalesPlanReference') IS NOT NULL
        ALTER TABLE [Warehouse].[PaidOrderInbox] DROP COLUMN [IdSalesPlanReference];

    IF (SELECT COUNT_BIG(*) FROM [Warehouse].[SalesPlanReference] WHERE [IsRegular] = 1) <> 1
        THROW 51033, 'Exactly one regular sales plan reference is required before migrating minimum stock.', 1;

    DECLARE @IdRegularSalesPlanReference BIGINT =
        (SELECT [Id] FROM [Warehouse].[SalesPlanReference] WHERE [IsRegular] = 1);

    ;WITH NonRegularMinimum AS
    (
        SELECT [IdWarehouse], [IdInventoryItem], MAX([MinimumQuantity]) AS [MinimumQuantity]
        FROM [Warehouse].[StockBalance]
        WHERE [MinimumQuantity] > 0 AND [IdSalesPlanReference] <> @IdRegularSalesPlanReference
        GROUP BY [IdWarehouse], [IdInventoryItem]
    )
    INSERT INTO [Warehouse].[StockBalance]
        ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [Quantity], [MinimumQuantity])
    SELECT source.[IdWarehouse], source.[IdInventoryItem], @IdRegularSalesPlanReference, 0, source.[MinimumQuantity]
    FROM NonRegularMinimum AS source
    WHERE NOT EXISTS
    (
        SELECT 1 FROM [Warehouse].[StockBalance] AS regularBalance
        WHERE regularBalance.[IdWarehouse] = source.[IdWarehouse]
          AND regularBalance.[IdInventoryItem] = source.[IdInventoryItem]
          AND regularBalance.[IdSalesPlanReference] = @IdRegularSalesPlanReference
    );

    ;WITH NonRegularMinimum AS
    (
        SELECT [IdWarehouse], [IdInventoryItem], MAX([MinimumQuantity]) AS [MinimumQuantity]
        FROM [Warehouse].[StockBalance]
        WHERE [MinimumQuantity] > 0 AND [IdSalesPlanReference] <> @IdRegularSalesPlanReference
        GROUP BY [IdWarehouse], [IdInventoryItem]
    )
    UPDATE regularBalance
    SET regularBalance.[MinimumQuantity] = CASE
        WHEN regularBalance.[MinimumQuantity] >= source.[MinimumQuantity] THEN regularBalance.[MinimumQuantity]
        ELSE source.[MinimumQuantity]
    END
    FROM [Warehouse].[StockBalance] AS regularBalance
    INNER JOIN NonRegularMinimum AS source
        ON source.[IdWarehouse] = regularBalance.[IdWarehouse]
       AND source.[IdInventoryItem] = regularBalance.[IdInventoryItem]
    WHERE regularBalance.[IdSalesPlanReference] = @IdRegularSalesPlanReference;

    UPDATE [Warehouse].[StockBalance]
    SET [MinimumQuantity] = 0
    WHERE [IdSalesPlanReference] <> @IdRegularSalesPlanReference AND [MinimumQuantity] > 0;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[Warehouse].[PaidOrderInboxLine]') AND [name] = N'IX_Warehouse_PaidOrderInboxLine_Order_Plan')
        CREATE INDEX [IX_Warehouse_PaidOrderInboxLine_Order_Plan]
            ON [Warehouse].[PaidOrderInboxLine] ([IdPaidOrderInbox], [IdSalesPlanReference]);

    EXEC(N'CREATE OR ALTER TRIGGER [Warehouse].[TR_StockBalance_RegularPlanMinimumOnly]
        ON [Warehouse].[StockBalance]
        AFTER INSERT, UPDATE
        AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS
            (
                SELECT 1
                FROM inserted AS balance
                INNER JOIN [Warehouse].[SalesPlanReference] AS plan
                    ON plan.[Id] = balance.[IdSalesPlanReference]
                WHERE plan.[IsRegular] = 0 AND balance.[MinimumQuantity] > 0
            )
                THROW 51032, ''Minimum stock can only be configured for the regular sales plan.'', 1;
        END;');

    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'003-paid-orders-and-regular-minimum');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
