SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @warehouse bigint, @item bigint, @plan bigint, @party bigint, @receiptType bigint,
            @issueType bigint, @pack bigint, @pair bigint, @product bigint, @document bigint, @issue bigint, @before decimal(18,3), @name nvarchar(300);
    SELECT TOP (1) @warehouse = b.IdWarehouse, @item = b.IdInventoryItem, @plan = b.IdSalesPlanReference,
        @before = b.Quantity, @name = p.Name, @product = p.Id
    FROM Warehouse.StockBalance b JOIN Warehouse.InventoryItem i ON i.Id = b.IdInventoryItem
        JOIN Warehouse.Product p ON p.Id = i.IdProduct
    WHERE b.Quantity >= 3 ORDER BY b.Id;
    SELECT TOP (1) @party = Id FROM Warehouse.Party WHERE IsActive = 1;
    SELECT TOP (1) @receiptType = Id FROM Warehouse.DocumentType WHERE IsActive = 1 AND Direction = 'R' AND AffectsStock = 1;
    SELECT @pack = Id FROM Warehouse.Unit WHERE Code = 'CAT020' AND IsActive = 1;
    SELECT @pair = Id FROM Warehouse.Unit WHERE Code = 'CAT002' AND IsActive = 1;
    IF @item IS NULL OR @party IS NULL OR @receiptType IS NULL OR @pack IS NULL OR @pair IS NULL THROW 51000, 'Required test data missing.', 1;
    IF EXISTS (SELECT 1 FROM Warehouse.ProductUnit WHERE IdProduct = @product AND IdUnit IN (@pack, @pair))
        THROW 51005, 'Test unit already assigned to selected product.', 1;
    INSERT Warehouse.ProductUnit (IdProduct, IdUnit, ConversionFactor, IsBaseUnit, IsActive)
        VALUES (@product, @pack, 6, 0, 1), (@product, @pair, 2, 0, 1);
    INSERT Warehouse.DocumentType (Code, Name, Direction, AffectsStock, IsActive)
        VALUES (N'CODEX-TEST-ISSUE', N'Codex test issue', 'I', 1, 1);
    SET @issueType = SCOPE_IDENTITY();
    INSERT Warehouse.WarehouseDocument (IdWarehouse, IdDocumentType, IdParty, IdSalesPlanReference, DocumentDate, Status, Reason, CreatedByUserKey)
        VALUES (@warehouse, @receiptType, @party, @plan, CONVERT(date, GETUTCDATE()), 'D', N'Codex test - rollback', N'codex:test');
    SET @document = SCOPE_IDENTITY();
    INSERT Warehouse.WarehouseDocumentLine (IdDocument, IdInventoryItem, IdUnit, IdSalesPlanReference, Quantity, ConversionFactor, BaseQuantity, ProductNameSnapshot, UnitNameSnapshot)
        SELECT @document, @item, pu.IdUnit, @plan, 2, pu.ConversionFactor, 12, @name, u.Name
        FROM Warehouse.ProductUnit pu JOIN Warehouse.Unit u ON u.Id=pu.IdUnit WHERE pu.IdProduct=@product AND pu.IdUnit=@pack;
    EXEC Warehouse.RebuildDocumentStock @IdDocument = @document;
    IF (SELECT Quantity FROM Warehouse.StockBalance WHERE IdWarehouse=@warehouse AND IdInventoryItem=@item AND IdSalesPlanReference=@plan) <> @before
        THROW 51001, 'Draft changed stock.', 1;
    UPDATE Warehouse.WarehouseDocument SET Status='F' WHERE Id=@document;
    EXEC Warehouse.RebuildDocumentStock @IdDocument = @document;
    IF (SELECT Quantity FROM Warehouse.StockBalance WHERE IdWarehouse=@warehouse AND IdInventoryItem=@item AND IdSalesPlanReference=@plan) <> @before + 12
        THROW 51002, 'Receipt conversion failed.', 1;
    INSERT Warehouse.WarehouseDocument (IdWarehouse, IdDocumentType, IdParty, IdSalesPlanReference, DocumentDate, Status, Reason, CreatedByUserKey)
        VALUES (@warehouse, @issueType, @party, @plan, CONVERT(date, GETUTCDATE()), 'F', N'Codex test - rollback', N'codex:test');
    SET @issue = SCOPE_IDENTITY();
    INSERT Warehouse.WarehouseDocumentLine (IdDocument, IdInventoryItem, IdUnit, IdSalesPlanReference, Quantity, ConversionFactor, BaseQuantity, ProductNameSnapshot, UnitNameSnapshot)
        SELECT @issue, @item, pu.IdUnit, @plan, 1.5, pu.ConversionFactor, 3, @name, u.Name
        FROM Warehouse.ProductUnit pu JOIN Warehouse.Unit u ON u.Id=pu.IdUnit WHERE pu.IdProduct=@product AND pu.IdUnit=@pair;
    EXEC Warehouse.RebuildDocumentStock @IdDocument = @issue;
    IF (SELECT Quantity FROM Warehouse.StockBalance WHERE IdWarehouse=@warehouse AND IdInventoryItem=@item AND IdSalesPlanReference=@plan) <> @before + 9
        THROW 51003, 'Issue conversion failed.', 1;
    UPDATE Warehouse.WarehouseDocument SET Status='D' WHERE Id=@issue;
    EXEC Warehouse.RebuildDocumentStock @IdDocument = @issue;
    IF (SELECT Quantity FROM Warehouse.StockBalance WHERE IdWarehouse=@warehouse AND IdInventoryItem=@item AND IdSalesPlanReference=@plan) <> @before + 12
        THROW 51004, 'Returning issue to draft failed.', 1;
    SELECT @before AS BeforeQuantity, @before + 12 AS AfterReceipt, @before + 9 AS AfterIssue, @before + 12 AS AfterDraft;
    ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
