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
    IF OBJECT_ID(N'Store.SchemaMigration', N'U') IS NULL
        THROW 51510, 'Store.SchemaMigration is required before migration 151.', 1;

    IF NOT EXISTS (SELECT 1 FROM Store.SchemaMigration WHERE MigrationKey = N'151-order-accessories-by-product-plan')
    BEGIN
        IF OBJECT_ID(N'Store.OrderItemPriceReference', N'U') IS NULL
           OR OBJECT_ID(N'Store.OrderItem', N'U') IS NULL
           OR OBJECT_ID(N'Store.PriceVersion', N'U') IS NULL
            THROW 51511, 'Expected migration 149 order price schema is missing.', 1;

        -- This known demo fixture predates the shared-selection rule. Normalize only this demo order,
        -- then recalculate its demo totals from the existing per-line amount view.
        DECLARE @InvoiceDemoOrderId bigint =
            (SELECT Id FROM Store.[Order] WHERE ExternalOrderKey=N'demo-order-invoice-combinations-20261122' AND PaymentMethod=N'demo');
        IF @InvoiceDemoOrderId IS NOT NULL
        BEGIN
            INSERT Store.OrderItemPriceReference (IdOrderItem, IdPriceVersion)
            SELECT DISTINCT target.Id, sourceReference.IdPriceVersion
            FROM Store.OrderItem target
            JOIN Store.OrderItem source ON source.IdOrder=target.IdOrder AND source.IdProduct=target.IdProduct
                AND source.IdSalesPlan=target.IdSalesPlan AND source.Id<>target.Id
            JOIN Store.OrderItemPriceReference sourceReference ON sourceReference.IdOrderItem=source.Id
            JOIN Store.PriceVersion sourcePrice ON sourcePrice.Id=sourceReference.IdPriceVersion
            WHERE target.IdOrder=@InvoiceDemoOrderId
              AND NOT EXISTS (SELECT 1 FROM Store.OrderItemPriceReference existing
                  JOIN Store.PriceVersion existingPrice ON existingPrice.Id=existing.IdPriceVersion
                  WHERE existing.IdOrderItem=target.Id AND existingPrice.IdAccessory=sourcePrice.IdAccessory);

            UPDATE o
            SET SubtotalAmount=totals.SubtotalAmount,
                TotalAmount=totals.SubtotalAmount-o.CouponDiscountAmount
            FROM Store.[Order] o
            CROSS APPLY (SELECT CONVERT(decimal(19,0),SUM(amount.UnitAmount*item.Quantity)) AS SubtotalAmount
                FROM Store.OrderItem item JOIN Store.OrderItemAmount amount ON amount.IdOrderItem=item.Id
                WHERE item.IdOrder=o.Id) totals
            WHERE o.Id=@InvoiceDemoOrderId AND totals.SubtotalAmount IS NOT NULL;
        END;

        IF EXISTS (
            SELECT 1
            FROM Store.OrderItem i
            JOIN Store.OrderItem other ON other.IdOrder = i.IdOrder AND other.IdProduct = i.IdProduct
                AND other.IdSalesPlan = i.IdSalesPlan AND other.Id > i.Id
            WHERE EXISTS (
                SELECT v.IdAccessory
                FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion
                WHERE r.IdOrderItem = i.Id
                EXCEPT
                SELECT v.IdAccessory
                FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion
                WHERE r.IdOrderItem = other.Id
            ) OR EXISTS (
                SELECT v.IdAccessory
                FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion
                WHERE r.IdOrderItem = other.Id
                EXCEPT
                SELECT v.IdAccessory
                FROM Store.OrderItemPriceReference r JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion
                WHERE r.IdOrderItem = i.Id
            )
        ) THROW 51512, 'Accessory selections differ between order lines for one product and sales plan; migration stopped without changing data.', 1;

        IF EXISTS (
            SELECT i.IdOrder, i.IdProduct, i.IdSalesPlan, v.IdAccessory
            FROM Store.OrderItemPriceReference r
            JOIN Store.OrderItem i ON i.Id = r.IdOrderItem
            JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion
            GROUP BY i.IdOrder, i.IdProduct, i.IdSalesPlan, v.IdAccessory
            HAVING COUNT(DISTINCT r.IdPriceVersion) > 1
        ) THROW 51513, 'Conflicting accessory price versions exist for one order, product, plan and accessory.', 1;

        CREATE TABLE Store.OrderAccessory
        (
            IdOrder bigint NOT NULL,
            IdProduct bigint NOT NULL,
            IdSalesPlan bigint NOT NULL,
            IdAccessory bigint NOT NULL,
            IdPriceVersion bigint NOT NULL,
            AccessoryNameSnapshot nvarchar(300) NOT NULL,
            UnitAmountSnapshot decimal(19,0) NOT NULL,
            CONSTRAINT PK_Store_OrderAccessory PRIMARY KEY (IdOrder, IdProduct, IdSalesPlan, IdAccessory),
            CONSTRAINT FK_Store_OrderAccessory_Order FOREIGN KEY (IdOrder) REFERENCES Store.[Order](Id) ON DELETE CASCADE,
            CONSTRAINT FK_Store_OrderAccessory_Product FOREIGN KEY (IdProduct) REFERENCES Warehouse.Product(Id),
            CONSTRAINT FK_Store_OrderAccessory_SalesPlan FOREIGN KEY (IdSalesPlan) REFERENCES Warehouse.SalesPlanReference(Id),
            CONSTRAINT FK_Store_OrderAccessory_Accessory FOREIGN KEY (IdAccessory) REFERENCES Store.Accessory(Id),
            CONSTRAINT FK_Store_OrderAccessory_PriceVersion FOREIGN KEY (IdPriceVersion) REFERENCES Store.PriceVersion(Id)
        );

        INSERT Store.OrderAccessory (IdOrder, IdProduct, IdSalesPlan, IdAccessory, IdPriceVersion, AccessoryNameSnapshot, UnitAmountSnapshot)
        SELECT i.IdOrder, i.IdProduct, i.IdSalesPlan, v.IdAccessory, r.IdPriceVersion, MAX(a.Name), v.PriceAmount
        FROM Store.OrderItemPriceReference r
        JOIN Store.OrderItem i ON i.Id = r.IdOrderItem
        JOIN Store.PriceVersion v ON v.Id = r.IdPriceVersion
        JOIN Store.Accessory a ON a.Id = v.IdAccessory
        WHERE v.IdAccessory IS NOT NULL
        GROUP BY i.IdOrder, i.IdProduct, i.IdSalesPlan, v.IdAccessory, r.IdPriceVersion, v.PriceAmount;

        EXEC(N'CREATE TRIGGER Store.TR_OrderAccessory_Validate ON Store.OrderAccessory AFTER INSERT, UPDATE AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (SELECT 1 FROM inserted x JOIN Store.PriceVersion v ON v.Id=x.IdPriceVersion
                WHERE v.IdAccessory IS NULL OR v.IdAccessory<>x.IdAccessory OR v.IdProduct<>x.IdProduct OR v.IdSalesPlan<>x.IdSalesPlan)
                THROW 51514, ''Order accessory price must match its order, product, plan and accessory.'', 1;
        END');

        EXEC(N'ALTER TRIGGER Store.TR_OrderItem_ValidatePrice ON Store.OrderItem AFTER INSERT, UPDATE AS
        BEGIN
            SET NOCOUNT ON;
            IF EXISTS (SELECT 1 FROM inserted i JOIN Store.PriceVersion v ON v.Id=i.IdPriceVersion
                WHERE v.IdAccessory IS NOT NULL OR v.IdProduct<>i.IdProduct OR v.IdSalesPlan<>i.IdSalesPlan
                  OR v.IdVariant<>i.IdVariant OR (v.IdVariant IS NULL AND i.IdVariant IS NOT NULL) OR (v.IdVariant IS NOT NULL AND i.IdVariant IS NULL))
                THROW 51515, ''Order item price must match its product, variant and sales plan.'', 1;
            IF EXISTS (SELECT 1 FROM inserted i JOIN Store.OrderAccessory a ON a.IdOrder=i.IdOrder AND a.IdProduct=i.IdProduct AND a.IdSalesPlan=i.IdSalesPlan
                JOIN Store.PriceVersion v ON v.Id=a.IdPriceVersion
                WHERE v.IdAccessory IS NULL OR v.IdAccessory<>a.IdAccessory OR v.IdProduct<>i.IdProduct OR v.IdSalesPlan<>i.IdSalesPlan)
                THROW 51516, ''Order accessory price must match its order item product and sales plan.'', 1;
        END');

        EXEC(N'ALTER VIEW Store.OrderItemAmount AS
            SELECT i.Id AS IdOrderItem,
                CONVERT(decimal(19,0), ROUND(v.PriceAmount * (100 - v.DiscountPercent) / 100, 0) + COALESCE(a.AccessoryAmount, 0)) AS UnitAmount
            FROM Store.OrderItem i JOIN Store.PriceVersion v ON v.Id=i.IdPriceVersion
            OUTER APPLY (SELECT SUM(x.UnitAmountSnapshot) AS AccessoryAmount FROM Store.OrderAccessory x
                WHERE x.IdOrder=i.IdOrder AND x.IdProduct=i.IdProduct AND x.IdSalesPlan=i.IdSalesPlan) a');

        IF OBJECT_ID(N'Store.TR_OrderItemPriceReference_Validate', N'TR') IS NOT NULL
            DROP TRIGGER Store.TR_OrderItemPriceReference_Validate;
        DROP TABLE Store.OrderItemPriceReference;
        INSERT Store.SchemaMigration (MigrationKey) VALUES (N'151-order-accessories-by-product-plan');
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
