DECLARE @ProfileId BIGINT = 90;

;WITH CartRows AS
(
    SELECT
        c.IdProfile,
        ci.IdCart,
        ci.Id AS IdCartItem,
        ci.IdProduct,
        p.Name AS ProductName,
        ci.IdProductVariant,
        ci.IdSalesPlan,
        ci.Quantity,
        COALESCE(vp.PriceAmount, vo.PriceAmount, pp.PriceAmount, po.PriceAmount, p.PriceAmount, 0) AS BaseAmount,
        COALESCE(vp.DiscountPercent, vo.DiscountPercent, pp.DiscountPercent, po.DiscountPercent, p.DiscountPercent, 0) AS DiscountPercent
    FROM Account.Cart AS c
    JOIN Account.CartItem AS ci ON ci.IdCart = c.Id
    JOIN Warehouse.Product AS p ON p.Id = ci.IdProduct
       AND p.IsStoreEnabled = 1
       AND p.IsCatalogActive = 1
    JOIN Warehouse.SalesPlanReference AS sp ON sp.Id = ci.IdSalesPlan
       AND sp.IsEnabled = 1
       AND (sp.StartsAtUtc IS NULL OR sp.StartsAtUtc <= SYSUTCDATETIME())
       AND (sp.EndsAtUtc IS NULL OR sp.EndsAtUtc > SYSUTCDATETIME())
    OUTER APPLY
    (
        SELECT TOP (1) offer.Id, offer.PriceAmount, offer.DiscountPercent
        FROM Store.ProductSalesPlanOffer AS offer
        WHERE offer.IdProduct = ci.IdProduct
          AND offer.IdSalesPlan = ci.IdSalesPlan
          AND offer.IsEnabled = 1
        ORDER BY offer.Id DESC
    ) AS po
    OUTER APPLY
    (
        SELECT TOP (1) offer.PriceAmount, offer.DiscountPercent
        FROM Store.ProductVariantSalesPlanOffer AS offer
        WHERE offer.IdProductVariant = ci.IdProductVariant
          AND offer.IdSalesPlan = ci.IdSalesPlan
          AND offer.IsEnabled = 1
        ORDER BY offer.Id DESC
    ) AS vo
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount, version.DiscountPercent
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.ScopeKey = CONCAT(N'product/',ci.IdProduct,N'/plan/',ci.IdSalesPlan)
        ORDER BY version.Id DESC
    ) AS pp
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount, version.DiscountPercent
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.ScopeKey = CONCAT(N'variant/',ci.IdProductVariant,N'/plan/',ci.IdSalesPlan)
        ORDER BY version.Id DESC
    ) AS vp
    WHERE c.IdProfile = @ProfileId
      AND
      (
          ci.IdProductVariant IS NULL OR EXISTS
          (
              SELECT 1 FROM Warehouse.ProductVariant AS activeVariant 
              WHERE activeVariant.Id = ci.IdProductVariant
                AND activeVariant.IdProduct = ci.IdProduct
                AND activeVariant.IsActive = 1
          )
      )
),
Components AS
(
    -- ردیف پایهٔ کالا
    SELECT
        cr.IdProfile,
        cr.IdCart,
        cr.IdCartItem,
        cr.IdProduct,
        cr.ProductName,
        cr.IdProductVariant,
        cr.IdSalesPlan,
        cr.Quantity,
        N'کالا' AS RowType,
        CONVERT(BIGINT, cr.IdProduct) AS ComponentId,
        cr.ProductName AS ComponentName,
        CONVERT(DECIMAL(19, 0), cr.BaseAmount) AS BaseAmount,
        CONVERT(DECIMAL(5, 2), cr.DiscountPercent) AS DiscountPercent
    FROM CartRows AS cr

    UNION ALL

    -- هر ویژگی انتخاب‌شده، یک ردیف
    SELECT
        cr.IdProfile,
        cr.IdCart,
        cr.IdCartItem,
        cr.IdProduct,
        cr.ProductName,
        cr.IdProductVariant,
        cr.IdSalesPlan,
        cr.Quantity,
        N'ویژگی' AS RowType,
        CONVERT(BIGINT, value.Id) AS ComponentId,
        CONCAT(purchaseOption.Label, N' / ', value.Label) AS ComponentName,
        CONVERT(DECIMAL(19, 0),COALESCE(optionVersion.PriceAmount, ROUND(value.PriceDelta * 1000000, 0))) AS BaseAmount,
        CONVERT(DECIMAL(5, 2), 0) AS DiscountPercent
    FROM CartRows AS cr
    JOIN Account.CartItemPurchaseOptionValue AS selected ON selected.IdCartItem = cr.IdCartItem
    JOIN Store.ProductPurchaseOptionValue AS value ON value.Id = selected.IdProductPurchaseOptionValue
    JOIN Store.ProductPurchaseOption AS purchaseOption ON purchaseOption.Id = value.IdProductPurchaseOption
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1 AND version.ScopeKey = CONCAT(N'option/', value.Id)
        ORDER BY version.Id DESC
    ) AS optionVersion

    UNION ALL

    -- هر متعلقهٔ اجباری یا انتخاب‌شده، یک ردیف
    SELECT
        cr.IdProfile,
        cr.IdCart,
        cr.IdCartItem,
        cr.IdProduct,
        cr.ProductName,
        cr.IdProductVariant,
        cr.IdSalesPlan,
        cr.Quantity,
        N'متعلقه' AS RowType,
        CONVERT(BIGINT, pa.Id) AS ComponentId,
        accessory.Name AS ComponentName,
        CONVERT(DECIMAL(19, 0),COALESCE(accessoryVersion.PriceAmount, offerAccessory.PriceAmount, pa.PriceAmount, 0)) AS BaseAmount,
        CONVERT(DECIMAL(5, 2), 0) AS DiscountPercent
    FROM CartRows AS cr
    JOIN Store.ProductAccessory AS pa ON pa.IdProduct = cr.IdProduct
    JOIN Store.Accessory AS accessory ON accessory.Id = pa.IdAccessory AND accessory.IsActive = 1
    LEFT JOIN Account.CartItemAccessory AS selected ON selected.IdCartItem = cr.IdCartItem AND selected.IdProductAccessory = pa.Id
    OUTER APPLY
    (
        SELECT TOP (1) offerAccessory.PriceAmount
        FROM Store.ProductSalesPlanOfferAccessory AS offerAccessory
        JOIN Store.ProductSalesPlanOffer AS offer ON offer.Id = offerAccessory.IdProductSalesPlanOffer
           AND offer.IdProduct = cr.IdProduct
           AND offer.IdSalesPlan = cr.IdSalesPlan
           AND offer.IsEnabled = 1
        WHERE offerAccessory.IdAccessory = pa.IdAccessory
          AND offerAccessory.IsAllowed = 1
        ORDER BY offerAccessory.Id DESC
    ) AS offerAccessory
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.ScopeKey = CONCAT(N'accessory/',cr.IdProduct,N'/',pa.IdAccessory,N'/plan/',cr.IdSalesPlan)
        ORDER BY version.Id DESC
    ) AS accessoryVersion
    WHERE pa.IsRequired = 1 OR selected.Id IS NOT NULL
)
SELECT
    IdProfile,
    IdCart,
    IdCartItem,
    IdProduct,
    ProductName,
    IdProductVariant,
    IdSalesPlan,
    Quantity,
    RowType,
    ComponentId,
    ComponentName,
    BaseAmount,
    DiscountPercent,
    CONVERT(DECIMAL(19, 0),ROUND(BaseAmount * DiscountPercent / 100.0, 0)) AS DiscountAmount,
    CONVERT(DECIMAL(19, 0),BaseAmount - ROUND(BaseAmount * DiscountPercent / 100.0, 0)) AS FinalUnitAmount,
    CONVERT(DECIMAL(19, 0),(BaseAmount - ROUND(BaseAmount * DiscountPercent / 100.0, 0)) * Quantity) AS LineTotal
FROM Components
ORDER BY
    IdProfile,
    IdCart,
    IdCartItem,
    CASE RowType
        WHEN N'کالا' THEN 1
        WHEN N'ویژگی' THEN 2
        ELSE 3
    END,
    ComponentId;
