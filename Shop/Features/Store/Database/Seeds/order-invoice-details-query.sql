-- Invoice detail query for one order. Set @OrderNumber to the order being reviewed.
DECLARE @OrderNumber nvarchar(32) = N'DEMO-INVOICE-20261122';

SELECT
    o.Id AS OrderId,
    o.OrderNumber,
    o.OrderedAtUtc,
    o.Status,
    o.PaymentMethod,
    o.IdBank,
    bank.NameFa AS BankName,
    customer.Id AS CustomerId,
    customer.FirstName AS CustomerFirstName,
    customer.LastName AS CustomerLastName,
    customer.Email AS CustomerEmail,
    customer.Phone AS CustomerPhone,
    o.AddressSnapshotJson,
    o.ShippingCarrier,
    o.ShippingTrackingCode,
    o.ItemCount AS StoredItemCount,
    COUNT(*) OVER (PARTITION BY o.Id) AS LineCount,
    SUM(i.Quantity) OVER (PARTITION BY o.Id) AS CalculatedItemCount,
    o.SubtotalAmount,
    SUM(amount.UnitAmount * i.Quantity) OVER (PARTITION BY o.Id) AS CalculatedSubtotalAmount,
    coupon.Code AS CouponCode,
    o.CouponDiscountAmount,
    o.TotalAmount,
    i.Id AS OrderItemId,
    i.IdProduct,
    product.NameFa AS ProductName,
    i.IdVariant,
    variantName.Name AS VariantName,
    i.IdSalesPlan,
    salesPlan.DisplayName AS SalesPlanName,
    salesPlan.StoreSalesPlanKey AS SalesPlanKey,
    i.IdPriceVersion AS MainPriceVersionId,
    mainPrice.PriceBeforeDiscountAmount AS MainPriceBeforeDiscount,
    mainPrice.DiscountPercent AS MainPriceDiscountPercent,
    i.Quantity,
    amount.UnitAmount,
    amount.UnitAmount * i.Quantity AS LineTotal,
    JSON_QUERY((
        SELECT component, name, priceVersionId, recordedPriceAmount, discountPercent, calculatedUnitAmount
        FROM (
            SELECT CASE WHEN mainPrice.IdVariant IS NULL THEN N'product' ELSE N'variant' END AS component,
                   COALESCE(variantName.Name, product.NameFa) AS name,
                   mainPrice.Id AS priceVersionId, mainPrice.PriceBeforeDiscountAmount AS recordedPriceAmount,
                   mainPrice.DiscountPercent AS discountPercent,
                   mainPrice.FinalPriceAmount AS calculatedUnitAmount
            UNION ALL
            SELECT N'accessory', reference.AccessoryNameSnapshot, accessoryPrice.Id, accessoryPrice.PriceBeforeDiscountAmount,
                   accessoryPrice.DiscountPercent, reference.UnitAmountSnapshot
            FROM Store.OrderAccessory reference
            JOIN Store.PriceVersion accessoryPrice ON accessoryPrice.Id=reference.IdPriceVersion
            WHERE reference.IdOrder=o.Id AND reference.IdProduct=i.IdProduct AND reference.IdSalesPlan=i.IdSalesPlan
        ) components
        FOR JSON PATH
    )) AS PriceComponentsJson,
    JSON_QUERY((
        SELECT history.PreviousStatus, history.NewStatus, history.ChangedAtUtc
        FROM Store.OrderShippingHistory history
        WHERE history.IdOrder=o.Id
        ORDER BY history.Id
        FOR JSON PATH
    )) AS ShippingHistoryJson,
    JSON_QUERY((
        SELECT history.Carrier, history.TrackingCode, history.ChangedAtUtc
        FROM Store.OrderShipmentTrackingHistory history
        WHERE history.IdOrder=o.Id
        ORDER BY history.Id
        FOR JSON PATH
    )) AS TrackingHistoryJson
FROM Store.[Order] o
JOIN Account.Profile customer ON customer.Id=o.IdProfile
JOIN Store.Bank bank ON bank.Id=o.IdBank
JOIN Store.OrderItem i ON i.IdOrder=o.Id
JOIN Warehouse.Product product ON product.Id=i.IdProduct
JOIN Warehouse.SalesPlanReference salesPlan ON salesPlan.Id=i.IdSalesPlan
JOIN Store.OrderItemAmount amount ON amount.IdOrderItem=i.Id
JOIN Store.PriceVersion mainPrice ON mainPrice.Id=i.IdPriceVersion
LEFT JOIN Warehouse.ProductVariant variant ON variant.Id=i.IdVariant
LEFT JOIN Warehouse.VariantName variantName ON variantName.Id=variant.IdVariantName
LEFT JOIN Store.Coupon coupon ON coupon.Id=o.IdCoupon
WHERE o.OrderNumber=@OrderNumber
ORDER BY i.Id;
