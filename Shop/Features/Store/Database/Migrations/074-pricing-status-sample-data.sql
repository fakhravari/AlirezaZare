SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'074-pricing-status-sample-data')
    BEGIN
        DECLARE @IdProduct BIGINT = (
            SELECT TOP (1) [Id] FROM [Warehouse].[Product]
            WHERE [IsCatalogActive] = 1 AND [IsStoreEnabled] = 1 AND [IsWarehouseEnabled] = 1
            ORDER BY [Id]);
        DECLARE @IdPlan BIGINT = (
            SELECT TOP (1) [Id] FROM [Warehouse].[SalesPlanReference]
            WHERE [IdStockOwnerType] = 1 AND [IsActive] = 1
            ORDER BY [IsRegular] DESC, [Id]);

        IF @IdProduct IS NULL OR @IdPlan IS NULL
            THROW 51074, 'Active sample product and sales plan are required for pricing status fixtures.', 1;
        BEGIN
            /* Price-bearing product options. An inactive PriceVersion is retained as history;
               only its active sibling is consumed as the current quote price. */
            IF NOT EXISTS (SELECT 1 FROM [Store].[ProductPurchaseOption] WHERE [IdProduct] = @IdProduct AND [OptionKey] = N'sample-finish')
                INSERT [Store].[ProductPurchaseOption] ([IdProduct],[OptionKey],[Label],[SortOrder])
                VALUES (@IdProduct,N'sample-finish',N'پوشش نمونه برای آزمون قیمت',900);

            DECLARE @IdOption BIGINT = (SELECT [Id] FROM [Store].[ProductPurchaseOption] WHERE [IdProduct]=@IdProduct AND [OptionKey]=N'sample-finish');
            IF NOT EXISTS (SELECT 1 FROM [Store].[ProductPurchaseOptionValue] WHERE [IdProductPurchaseOption]=@IdOption AND [ValueKey]=N'sample-standard')
                INSERT [Store].[ProductPurchaseOptionValue] ([IdProductPurchaseOption],[ValueKey],[Label],[PriceDelta],[SortOrder])
                VALUES (@IdOption,N'sample-standard',N'استاندارد ـ فعال',0,1);
            IF NOT EXISTS (SELECT 1 FROM [Store].[ProductPurchaseOptionValue] WHERE [IdProductPurchaseOption]=@IdOption AND [ValueKey]=N'sample-premium')
                INSERT [Store].[ProductPurchaseOptionValue] ([IdProductPurchaseOption],[ValueKey],[Label],[PriceDelta],[SortOrder])
                VALUES (@IdOption,N'sample-premium',N'ویژه ـ فعال',0,2);

            DECLARE @IdActiveOptionValue BIGINT = (SELECT [Id] FROM [Store].[ProductPurchaseOptionValue] WHERE [IdProductPurchaseOption]=@IdOption AND [ValueKey]=N'sample-standard');
            DECLARE @IdPremiumOptionValue BIGINT = (SELECT [Id] FROM [Store].[ProductPurchaseOptionValue] WHERE [IdProductPurchaseOption]=@IdOption AND [ValueKey]=N'sample-premium');
            IF NOT EXISTS (SELECT 1 FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'option/',@IdActiveOptionValue) AND [IsActive]=1)
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'option/',@IdActiveOptionValue))
                    INSERT [Store].[PriceVersion] ([ScopeKey],[IdProduct],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
                    VALUES (CONCAT(N'option/',@IdActiveOptionValue),@IdProduct,N'option',@IdActiveOptionValue,200000,0,0,N'Sample previous option price');
                DECLARE @IdPreviousOptionPrice BIGINT = (SELECT TOP (1) [Id] FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'option/',@IdActiveOptionValue) AND [IsActive]=0 ORDER BY [Id] DESC);
                INSERT [Store].[PriceVersion] ([ScopeKey],[IdProduct],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
                VALUES (CONCAT(N'option/',@IdActiveOptionValue),@IdProduct,N'option',@IdActiveOptionValue,250000,0,1,N'Sample active option price');
                UPDATE [Store].[PriceVersion] SET [PreviousVersionId]=@IdPreviousOptionPrice WHERE [ScopeKey]=CONCAT(N'option/',@IdActiveOptionValue) AND [IsActive]=1;
            END;
            IF NOT EXISTS (SELECT 1 FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'option/',@IdPremiumOptionValue) AND [IsActive]=1)
                INSERT [Store].[PriceVersion] ([ScopeKey],[IdProduct],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
                VALUES (CONCAT(N'option/',@IdPremiumOptionValue),@IdProduct,N'option',@IdPremiumOptionValue,450000,0,1,N'Sample active premium option price');

            /* Active and inactive accessories exercise availability filtering. */
            IF NOT EXISTS (SELECT 1 FROM [Store].[Accessory] WHERE [AccessoryKey]=N'sample-status-active')
                INSERT [Store].[Accessory] ([AccessoryKey],[Name],[Description],[IsActive]) VALUES (N'sample-status-active',N'متعلق آزمایشی فعال',N'برای آزمون قیمت متعلق در پیشنهاد فروش.',1);
            IF NOT EXISTS (SELECT 1 FROM [Store].[Accessory] WHERE [AccessoryKey]=N'sample-status-inactive')
                INSERT [Store].[Accessory] ([AccessoryKey],[Name],[Description],[IsActive]) VALUES (N'sample-status-inactive',N'متعلق آزمایشی غیرفعال',N'برای آزمون پنهان‌سازی متعلق غیرفعال.',0);

            DECLARE @IdAccessory BIGINT = (SELECT [Id] FROM [Store].[Accessory] WHERE [AccessoryKey]=N'sample-status-active');
            IF NOT EXISTS (SELECT 1 FROM [Store].[ProductAccessory] WHERE [IdProduct]=@IdProduct AND [IdAccessory]=@IdAccessory)
                INSERT [Store].[ProductAccessory] ([IdProduct],[IdAccessory],[IsRequired],[SortOrder],[PriceAmount]) VALUES (@IdProduct,@IdAccessory,0,900,175000);
            UPDATE [Store].[ProductAccessory] SET [PriceAmount]=175000 WHERE [IdProduct]=@IdProduct AND [IdAccessory]=@IdAccessory;
            DECLARE @IdOffer BIGINT = (SELECT [Id] FROM [Store].[ProductSalesPlanOffer] WHERE [IdProduct]=@IdProduct AND [IdSalesPlan]=@IdPlan);
            IF @IdOffer IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Store].[ProductSalesPlanOfferAccessory] WHERE [IdProductSalesPlanOffer]=@IdOffer AND [IdAccessory]=@IdAccessory)
                INSERT [Store].[ProductSalesPlanOfferAccessory] ([IdProductSalesPlanOffer],[IdAccessory],[PriceAmount],[IsIncludedByDefault],[IsAllowed])
                VALUES (@IdOffer,@IdAccessory,175000,0,1);

            /* Variant-level active/inactive state and offer visibility. */
            IF NOT EXISTS (SELECT 1 FROM [Warehouse].[ProductVariant] WHERE [IdProduct]=@IdProduct AND [Code]=N'SAMPLE-STATUS-ACTIVE')
                INSERT [Warehouse].[ProductVariant] ([IdProduct],[Code],[Name],[AttributesJson],[IsActive],[IsDefault])
                VALUES (@IdProduct,N'SAMPLE-STATUS-ACTIVE',N'تنوع آزمایشی فعال',N'{"sampleStatus":"active"}',1,0);
            IF NOT EXISTS (SELECT 1 FROM [Warehouse].[ProductVariant] WHERE [IdProduct]=@IdProduct AND [Code]=N'SAMPLE-STATUS-INACTIVE')
                INSERT [Warehouse].[ProductVariant] ([IdProduct],[Code],[Name],[AttributesJson],[IsActive],[IsDefault])
                VALUES (@IdProduct,N'SAMPLE-STATUS-INACTIVE',N'تنوع آزمایشی غیرفعال',N'{"sampleStatus":"inactive"}',0,0);
            DECLARE @IdActiveVariant BIGINT = (SELECT [Id] FROM [Warehouse].[ProductVariant] WHERE [IdProduct]=@IdProduct AND [Code]=N'SAMPLE-STATUS-ACTIVE');
            DECLARE @IdInactiveVariant BIGINT = (SELECT [Id] FROM [Warehouse].[ProductVariant] WHERE [IdProduct]=@IdProduct AND [Code]=N'SAMPLE-STATUS-INACTIVE');
            IF NOT EXISTS (SELECT 1 FROM [Warehouse].[InventoryItem] WHERE [IdProduct]=@IdProduct AND [IdProductVariant]=@IdActiveVariant)
                INSERT [Warehouse].[InventoryItem] ([IdProduct],[IdProductVariant],[IsActive]) VALUES (@IdProduct,@IdActiveVariant,1);
            IF NOT EXISTS (SELECT 1 FROM [Store].[ProductVariantSalesPlanOffer] WHERE [IdProductVariant]=@IdActiveVariant AND [IdSalesPlan]=@IdPlan)
                INSERT [Store].[ProductVariantSalesPlanOffer] ([IdProductVariant],[IdSalesPlan],[PriceAmount],[DiscountPercent],[IsEnabled]) VALUES (@IdActiveVariant,@IdPlan,1250000,0,1);
            IF NOT EXISTS (SELECT 1 FROM [Store].[ProductVariantSalesPlanOffer] WHERE [IdProductVariant]=@IdInactiveVariant AND [IdSalesPlan]=@IdPlan)
                INSERT [Store].[ProductVariantSalesPlanOffer] ([IdProductVariant],[IdSalesPlan],[PriceAmount],[DiscountPercent],[IsEnabled]) VALUES (@IdInactiveVariant,@IdPlan,990000,0,0);
            IF NOT EXISTS (SELECT 1 FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'variant/',@IdActiveVariant,N'/plan/',@IdPlan) AND [IsActive]=1)
                INSERT [Store].[PriceVersion] ([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
                VALUES (CONCAT(N'variant/',@IdActiveVariant,N'/plan/',@IdPlan),@IdProduct,@IdPlan,N'variant',@IdActiveVariant,1250000,0,1,N'Sample active variant price');
            IF NOT EXISTS (SELECT 1 FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'variant/',@IdInactiveVariant,N'/plan/',@IdPlan))
                INSERT [Store].[PriceVersion] ([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
                VALUES (CONCAT(N'variant/',@IdInactiveVariant,N'/plan/',@IdPlan),@IdProduct,@IdPlan,N'variant',@IdInactiveVariant,990000,0,0,N'Sample inactive variant price history');
            IF NOT EXISTS (SELECT 1 FROM [Store].[PriceVersion] WHERE [ScopeKey]=CONCAT(N'accessory/',@IdProduct,N'/',@IdAccessory,N'/plan/',@IdPlan) AND [IsActive]=1)
                INSERT [Store].[PriceVersion] ([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
                SELECT CONCAT(N'accessory/',@IdProduct,N'/',@IdAccessory,N'/plan/',@IdPlan),@IdProduct,@IdPlan,N'accessory',@IdAccessory,pa.[PriceAmount],0,1,N'Sample active accessory price'
                FROM [Store].[ProductAccessory] pa WHERE pa.[IdProduct]=@IdProduct AND pa.[IdAccessory]=@IdAccessory;

            /* Coupon validation covers enabled and disabled records without touching user coupons. */
            IF NOT EXISTS (SELECT 1 FROM [Store].[Coupon] WHERE [CouponKey]=N'sample-status-enabled')
                INSERT [Store].[Coupon] ([CouponKey],[Code],[DiscountPercent],[UsageLimit],[Enabled],[ExpiresOn]) VALUES (N'sample-status-enabled',N'TESTACTIVE15',15,100,1,CONVERT(date,DATEADD(year,1,SYSUTCDATETIME())));
            IF NOT EXISTS (SELECT 1 FROM [Store].[Coupon] WHERE [CouponKey]=N'sample-status-disabled')
                INSERT [Store].[Coupon] ([CouponKey],[Code],[DiscountPercent],[UsageLimit],[Enabled],[ExpiresOn]) VALUES (N'sample-status-disabled',N'TESTINACTIVE15',15,100,0,CONVERT(date,DATEADD(year,1,SYSUTCDATETIME())));
        END;

        INSERT [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'074-pricing-status-sample-data');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
