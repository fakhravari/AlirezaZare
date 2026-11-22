SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM Store.[Order] WHERE ExternalOrderKey=N'demo-order-price-components-20261122')
    BEGIN
        DECLARE @source bigint = (SELECT TOP (1) Id FROM Store.[Order] WHERE PaymentMethod=N'demo' AND ExternalOrderKey LIKE N'demo-order-fixture-%' ORDER BY Id);
        DECLARE @variantPrice bigint, @productPrice bigint, @accessoryPrice bigint, @product bigint, @unvariedProduct bigint, @variant bigint, @plan bigint;
        SELECT TOP (1) @variantPrice=v.Id, @product=v.IdProduct, @variant=v.IdVariant, @plan=v.IdSalesPlan
        FROM Store.PriceVersion v
        WHERE v.IsActive=1 AND v.IdVariant IS NOT NULL AND v.IdAccessory IS NULL
          AND EXISTS (SELECT 1 FROM Store.PriceVersion b WHERE b.IdProduct=v.IdProduct AND b.IdSalesPlan=v.IdSalesPlan AND b.IdVariant IS NULL AND b.IdAccessory IS NULL AND b.IsActive=1)
          AND EXISTS (SELECT 1 FROM Store.PriceVersion a WHERE a.IdProduct=v.IdProduct AND a.IdSalesPlan=v.IdSalesPlan AND a.IdAccessory IS NOT NULL AND a.IsActive=1)
        ORDER BY v.Id;
        SELECT TOP (1) @productPrice=v.Id, @unvariedProduct=v.IdProduct FROM Store.PriceVersion v WHERE v.IdSalesPlan=@plan AND v.IdVariant IS NULL AND v.IdAccessory IS NULL AND v.IsActive=1 AND NOT EXISTS (SELECT 1 FROM Warehouse.ProductVariant pv WHERE pv.IdProduct=v.IdProduct) ORDER BY v.Id;
        SELECT TOP (1) @accessoryPrice=Id FROM Store.PriceVersion WHERE IdProduct=@product AND IdSalesPlan=@plan AND IdAccessory IS NOT NULL AND IsActive=1 ORDER BY Id;
        IF @source IS NULL OR @variantPrice IS NULL OR @productPrice IS NULL OR @accessoryPrice IS NULL
            THROW 51500, 'Demo source and active product, variant and accessory prices are required.', 1;
        DECLARE @total decimal(19,0) = (SELECT SUM(CASE WHEN IdAccessory IS NULL THEN ROUND(PriceAmount*(100-DiscountPercent)/100,0) ELSE PriceAmount END)
            FROM Store.PriceVersion WHERE Id IN (@productPrice,@variantPrice,@accessoryPrice));
        DECLARE @newOrder bigint;
        INSERT Store.[Order] (IdProfile, OrderNumber, ExternalOrderKey, Status, PaymentMethod, IdBank, ItemCount, TotalAmount, SubtotalAmount, CouponDiscountAmount, OrderedAtUtc, AddressSnapshotJson)
        SELECT IdProfile, N'DEMO-PRICE-COMPONENTS-20261122', N'demo-order-price-components-20261122', N'pending-payment', N'demo', IdBank, 2, @total, @total, 0, SYSUTCDATETIME(), AddressSnapshotJson
        FROM Store.[Order] WHERE Id=@source;
        SET @newOrder=SCOPE_IDENTITY();
        INSERT Store.OrderItem (IdOrder,IdProduct,IdVariant,IdSalesPlan,Quantity,IdPriceVersion)
        VALUES (@newOrder,@unvariedProduct,NULL,@plan,1,@productPrice);
        INSERT Store.OrderItem (IdOrder,IdProduct,IdVariant,IdSalesPlan,Quantity,IdPriceVersion)
        VALUES (@newOrder,@product,@variant,@plan,1,@variantPrice);
        INSERT Store.OrderAccessory (IdOrder,IdProduct,IdSalesPlan,IdAccessory,IdPriceVersion,AccessoryNameSnapshot,UnitAmountSnapshot)
        SELECT @newOrder,@product,@plan,IdAccessory,Id,Name,PriceAmount
        FROM Store.PriceVersion JOIN Store.Accessory ON Accessory.Id=PriceVersion.IdAccessory
        WHERE PriceVersion.Id=@accessoryPrice;
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
