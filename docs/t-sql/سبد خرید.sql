DECLARE @ProfileId BIGINT = 90;

;WITH CartItems AS
(
    SELECT
        cart.IdProfile,
        cart.Id AS IdCart,
        item.Id AS IdCartItem,
        item.IdProduct,
        product.NameFa AS ProductName,
        item.IdVariant AS IdProductVariant,
        item.IdSalesPlan,
        item.Quantity
    FROM Account.Cart AS cart
    INNER JOIN Account.CartItem AS item
        ON item.IdCart = cart.Id
    INNER JOIN Warehouse.Product AS product
        ON product.Id = item.IdProduct
    WHERE cart.IdProfile = @ProfileId
),
CartLines AS
(
    -- کالای ساده؛ قیمت فعال برای محصول و طرح فروش
    SELECT
        cartItem.IdProfile,
        cartItem.IdCart,
        cartItem.IdCartItem,
        cartItem.IdProduct,
        cartItem.ProductName,
        cartItem.IdProductVariant,
        cartItem.IdSalesPlan,
        cartItem.Quantity,
        price.PriceBeforeDiscountAmount AS BaseAmount,
        price.DiscountPercent,
        price.FinalPriceAmount AS FinalUnitAmount
    FROM CartItems AS cartItem
    INNER JOIN Store.PriceVersion AS price
        ON price.IdProduct = cartItem.IdProduct
       AND price.IdSalesPlan = cartItem.IdSalesPlan
       AND price.IdVariant IS NULL
       AND price.IdAccessory IS NULL
       AND price.IsActive = 1
    WHERE cartItem.IdProductVariant IS NULL

    UNION ALL

    -- تنوع؛ قیمت فعال اختصاصی همان تنوع و طرح فروش
    SELECT
        cartItem.IdProfile,
        cartItem.IdCart,
        cartItem.IdCartItem,
        cartItem.IdProduct,
        cartItem.ProductName,
        cartItem.IdProductVariant,
        cartItem.IdSalesPlan,
        cartItem.Quantity,
        price.PriceBeforeDiscountAmount AS BaseAmount,
        price.DiscountPercent,
        price.FinalPriceAmount AS FinalUnitAmount
    FROM CartItems AS cartItem
    INNER JOIN Store.PriceVersion AS price
        ON price.IdProduct = cartItem.IdProduct
       AND price.IdSalesPlan = cartItem.IdSalesPlan
       AND price.IdVariant = cartItem.IdProductVariant
       AND price.IdAccessory IS NULL
       AND price.IsActive = 1
    WHERE cartItem.IdProductVariant IS NOT NULL
),
CartAccessories AS
(
    -- یک ردیف متعلقه برای هر محصول، طرح فروش و متعلق؛ مطابق OrderAccessory
    SELECT
        line.IdProfile,
        line.IdCart,
        MIN(line.IdCartItem) AS SortCartItemId,
        line.IdProduct,
        MAX(line.ProductName) AS ProductName,
        line.IdSalesPlan,
        SUM(line.Quantity) AS Quantity,
        selectedAccessory.IdProductAccessory
    FROM CartLines AS line
    INNER JOIN Account.CartItemAccessory AS selectedAccessory
        ON selectedAccessory.IdCartItem = line.IdCartItem
    GROUP BY
        line.IdProfile,
        line.IdCart,
        line.IdProduct,
        line.IdSalesPlan,
        selectedAccessory.IdProductAccessory
),
CartComponents AS
(
    -- کالای ساده
    SELECT
        line.IdProfile,
        line.IdCart,
        line.IdCartItem,
        line.IdCartItem AS SortCartItemId,
        line.IdProduct,
        line.ProductName,
        line.IdProductVariant,
        line.IdSalesPlan,
        line.Quantity,
        N'کالا' AS ComponentType,
        CAST(NULL AS NVARCHAR(10)) AS AccessoryRequirement,
        CONVERT(BIGINT, line.IdProduct) AS ComponentId,
        CAST(NULL AS NVARCHAR(4000)) AS TreePathNames,
        CAST(NULL AS NVARCHAR(4000)) AS AccessoryName,
        line.BaseAmount,
        line.DiscountPercent,
        line.FinalUnitAmount
    FROM CartLines AS line
    WHERE line.IdProductVariant IS NULL

    UNION ALL

    -- تنوع
    SELECT
        line.IdProfile,
        line.IdCart,
        line.IdCartItem,
        line.IdCartItem AS SortCartItemId,
        line.IdProduct,
        line.ProductName,
        line.IdProductVariant,
        line.IdSalesPlan,
        line.Quantity,
        N'تنوع' AS ComponentType,
        CAST(NULL AS NVARCHAR(10)) AS AccessoryRequirement,
        CONVERT(BIGINT, variant.Id) AS ComponentId,
        variantName.TreePathNames AS TreePathNames,
        CAST(NULL AS NVARCHAR(4000)) AS AccessoryName,
        line.BaseAmount,
        line.DiscountPercent,
        line.FinalUnitAmount
    FROM CartLines AS line
    INNER JOIN Warehouse.ProductVariant AS variant
        ON variant.Id = line.IdProductVariant
    LEFT JOIN Warehouse.VariantName AS variantName
        ON variantName.Id = variant.IdVariantName
    WHERE line.IdProductVariant IS NOT NULL

    UNION ALL

    -- متعلقهٔ الزامی یا اختیاریِ انتخاب‌شده
    SELECT
        accessoryGroup.IdProfile,
        accessoryGroup.IdCart,
        CAST(NULL AS BIGINT) AS IdCartItem,
        accessoryGroup.SortCartItemId,
        accessoryGroup.IdProduct,
        accessoryGroup.ProductName,
        CAST(NULL AS BIGINT) AS IdProductVariant,
        accessoryGroup.IdSalesPlan,
        accessoryGroup.Quantity,
        N'متعلقه' AS ComponentType,
        CASE
            WHEN productAccessory.IsRequired = 1 THEN N'الزامی'
            ELSE N'اختیاری'
        END AS AccessoryRequirement,
        CONVERT(BIGINT, productAccessory.IdAccessory) AS ComponentId,
        CAST(NULL AS NVARCHAR(4000)) AS TreePathNames,
        accessory.Name AS AccessoryName,
        price.PriceBeforeDiscountAmount AS BaseAmount,
        price.DiscountPercent,
        price.FinalPriceAmount AS FinalUnitAmount
    FROM CartAccessories AS accessoryGroup
    INNER JOIN Store.ProductAccessory AS productAccessory
        ON productAccessory.Id = accessoryGroup.IdProductAccessory
       AND productAccessory.IdProduct = accessoryGroup.IdProduct
    INNER JOIN Store.Accessory AS accessory
        ON accessory.Id = productAccessory.IdAccessory
    INNER JOIN Store.PriceVersion AS price
        ON price.IdProduct = accessoryGroup.IdProduct
       AND price.IdSalesPlan = accessoryGroup.IdSalesPlan
       AND price.IdAccessory = productAccessory.IdAccessory
       AND price.IdVariant IS NULL
       AND price.IsActive = 1
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
    ComponentType,
    AccessoryRequirement,
    ComponentId,
    TreePathNames,
    AccessoryName,
    BaseAmount,
    DiscountPercent,
    CONVERT(DECIMAL(19, 0), BaseAmount - FinalUnitAmount) AS PriceDifferencePerUnit,
    FinalUnitAmount,
    CONVERT(DECIMAL(19, 0), FinalUnitAmount * Quantity) AS LineTotal
FROM CartComponents
ORDER BY
    SortCartItemId,
    CASE ComponentType
        WHEN N'کالا' THEN 1
        WHEN N'تنوع' THEN 2
        ELSE 3
    END,
    AccessoryRequirement,
    ComponentId;
