SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'006-draft-documents-no-stock-effect')
    BEGIN COMMIT TRANSACTION; RETURN; END;
    EXEC(N' CREATE OR ALTER PROCEDURE [Warehouse].[RebuildDocumentStock]
    @IdDocument BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartedTransaction BIT = 0;
    DECLARE @IdWarehouse BIGINT;
    DECLARE @IdDocumentType BIGINT;
    DECLARE @IdParty BIGINT;
    DECLARE @IdSalesPlanReference BIGINT;
    DECLARE @Status CHAR(1);
    DECLARE @Direction CHAR(1);
    DECLARE @AffectsStock BIT;
    DECLARE @DocumentExists BIT = 0;
    DECLARE @ErrorMessage NVARCHAR(2048);

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTransaction = 1;
        END;

        SELECT
            @IdWarehouse = d.[IdWarehouse],
            @IdDocumentType = d.[IdDocumentType],
            @IdParty = d.[IdParty],
            @IdSalesPlanReference = d.[IdSalesPlanReference],
            @Status = d.[Status],
            @Direction = t.[Direction],
            @AffectsStock = t.[AffectsStock],
            @DocumentExists = 1
        FROM [Warehouse].[WarehouseDocument] AS d WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Warehouse].[DocumentType] AS t ON t.[Id] = d.[IdDocumentType]
        WHERE d.[Id] = @IdDocument;

        IF @DocumentExists = 0
            THROW 51001, ''Warehouse document was not found.'', 1;

        CREATE TABLE #StockDelta
        (
            [IdWarehouse] BIGINT NOT NULL,
            [IdInventoryItem] BIGINT NOT NULL,
            [IdSalesPlanReference] BIGINT NOT NULL,
            [QuantityDelta] DECIMAL(18,3) NOT NULL
        );

        INSERT INTO #StockDelta ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [QuantityDelta])
        SELECT m.[IdWarehouse], m.[IdInventoryItem], m.[IdSalesPlanReference], -SUM(m.[QuantityDelta])
        FROM [Warehouse].[StockMovement] AS m WITH (UPDLOCK, HOLDLOCK)
        WHERE m.[IdDocument] = @IdDocument
        GROUP BY m.[IdWarehouse], m.[IdInventoryItem], m.[IdSalesPlanReference];

        IF @AffectsStock = 1 AND @Status = ''F''
        BEGIN
            INSERT INTO #StockDelta ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [QuantityDelta])
            SELECT
                @IdWarehouse,
                l.[IdInventoryItem],
                @IdSalesPlanReference,
                SUM(CASE
                    WHEN @Direction = ''R'' AND @Status = ''F'' THEN l.[BaseQuantity]
                    WHEN @Direction = ''I'' AND @Status = ''F'' THEN -l.[BaseQuantity]
                    ELSE 0
                END)
            FROM [Warehouse].[WarehouseDocumentLine] AS l
            WHERE l.[IdDocument] = @IdDocument
            GROUP BY l.[IdInventoryItem]
            HAVING SUM(CASE
                    WHEN @Direction = ''R'' AND @Status = ''F'' THEN l.[BaseQuantity]
                    WHEN @Direction = ''I'' AND @Status = ''F'' THEN -l.[BaseQuantity]
                    ELSE 0
                END) <> 0;
        END;

        CREATE TABLE #AggregatedDelta
        (
            [IdWarehouse] BIGINT NOT NULL,
            [IdInventoryItem] BIGINT NOT NULL,
            [IdSalesPlanReference] BIGINT NOT NULL,
            [QuantityDelta] DECIMAL(18,3) NOT NULL,
            PRIMARY KEY ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference])
        );

        INSERT INTO #AggregatedDelta ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [QuantityDelta])
        SELECT [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], SUM([QuantityDelta])
        FROM #StockDelta
        GROUP BY [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference];

        INSERT INTO [Warehouse].[StockBalance] ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference])
        SELECT k.[IdWarehouse], k.[IdInventoryItem], k.[IdSalesPlanReference]
        FROM #AggregatedDelta AS k
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [Warehouse].[StockBalance] AS b WITH (UPDLOCK, HOLDLOCK, INDEX([UQ_Warehouse_StockBalance_Key]))
            WHERE b.[IdWarehouse] = k.[IdWarehouse]
              AND b.[IdInventoryItem] = k.[IdInventoryItem]
              AND b.[IdSalesPlanReference] = k.[IdSalesPlanReference]
        );

        IF EXISTS
        (
            SELECT 1
            FROM [Warehouse].[StockBalance] AS b WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN #AggregatedDelta AS d
                ON d.[IdWarehouse] = b.[IdWarehouse]
               AND d.[IdInventoryItem] = b.[IdInventoryItem]
               AND d.[IdSalesPlanReference] = b.[IdSalesPlanReference]
            WHERE b.[Quantity] + d.[QuantityDelta] < 0
        )
            THROW 51002, ''Stock change would make physical inventory negative.'', 1;

        UPDATE b
        SET b.[Quantity] = b.[Quantity] + d.[QuantityDelta],
            b.[UpdatedAtUtc] = SYSUTCDATETIME()
        FROM [Warehouse].[StockBalance] AS b
        INNER JOIN #AggregatedDelta AS d
            ON d.[IdWarehouse] = b.[IdWarehouse]
           AND d.[IdInventoryItem] = b.[IdInventoryItem]
           AND d.[IdSalesPlanReference] = b.[IdSalesPlanReference]
        WHERE d.[QuantityDelta] <> 0;

        DELETE FROM [Warehouse].[StockMovement] WHERE [IdDocument] = @IdDocument;

        IF @AffectsStock = 1 AND @Status = ''F''
        BEGIN
            INSERT INTO [Warehouse].[StockMovement]
            (
                [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [IdDocument], [IdDocumentLine], [QuantityDelta]
            )
            SELECT
                @IdWarehouse,
                l.[IdInventoryItem],
                @IdSalesPlanReference,
                @IdDocument,
                l.[Id],
                CASE
                    WHEN @Direction = ''R'' AND @Status = ''F'' THEN l.[BaseQuantity]
                    WHEN @Direction = ''I'' AND @Status = ''F'' THEN -l.[BaseQuantity]
                END
            FROM [Warehouse].[WarehouseDocumentLine] AS l
            WHERE l.[IdDocument] = @IdDocument;
        END;

        IF @StartedTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;');
    DECLARE @DraftId BIGINT;
    DECLARE draft_documents CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT d.[Id] FROM [Warehouse].[WarehouseDocument] AS d
        INNER JOIN [Warehouse].[StockMovement] AS m ON m.[IdDocument] = d.[Id]
        WHERE d.[Status] = 'D';
    OPEN draft_documents;
    FETCH NEXT FROM draft_documents INTO @DraftId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC [Warehouse].[RebuildDocumentStock] @IdDocument = @DraftId;
        FETCH NEXT FROM draft_documents INTO @DraftId;
    END;
    CLOSE draft_documents;
    DEALLOCATE draft_documents;
    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'006-draft-documents-no-stock-effect');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
