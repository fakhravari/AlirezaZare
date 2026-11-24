SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM Store.[Order] WHERE OrderNumber=N'DEMO-INVOICE-20261122')
    BEGIN
        DECLARE @source bigint = (SELECT TOP (1) o.Id FROM Store.[Order] o WHERE o.PaymentMethod=N'demo' AND EXISTS (SELECT 1 FROM Store.OrderItem i WHERE i.IdOrder=o.Id) ORDER BY o.Id);
        DECLARE @plan bigint, @product bigint, @variant bigint, @plainProduct bigint;
        DECLARE @plainPrice bigint, @variantPrice bigint, @accessoryPrice bigint;
        DECLARE @total decimal(19,0), @newOrder bigint, @variantWithAccessoryItem bigint;

        SELECT TOP (1)
            @variantPrice = variantPrice.Id,
            @product = variantPrice.IdProduct,
            @variant = variantPrice.IdVariant,
            @plan = variantPrice.IdSalesPlan,
            @accessoryPrice = accessoryPrice.Id
        FROM Store.PriceVersion variantPrice
        JOIN Store.PriceVersion accessoryPrice
          ON accessoryPrice.IdProduct=variantPrice.IdProduct
         AND accessoryPrice.IdSalesPlan=variantPrice.IdSalesPlan
         AND accessoryPrice.IdAccessory IS NOT NULL
         AND accessoryPrice.IsActive=1
        WHERE variantPrice.IsActive=1
          AND variantPrice.IdVariant IS NOT NULL
          AND variantPrice.IdAccessory IS NULL
          AND EXISTS (SELECT 1 FROM Store.PriceVersion basePrice
                      WHERE basePrice.IdProduct=variantPrice.IdProduct
                        AND basePrice.IdSalesPlan=variantPrice.IdSalesPlan
                        AND basePrice.IdVariant IS NULL
                        AND basePrice.IdAccessory IS NULL
                        AND basePrice.IsActive=1)
        ORDER BY variantPrice.Id, accessoryPrice.Id;

        SELECT TOP (1) @plainPrice=price.Id, @plainProduct=price.IdProduct
        FROM Store.PriceVersion price
        WHERE price.IdSalesPlan=@plan
          AND price.IdVariant IS NULL
          AND price.IdAccessory IS NULL
          AND price.IsActive=1
          AND NOT EXISTS (SELECT 1 FROM Warehouse.ProductVariant variant WHERE variant.IdProduct=price.IdProduct)
          AND price.IdProduct<>@product
        ORDER BY price.Id;

        IF @source IS NULL OR @variantPrice IS NULL OR @plainPrice IS NULL OR @accessoryPrice IS NULL
            THROW 51510, 'Demo source and active plain, variant and accessory prices are required.', 1;

        SET @total =
            2 * (SELECT FinalPriceAmount FROM Store.PriceVersion WHERE Id=@plainPrice)
          + 1 * (SELECT FinalPriceAmount FROM Store.PriceVersion WHERE Id=@variantPrice)
          + 3 * (SELECT CONVERT(decimal(19,0), variantPrice.FinalPriceAmount + accessoryPrice.FinalPriceAmount)
                 FROM Store.PriceVersion variantPrice CROSS JOIN Store.PriceVersion accessoryPrice
                 WHERE variantPrice.Id=@variantPrice AND accessoryPrice.Id=@accessoryPrice);

        INSERT Store.[Order]
            (IdProfile,OrderNumber,IdStatus,PaymentMethod,IdBank,ItemCount,TotalAmount,SubtotalAmount,CouponDiscountAmount,CreatedAtUtc,AddressSnapshotJson)
        SELECT source.IdProfile,N'DEMO-INVOICE-20261122',status.Id,N'demo',source.IdBank,5,@total,@total,0,SYSUTCDATETIME(),source.AddressSnapshotJson
        FROM Store.[Order] source CROSS JOIN Store.OrderStatus status WHERE source.Id=@source AND status.Code=N'pending-payment';
        SET @newOrder=SCOPE_IDENTITY();

        -- Plain product and two lines sharing the same product, plan and accessory selection.
        INSERT Store.OrderItem (IdOrder,IdProduct,IdVariant,IdSalesPlan,Quantity,IdPriceVersion)
        VALUES (@newOrder,@plainProduct,NULL,@plan,2,@plainPrice),
               (@newOrder,@product,@variant,@plan,1,@variantPrice),
               (@newOrder,@product,@variant,@plan,2,@variantPrice);

        INSERT Store.OrderAccessory (IdOrder,IdProduct,IdSalesPlan,IdAccessory,IdPriceVersion,AccessoryNameSnapshot,UnitAmountSnapshot)
        SELECT @newOrder,@product,@plan,price.IdAccessory,price.Id,accessory.Name,price.FinalPriceAmount
        FROM Store.PriceVersion price JOIN Store.Accessory accessory ON accessory.Id=price.IdAccessory
        WHERE price.Id=@accessoryPrice;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
