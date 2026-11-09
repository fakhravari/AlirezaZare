SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'008-document-line-sales-plan')
    BEGIN COMMIT TRANSACTION; RETURN; END;

    IF COL_LENGTH(N'Warehouse.WarehouseDocumentLine', N'IdSalesPlanReference') IS NULL
        ALTER TABLE [Warehouse].[WarehouseDocumentLine] ADD [IdSalesPlanReference] BIGINT NULL;

    EXEC(N'UPDATE line SET [IdSalesPlanReference] = document.[IdSalesPlanReference]
        FROM [Warehouse].[WarehouseDocumentLine] AS line
        INNER JOIN [Warehouse].[WarehouseDocument] AS document ON document.[Id] = line.[IdDocument]
        WHERE line.[IdSalesPlanReference] IS NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = N'FK_Warehouse_DocumentLine_SalesPlan')
        ALTER TABLE [Warehouse].[WarehouseDocumentLine] ADD CONSTRAINT [FK_Warehouse_DocumentLine_SalesPlan]
            FOREIGN KEY ([IdSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]);

    DECLARE @Procedure NVARCHAR(MAX) = OBJECT_DEFINITION(OBJECT_ID(N'[Warehouse].[RebuildDocumentStock]'));
    SET @Procedure = REPLACE(@Procedure, CHAR(13) + CHAR(10), CHAR(10));
    DECLARE @OldPlan NVARCHAR(MAX) = N'l.[IdInventoryItem],' + CHAR(10) + N'                @IdSalesPlanReference,';
    DECLARE @NewPlan NVARCHAR(MAX) = N'l.[IdInventoryItem],' + CHAR(10) + N'                COALESCE(l.[IdSalesPlanReference], @IdSalesPlanReference),';
    DECLARE @OldGroup NVARCHAR(MAX) = N'GROUP BY l.[IdInventoryItem]' + CHAR(10);
    IF @Procedure IS NULL OR (LEN(@Procedure) - LEN(REPLACE(@Procedure, @OldPlan, N''))) / LEN(@OldPlan) <> 2
        OR (LEN(@Procedure) - LEN(REPLACE(@Procedure, @OldGroup, N''))) / LEN(@OldGroup) <> 1
        THROW 51008, 'Unexpected stock procedure definition; migration was not applied.', 1;
    SET @Procedure = REPLACE(@Procedure, @OldPlan, @NewPlan);
    SET @Procedure = REPLACE(@Procedure, @OldGroup, N'GROUP BY l.[IdInventoryItem], COALESCE(l.[IdSalesPlanReference], @IdSalesPlanReference)' + CHAR(10));
    DECLARE @ProcedureKeyword INT = CHARINDEX(N'PROCEDURE', UPPER(@Procedure));
    IF @ProcedureKeyword = 0 THROW 51008, 'Unexpected stock procedure declaration.', 1;
    SET @Procedure = N'ALTER ' + SUBSTRING(@Procedure, @ProcedureKeyword, LEN(@Procedure));
    EXEC sys.sp_executesql @Procedure;

    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'008-document-line-sales-plan');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
