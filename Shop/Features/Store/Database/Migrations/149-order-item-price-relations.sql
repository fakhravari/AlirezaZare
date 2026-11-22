SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'149-order-item-price-relations')
    BEGIN
        IF COL_LENGTH(N'Store.OrderItem', N'IdOrder') IS NULL OR COL_LENGTH(N'Store.OrderItem', N'OptionsJson') IS NULL
            THROW 51490, 'Expected pre-migration OrderItem schema is missing.', 1;
        IF EXISTS (SELECT 1 FROM Store.OrderItem WHERE ISJSON(OptionsJson) <> 1)
            THROW 51491, 'Order item options contain invalid JSON.', 1;
        IF EXISTS (
            SELECT 1 FROM Store.OrderItem i
            WHERE NOT EXISTS (SELECT 1 FROM Store.PriceVersion v WHERE v.IdProduct = i.IdProduct AND v.IdSalesPlan = i.IdSalesPlan
                AND v.IdAccessory IS NULL AND (v.IdVariant = i.IdVariant OR (v.IdVariant IS NULL AND i.IdVariant IS NULL)))
        ) THROW 51492, 'An order item has no matching product or variant price version.', 1;
        IF EXISTS (
            SELECT 1 FROM Store.OrderItem i CROSS APPLY OPENJSON(i.OptionsJson, '$.accessories') a
            WHERE ISNULL(JSON_VALUE(a.value, '$.selected'), 'true') <> 'false'
              AND (TRY_CONVERT(bigint, JSON_VALUE(a.value, '$.id')) IS NULL OR NOT EXISTS (
                SELECT 1 FROM Store.PriceVersion v WHERE v.IdProduct=i.IdProduct AND v.IdSalesPlan=i.IdSalesPlan
                  AND v.IdAccessory=TRY_CONVERT(bigint, JSON_VALUE(a.value, '$.id'))))
        ) THROW 51493, 'An accessory selection has no matching price version.', 1;

        ALTER TABLE Store.OrderItem ADD IdPriceVersion bigint NULL;
        EXEC sys.sp_executesql N'
            UPDATE i SET IdPriceVersion = COALESCE(existing.Id, candidate.Id)
            FROM Store.OrderItem i
            OUTER APPLY (SELECT TOP (1) v.Id FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id=r.IdPriceVersion
                WHERE r.IdOrderItem=i.Id AND v.IdAccessory IS NULL AND v.IdProduct=i.IdProduct AND v.IdSalesPlan=i.IdSalesPlan
                  AND (v.IdVariant=i.IdVariant OR (v.IdVariant IS NULL AND i.IdVariant IS NULL)) ORDER BY v.Id DESC) existing
            CROSS APPLY (SELECT TOP (1) v.Id FROM Store.PriceVersion v WHERE v.IdProduct=i.IdProduct AND v.IdSalesPlan=i.IdSalesPlan
                AND v.IdAccessory IS NULL AND (v.IdVariant=i.IdVariant OR (v.IdVariant IS NULL AND i.IdVariant IS NULL))
                ORDER BY v.IsActive DESC, v.Id DESC) candidate;';
        INSERT Store.OrderItemPriceReference (IdOrderItem, IdPriceVersion)
        SELECT DISTINCT i.Id, chosen.Id
        FROM Store.OrderItem i CROSS APPLY OPENJSON(i.OptionsJson, '$.accessories') a
        CROSS APPLY (SELECT TOP (1) v.Id FROM Store.PriceVersion v WHERE v.IdProduct=i.IdProduct AND v.IdSalesPlan=i.IdSalesPlan
            AND v.IdAccessory=TRY_CONVERT(bigint, JSON_VALUE(a.value, '$.id')) ORDER BY v.IsActive DESC, v.Id DESC) chosen
        WHERE ISNULL(JSON_VALUE(a.value, '$.selected'), 'true') <> 'false'
          AND NOT EXISTS (SELECT 1 FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id=r.IdPriceVersion
              WHERE r.IdOrderItem=i.Id AND v.IdAccessory=TRY_CONVERT(bigint, JSON_VALUE(a.value, '$.id')));
        DELETE r FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id=r.IdPriceVersion WHERE v.IdAccessory IS NULL;
        IF EXISTS (SELECT 1 FROM Store.OrderItemPriceReference r JOIN Store.OrderItem i ON i.Id=r.IdOrderItem JOIN Store.PriceVersion v ON v.Id=r.IdPriceVersion
                   WHERE v.IdAccessory IS NULL OR v.IdProduct<>i.IdProduct OR v.IdSalesPlan<>i.IdSalesPlan)
            THROW 51494, 'An accessory price reference has an incompatible product or plan.', 1;
        ALTER TABLE Store.OrderItem ALTER COLUMN IdPriceVersion bigint NOT NULL;
        ALTER TABLE Store.OrderItem ADD CONSTRAINT FK_Store_OrderItem_PriceVersion FOREIGN KEY (IdPriceVersion) REFERENCES Store.PriceVersion(Id);
        ALTER TABLE Store.OrderItem DROP CONSTRAINT CK_Account_CustomerOrderItem_Options;
        ALTER TABLE Store.OrderItem DROP COLUMN ProductNameSnapshot, UnitAmount, OptionsJson;
        EXEC(N'CREATE TRIGGER Store.TR_OrderItem_ValidatePrice ON Store.OrderItem AFTER INSERT, UPDATE AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (SELECT 1 FROM inserted i JOIN Store.PriceVersion v ON v.Id=i.IdPriceVersion
                WHERE v.IdAccessory IS NOT NULL OR v.IdProduct<>i.IdProduct OR v.IdSalesPlan<>i.IdSalesPlan
                  OR v.IdVariant<>i.IdVariant OR (v.IdVariant IS NULL AND i.IdVariant IS NOT NULL) OR (v.IdVariant IS NOT NULL AND i.IdVariant IS NULL))
                THROW 51495, ''Order item price must match its product, variant and sales plan.'', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN Store.OrderItemPriceReference r ON r.IdOrderItem=i.Id JOIN Store.PriceVersion v ON v.Id=r.IdPriceVersion
                WHERE v.IdAccessory IS NULL OR v.IdProduct<>i.IdProduct OR v.IdSalesPlan<>i.IdSalesPlan)
                THROW 51496, ''Accessory price must match its order item.'', 1;
        END');
        EXEC(N'CREATE TRIGGER Store.TR_OrderItemPriceReference_Validate ON Store.OrderItemPriceReference AFTER INSERT, UPDATE AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (SELECT 1 FROM inserted x JOIN Store.OrderItem i ON i.Id=x.IdOrderItem JOIN Store.PriceVersion v ON v.Id=x.IdPriceVersion
                WHERE v.IdAccessory IS NULL OR v.IdProduct<>i.IdProduct OR v.IdSalesPlan<>i.IdSalesPlan)
                THROW 51497, ''Order item price reference must be an accessory price for the same product and sales plan.'', 1;
            IF EXISTS (SELECT 1 FROM Store.OrderItemPriceReference r JOIN inserted x ON x.IdOrderItem=r.IdOrderItem
                JOIN Store.PriceVersion v ON v.Id=r.IdPriceVersion
                GROUP BY r.IdOrderItem,v.IdAccessory HAVING COUNT(DISTINCT r.IdPriceVersion)>1)
                THROW 51498, ''An order item cannot contain multiple prices for one accessory.'', 1;
        END');
        EXEC(N'CREATE VIEW Store.OrderItemAmount AS
            SELECT i.Id AS IdOrderItem,
                CONVERT(decimal(19,0), ROUND(v.PriceAmount * (100 - v.DiscountPercent) / 100, 0) + COALESCE(a.AccessoryAmount, 0)) AS UnitAmount
            FROM Store.OrderItem i JOIN Store.PriceVersion v ON v.Id=i.IdPriceVersion
            OUTER APPLY (SELECT SUM(p.PriceAmount) AS AccessoryAmount FROM Store.OrderItemPriceReference r
                JOIN Store.PriceVersion p ON p.Id=r.IdPriceVersion WHERE r.IdOrderItem=i.Id) a');
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'149-order-item-price-relations');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
