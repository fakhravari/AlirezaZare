SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'121-option-prices-by-sales-plan')
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'120-price-version-previous-version-name')
            THROW 51280, 'Migration 120 must be applied first.', 1;
        DECLARE @regular bigint;
        IF (SELECT COUNT(*) FROM Warehouse.SalesPlanReference WHERE IdStockOwnerType = 1 AND IsRegular = 1) <> 1
            THROW 51281, 'Exactly one regular store sales plan is required.', 1;
        SELECT @regular = Id FROM Warehouse.SalesPlanReference WHERE IdStockOwnerType = 1 AND IsRegular = 1;
        IF EXISTS (SELECT 1 FROM Store.PriceVersion WHERE IdSalesPlan IS NULL AND IdOptionValue IS NULL)
            THROW 51282, 'A non-option price has no sales plan; review it first.', 1;
        IF EXISTS (SELECT 1 FROM Store.PriceVersion v JOIN Store.PriceVersion previous ON previous.Id = v.IdPreviousVersion WHERE v.IdSalesPlan IS NULL AND (previous.IdSalesPlan IS NOT NULL OR previous.IdOptionValue <> v.IdOptionValue OR previous.IdProduct <> v.IdProduct))
            THROW 51283, 'A planless option price has an incompatible previous version.', 1;
        IF EXISTS (SELECT 1 FROM Store.PriceVersion v JOIN Store.PriceVersion previous ON previous.Id = v.IdPreviousVersion WHERE v.IdSalesPlan IS NOT NULL AND previous.IdSalesPlan IS NULL)
            THROW 51284, 'A plan-specific version references a planless previous version; review it first.', 1;
        IF EXISTS (SELECT 1 FROM Store.PriceVersion old JOIN Store.PriceVersion existing ON old.IdOptionValue = existing.IdOptionValue AND old.IdProduct = existing.IdProduct AND existing.IdSalesPlan IS NOT NULL WHERE old.IdSalesPlan IS NULL)
            THROW 51285, 'Planless and plan-specific option histories overlap; review them first.', 1;
        IF EXISTS (SELECT 1 FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion JOIN Store.OrderItem i ON i.Id = r.IdOrderItem LEFT JOIN Warehouse.SalesPlanReference p ON p.Id = i.IdSalesPlan AND p.IdStockOwnerType = 1 WHERE v.IdSalesPlan IS NULL AND p.Id IS NULL)
            THROW 51286, 'An order reference has an unknown store sales plan.', 1;
        IF EXISTS (SELECT 1 FROM sys.indexes i JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'Store.PriceVersion') AND c.name = N'IdSalesPlan' AND i.name NOT IN (N'IX_Store_PriceVersion_TypedLookup', N'UX_Store_PriceVersion_ActiveTypedScope'))
            THROW 51287, 'An unexpected index references IdSalesPlan.', 1;
        IF (SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID(N'Store.PriceVersion') AND ((name = N'IX_Store_PriceVersion_TypedLookup' AND is_unique = 0 AND filter_definition IS NULL) OR (name = N'UX_Store_PriceVersion_ActiveTypedScope' AND is_unique = 1 AND filter_definition = N'([IsActive]=(1))'))) <> 2
            THROW 51288, 'Unexpected price version index definitions.', 1;
        IF (SELECT COUNT(*) FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id WHERE fk.parent_object_id = OBJECT_ID(N'Store.PriceVersion') AND c.name = N'IdSalesPlan' AND fk.name = N'FK_Store_PriceVersion_SalesPlan' AND fk.referenced_object_id = OBJECT_ID(N'Warehouse.SalesPlanReference')) <> 1
            THROW 51289, 'Unexpected sales plan foreign key.', 1;

        CREATE TABLE #VersionMap (OldId bigint NOT NULL, PlanId bigint NOT NULL, NewId bigint NOT NULL, PRIMARY KEY (OldId, PlanId));
        INSERT #VersionMap (OldId, PlanId, NewId) SELECT Id, @regular, Id FROM Store.PriceVersion WHERE IdSalesPlan IS NULL;
        MERGE Store.PriceVersion AS target
        USING (SELECT v.Id AS OldId, p.Id AS PlanId, v.IdProduct, v.IdVariant, v.IdOptionValue, v.IdAccessory, v.PriceAmount, v.DiscountPercent, v.IsActive, v.CreatedAtUtc, v.IdChangedBy, v.ChangeReason
               FROM Store.PriceVersion v CROSS JOIN Warehouse.SalesPlanReference p
               WHERE v.IdSalesPlan IS NULL AND p.IdStockOwnerType = 1 AND p.Id <> @regular) AS source
        ON 1 = 0
        WHEN NOT MATCHED THEN INSERT (IdProduct, IdSalesPlan, IdVariant, IdOptionValue, IdAccessory, PriceAmount, DiscountPercent, IsActive, CreatedAtUtc, IdChangedBy, ChangeReason, IdPreviousVersion)
             VALUES (source.IdProduct, source.PlanId, source.IdVariant, source.IdOptionValue, source.IdAccessory, source.PriceAmount, source.DiscountPercent, source.IsActive, source.CreatedAtUtc, source.IdChangedBy, source.ChangeReason, NULL)
        OUTPUT source.OldId, source.PlanId, inserted.Id INTO #VersionMap (OldId, PlanId, NewId);
        UPDATE clone SET IdPreviousVersion = previousMap.NewId
        FROM #VersionMap m JOIN Store.PriceVersion clone ON clone.Id = m.NewId
        JOIN Store.PriceVersion original ON original.Id = m.OldId
        JOIN #VersionMap previousMap ON previousMap.OldId = original.IdPreviousVersion AND previousMap.PlanId = m.PlanId
        WHERE m.PlanId <> @regular;
        UPDATE Store.PriceVersion SET IdSalesPlan = @regular WHERE IdSalesPlan IS NULL;
        UPDATE reference SET IdPriceVersion = m.NewId
        FROM Store.OrderItemPriceReference reference JOIN Store.OrderItem item ON item.Id = reference.IdOrderItem
        JOIN #VersionMap m ON m.OldId = reference.IdPriceVersion AND m.PlanId = item.IdSalesPlan
        WHERE m.NewId <> m.OldId;
        DROP INDEX IX_Store_PriceVersion_TypedLookup ON Store.PriceVersion;
        DROP INDEX UX_Store_PriceVersion_ActiveTypedScope ON Store.PriceVersion;
        ALTER TABLE Store.PriceVersion DROP CONSTRAINT FK_Store_PriceVersion_SalesPlan;
        ALTER TABLE Store.PriceVersion ALTER COLUMN IdSalesPlan bigint NOT NULL;
        ALTER TABLE Store.PriceVersion WITH CHECK ADD CONSTRAINT FK_Store_PriceVersion_SalesPlan FOREIGN KEY (IdSalesPlan) REFERENCES Warehouse.SalesPlanReference(Id);
        CREATE NONCLUSTERED INDEX IX_Store_PriceVersion_TypedLookup ON Store.PriceVersion (IdProduct, IdSalesPlan, IdVariant, IdOptionValue, IdAccessory, Id);
        CREATE UNIQUE NONCLUSTERED INDEX UX_Store_PriceVersion_ActiveTypedScope ON Store.PriceVersion (IdProduct, IdSalesPlan, IdVariant, IdOptionValue, IdAccessory) WHERE IsActive = 1;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'121-option-prices-by-sales-plan');
    END
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
