SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'071-backfill-cart-item-price')
    BEGIN
        IF COL_LENGTH(N'Account.CartItem', N'UnitAmount') IS NULL
            THROW 51071, 'CartItem.UnitAmount must be installed before price backfill.', 1;

        ;WITH optionAmounts AS
        (
            SELECT selected.[IdCartItem], SUM(COALESCE(optionVersion.[PriceAmount], ROUND(value.[PriceDelta] * 1000000, 0))) AS [Amount]
            FROM [Account].[CartItemPurchaseOptionValue] AS selected
            JOIN [Store].[ProductPurchaseOptionValue] AS value ON value.[Id] = selected.[IdProductPurchaseOptionValue]
            OUTER APPLY
            (
                SELECT TOP (1) version.[PriceAmount]
                FROM [Store].[PriceVersion] AS version
                WHERE version.[IsActive] = 1 AND version.[ScopeKey] = CONCAT(N'option/', value.[Id])
                ORDER BY version.[Id] DESC
            ) AS optionVersion
            GROUP BY selected.[IdCartItem]
        ), accessoryAmounts AS
        (
            SELECT item.[Id] AS [IdCartItem], SUM(COALESCE(accessoryVersion.[PriceAmount],
                CASE WHEN planOffer.[Id] IS NOT NULL THEN offerAccessory.[PriceAmount] ELSE link.[PriceAmount] END, 0)) AS [Amount]
            FROM [Account].[CartItem] AS item
            JOIN [Store].[ProductAccessory] AS link ON link.[IdProduct] = item.[IdProduct]
            JOIN [Store].[Accessory] AS accessory ON accessory.[Id] = link.[IdAccessory] AND accessory.[IsActive] = 1
            LEFT JOIN [Account].[CartItemAccessory] AS selected ON selected.[IdCartItem] = item.[Id] AND selected.[IdProductAccessory] = link.[Id]
            LEFT JOIN [Store].[ProductSalesPlanOffer] AS planOffer ON planOffer.[IdProduct] = item.[IdProduct] AND planOffer.[IdSalesPlan] = item.[IdSalesPlan]
            LEFT JOIN [Store].[ProductSalesPlanOfferAccessory] AS offerAccessory
                ON offerAccessory.[IdProductSalesPlanOffer] = planOffer.[Id] AND offerAccessory.[IdAccessory] = link.[IdAccessory] AND offerAccessory.[IsAllowed] = 1
            OUTER APPLY
            (
                SELECT TOP (1) version.[PriceAmount]
                FROM [Store].[PriceVersion] AS version
                WHERE version.[IsActive] = 1 AND version.[ScopeKey] = CONCAT(N'accessory/', item.[IdProduct], N'/', link.[IdAccessory], N'/plan/', item.[IdSalesPlan])
                ORDER BY version.[Id] DESC
            ) AS accessoryVersion
            WHERE link.[IsRequired] = 1 OR selected.[Id] IS NOT NULL
            GROUP BY item.[Id]
        ), computed AS
        (
            SELECT item.[Id], CONVERT(DECIMAL(19,0), ROUND(
                COALESCE(variantVersion.[PriceAmount], variantOffer.[PriceAmount], productVersion.[PriceAmount], planOffer.[PriceAmount], product.[PriceAmount], 0)
                * (100 - COALESCE(variantVersion.[DiscountPercent], variantOffer.[DiscountPercent], productVersion.[DiscountPercent], planOffer.[DiscountPercent], product.[DiscountPercent], 0)) / 100.0
                + COALESCE(optionAmounts.[Amount], 0) + COALESCE(accessoryAmounts.[Amount], 0), 0)) AS [UnitAmount]
            FROM [Account].[CartItem] AS item
            JOIN [Warehouse].[Product] AS product ON product.[Id] = item.[IdProduct]
            LEFT JOIN [Store].[ProductSalesPlanOffer] AS planOffer ON planOffer.[IdProduct] = item.[IdProduct] AND planOffer.[IdSalesPlan] = item.[IdSalesPlan]
            LEFT JOIN [Store].[ProductVariantSalesPlanOffer] AS variantOffer ON variantOffer.[IdProductVariant] = item.[IdProductVariant] AND variantOffer.[IdSalesPlan] = item.[IdSalesPlan] AND variantOffer.[IsEnabled] = 1
            OUTER APPLY
            (
                SELECT TOP (1) version.[PriceAmount], version.[DiscountPercent]
                FROM [Store].[PriceVersion] AS version
                WHERE version.[IsActive] = 1 AND version.[ScopeKey] = CONCAT(N'product/', item.[IdProduct], N'/plan/', item.[IdSalesPlan])
                ORDER BY version.[Id] DESC
            ) AS productVersion
            OUTER APPLY
            (
                SELECT TOP (1) version.[PriceAmount], version.[DiscountPercent]
                FROM [Store].[PriceVersion] AS version
                WHERE version.[IsActive] = 1 AND version.[ScopeKey] = CONCAT(N'variant/', item.[IdProductVariant], N'/plan/', item.[IdSalesPlan])
                ORDER BY version.[Id] DESC
            ) AS variantVersion
            LEFT JOIN optionAmounts ON optionAmounts.[IdCartItem] = item.[Id]
            LEFT JOIN accessoryAmounts ON accessoryAmounts.[IdCartItem] = item.[Id]
        )
        UPDATE item SET [UnitAmount] = computed.[UnitAmount]
        FROM [Account].[CartItem] AS item JOIN computed ON computed.[Id] = item.[Id];

        INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'071-backfill-cart-item-price');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
