DECLARE @ProfileId BIGINT = 90;

;WITH CartRows AS
(
    SELECT
        c.IdProfile,
        ci.IdCart,
        ci.Id AS IdCartItem,
        ci.IdProduct,
        p.NameFa AS ProductName,
        ci.IdVariant AS IdProductVariant,
        ci.IdSalesPlan,
        ci.Quantity,
        COALESCE(vp.PriceAmount, pp.PriceAmount, 0) AS BaseAmount,
        COALESCE(vp.DiscountPercent, pp.DiscountPercent, 0) AS DiscountPercent
    FROM Account.Cart AS c
    JOIN Account.CartItem AS ci ON ci.IdCart = c.Id
    JOIN Warehouse.Product AS p ON p.Id = ci.IdProduct
       AND p.IsStoreEnabled = 1
    JOIN Warehouse.SalesPlanReference AS sp ON sp.Id = ci.IdSalesPlan
       AND sp.IsEnabled = 1
       AND (sp.StartsAtUtc IS NULL OR sp.StartsAtUtc <= SYSUTCDATETIME())
       AND (sp.EndsAtUtc IS NULL OR sp.EndsAtUtc > SYSUTCDATETIME())
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount, version.DiscountPercent
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.IdProduct = ci.IdProduct
          AND version.IdSalesPlan = ci.IdSalesPlan
          AND version.IdVariant IS NULL
          AND version.IdAccessory IS NULL        ORDER BY version.Id DESC
    ) AS pp
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount, version.DiscountPercent
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.IdProduct = ci.IdProduct
          AND version.IdSalesPlan = ci.IdSalesPlan
          AND version.IdVariant = ci.IdVariant
          AND EXISTS (SELECT 1 FROM Store.ProductVariantSalesPlanOffer AS variantOffer WHERE variantOffer.IdVariant = ci.IdVariant AND variantOffer.IdSalesPlan = ci.IdSalesPlan AND variantOffer.IsEnabled = 1)
          AND version.IdAccessory IS NULL        ORDER BY version.Id DESC
    ) AS vp
    WHERE c.IdProfile = @ProfileId
      AND
      (
          ci.IdVariant IS NULL OR EXISTS
          (
              SELECT 1
              FROM Warehouse.ProductVariant AS activeVariant
              WHERE activeVariant.Id = ci.IdVariant
                AND activeVariant.IdProduct = ci.IdProduct
                AND activeVariant.IsActive = 1
          )
      )
),
Components AS
(
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
        CONVERT(DECIMAL(19, 0), accessoryVersion.PriceAmount) AS BaseAmount,
        CONVERT(DECIMAL(5, 2), 0) AS DiscountPercent
    FROM CartRows AS cr
    JOIN Store.ProductAccessory AS pa ON pa.IdProduct = cr.IdProduct
    JOIN Store.Accessory AS accessory ON accessory.Id = pa.IdAccessory AND accessory.IsActive = 1
    LEFT JOIN Account.CartItemAccessory AS selected ON selected.IdCartItem = cr.IdCartItem AND selected.IdProductAccessory = pa.Id
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceAmount
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.IdProduct = cr.IdProduct
          AND version.IdSalesPlan = cr.IdSalesPlan
          AND version.IdAccessory = pa.IdAccessory
          AND version.IdVariant IS NULL
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
    CONVERT(DECIMAL(19, 0), ROUND(BaseAmount * DiscountPercent / 100.0, 0)) AS DiscountAmount,
    CONVERT(DECIMAL(19, 0), BaseAmount - ROUND(BaseAmount * DiscountPercent / 100.0, 0)) AS FinalUnitAmount,
    CONVERT(DECIMAL(19, 0), (BaseAmount - ROUND(BaseAmount * DiscountPercent / 100.0, 0)) * Quantity) AS LineTotal
FROM Components
ORDER BY
    IdProfile,
    IdCart,
    IdCartItem,
    CASE RowType
        WHEN N'کالا' THEN 1
            ELSE 2
    END,
    ComponentId;
