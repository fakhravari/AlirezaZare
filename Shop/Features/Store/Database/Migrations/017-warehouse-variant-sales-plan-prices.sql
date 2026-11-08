SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'017-warehouse-variant-sales-plan-prices')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Warehouse].[ProductVariant]', N'U') IS NULL OR OBJECT_ID(N'[Store].[SalesPlan]', N'U') IS NULL
        THROW 51042, 'Warehouse variants and store sales plans must be installed before variant price offers.', 1;

    IF OBJECT_ID(N'[Store].[ProductVariantSalesPlanOffer]', N'U') IS NULL
    BEGIN
    CREATE TABLE [Store].[ProductVariantSalesPlanOffer]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProductVariant] BIGINT NOT NULL,
        [IdSalesPlan] BIGINT NOT NULL,
        [PriceAmount] DECIMAL(19,0) NOT NULL,
        [DiscountPercent] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_Store_ProductVariantSalesPlanOffer_DiscountPercent] DEFAULT (0),
        [IsEnabled] BIT NOT NULL CONSTRAINT [DF_Store_ProductVariantSalesPlanOffer_IsEnabled] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_ProductVariantSalesPlanOffer_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_ProductVariantSalesPlanOffer] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductVariantSalesPlanOffer_WarehouseVariant] FOREIGN KEY ([IdProductVariant]) REFERENCES [Warehouse].[ProductVariant] ([Id]),
        CONSTRAINT [FK_Store_ProductVariantSalesPlanOffer_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Store].[SalesPlan] ([Id]),
        CONSTRAINT [UQ_Store_ProductVariantSalesPlanOffer_Variant_Plan] UNIQUE ([IdProductVariant], [IdSalesPlan]),
        CONSTRAINT [CK_Store_ProductVariantSalesPlanOffer_Price_NonNegative] CHECK ([PriceAmount] >= 0),
        CONSTRAINT [CK_Store_ProductVariantSalesPlanOffer_Discount_Range] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] < 100)
    );
    END;

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'017-warehouse-variant-sales-plan-prices');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
