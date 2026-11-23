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
        CASE WHEN ci.IdVariant IS NULL THEN pp.PriceBeforeDiscountAmount ELSE vp.PriceBeforeDiscountAmount END AS BaseAmount,
        CASE WHEN ci.IdVariant IS NULL THEN pp.DiscountPercent ELSE vp.DiscountPercent END AS DiscountPercent,
        CASE WHEN ci.IdVariant IS NULL THEN pp.FinalPriceAmount ELSE vp.FinalPriceAmount END AS FinalUnitAmount
    FROM Account.Cart AS c
    JOIN Account.CartItem AS ci ON ci.IdCart = c.Id
    LEFT JOIN Warehouse.Product AS p ON p.Id = ci.IdProduct
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceBeforeDiscountAmount, version.DiscountPercent, version.FinalPriceAmount
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.IdProduct = ci.IdProduct
          AND version.IdSalesPlan = ci.IdSalesPlan
          AND version.IdVariant IS NULL
          AND version.IdAccessory IS NULL
        ORDER BY version.Id DESC
    ) AS pp
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceBeforeDiscountAmount, version.DiscountPercent, version.FinalPriceAmount
        FROM Store.PriceVersion AS version
        WHERE version.IsActive = 1
          AND version.IdProduct = ci.IdProduct
          AND version.IdSalesPlan = ci.IdSalesPlan
          AND version.IdVariant = ci.IdVariant
          AND version.IdAccessory IS NULL
        ORDER BY version.Id DESC
    ) AS vp
    WHERE c.IdProfile = @ProfileId
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
        CONVERT(DECIMAL(5, 2), cr.DiscountPercent) AS DiscountPercent,
        cr.FinalUnitAmount
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
        CONVERT(DECIMAL(19, 0), accessoryVersion.PriceBeforeDiscountAmount) AS BaseAmount,
        CONVERT(DECIMAL(5, 2), accessoryVersion.DiscountPercent) AS DiscountPercent,
        accessoryVersion.FinalPriceAmount AS FinalUnitAmount
    FROM CartRows AS cr
    JOIN Store.ProductAccessory AS pa ON pa.IdProduct = cr.IdProduct
    LEFT JOIN Store.Accessory AS accessory ON accessory.Id = pa.IdAccessory
    LEFT JOIN Account.CartItemAccessory AS selected ON selected.IdCartItem = cr.IdCartItem AND selected.IdProductAccessory = pa.Id
    OUTER APPLY
    (
        SELECT TOP (1) version.PriceBeforeDiscountAmount, version.DiscountPercent, version.FinalPriceAmount
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
    CASE WHEN BaseAmount IS NULL THEN N'missing-price' ELSE N'price-found' END AS PriceStatus,
    CONVERT(DECIMAL(19, 0), BaseAmount - FinalUnitAmount) AS DiscountAmount,
    FinalUnitAmount,
    CONVERT(DECIMAL(19, 0), FinalUnitAmount * Quantity) AS LineTotal
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
