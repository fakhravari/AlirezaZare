SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'[Store].[PriceVersion]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Store].[PriceVersion](
            [Id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Store_PriceVersion] PRIMARY KEY,
            [ScopeKey] NVARCHAR(300) NOT NULL,
            [IdProduct] BIGINT NOT NULL,
            [IdSalesPlan] BIGINT NULL,
            [PriceKind] NVARCHAR(32) NOT NULL,
            [TargetId] BIGINT NULL,
            [PriceAmount] DECIMAL(19,0) NOT NULL,
            [DiscountPercent] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_Store_PriceVersion_Discount] DEFAULT(0),
            [IsActive] BIT NOT NULL,
            [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_PriceVersion_Created] DEFAULT SYSUTCDATETIME(),
            [ChangedBy] NVARCHAR(256) NULL,
            [ChangeReason] NVARCHAR(300) NULL,
            [PreviousVersionId] BIGINT NULL,
            CONSTRAINT [FK_Store_PriceVersion_Product] FOREIGN KEY([IdProduct]) REFERENCES [Warehouse].[Product]([Id]),
            CONSTRAINT [FK_Store_PriceVersion_SalesPlan] FOREIGN KEY([IdSalesPlan]) REFERENCES [Warehouse].[SalesPlanReference]([Id]),
            CONSTRAINT [FK_Store_PriceVersion_Previous] FOREIGN KEY([PreviousVersionId]) REFERENCES [Store].[PriceVersion]([Id])
        );
        CREATE UNIQUE INDEX [UX_Store_PriceVersion_ActiveScope] ON [Store].[PriceVersion]([ScopeKey]) WHERE [IsActive]=1;
        CREATE INDEX [IX_Store_PriceVersion_Lookup] ON [Store].[PriceVersion]([IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[Id]);
    END;
    IF OBJECT_ID(N'[Store].[OrderItemPriceReference]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Store].[OrderItemPriceReference](
            [IdOrderItem] BIGINT NOT NULL,
            [IdPriceVersion] BIGINT NOT NULL,
            CONSTRAINT [PK_Store_OrderItemPriceReference] PRIMARY KEY([IdOrderItem],[IdPriceVersion]),
            CONSTRAINT [FK_Store_OrderItemPriceReference_OrderItem] FOREIGN KEY([IdOrderItem]) REFERENCES [Store].[OrderItem]([Id]) ON DELETE CASCADE,
            CONSTRAINT [FK_Store_OrderItemPriceReference_PriceVersion] FOREIGN KEY([IdPriceVersion]) REFERENCES [Store].[PriceVersion]([Id])
        );
    END;

    /* Initial catalog snapshot: this records the current value at installation time. */
    INSERT [Store].[PriceVersion]([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
    SELECT CONCAT(N'product/',p.Id,N'/plan/',sp.Id),p.Id,sp.Id,N'product',
           COALESCE(o.PriceAmount,CASE WHEN COALESCE(p.DiscountPercent,0)<100 THEN ROUND(COALESCE(p.PriceAmount,0)*100.0/(100.0-COALESCE(p.DiscountPercent,0)),0) ELSE COALESCE(p.PriceAmount,0) END),
           COALESCE(o.DiscountPercent,p.DiscountPercent,0),1,N'Initial snapshot from current catalog'
    FROM [Warehouse].[Product] p CROSS JOIN [Warehouse].[SalesPlanReference] sp
    LEFT JOIN [Store].[ProductSalesPlanOffer] o ON o.IdProduct=p.Id AND o.IdSalesPlan=sp.Id
    WHERE (sp.IsRegular=1 OR o.Id IS NOT NULL)
      AND NOT EXISTS(SELECT 1 FROM [Store].[PriceVersion] v WHERE v.ScopeKey=CONCAT(N'product/',p.Id,N'/plan/',sp.Id));

    INSERT [Store].[PriceVersion]([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
    SELECT CONCAT(N'variant/',v.Id,N'/plan/',o.IdSalesPlan),v.IdProduct,o.IdSalesPlan,N'variant',v.Id,o.PriceAmount,o.DiscountPercent,1,N'Initial snapshot from current catalog'
    FROM [Store].[ProductVariantSalesPlanOffer] o JOIN [Warehouse].[ProductVariant] v ON v.Id=o.IdProductVariant
    WHERE NOT EXISTS(SELECT 1 FROM [Store].[PriceVersion] h WHERE h.ScopeKey=CONCAT(N'variant/',v.Id,N'/plan/',o.IdSalesPlan));

    INSERT [Store].[PriceVersion]([ScopeKey],[IdProduct],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
    SELECT CONCAT(N'option/',val.Id),opt.IdProduct,N'option',val.Id,ROUND(val.PriceDelta*1000000,0),0,1,N'Initial snapshot from current catalog'
    FROM [Store].[ProductPurchaseOptionValue] val JOIN [Store].[ProductPurchaseOption] opt ON opt.Id=val.IdProductPurchaseOption
    WHERE NOT EXISTS(SELECT 1 FROM [Store].[PriceVersion] h WHERE h.ScopeKey=CONCAT(N'option/',val.Id));

    INSERT [Store].[PriceVersion]([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
    SELECT CONCAT(N'accessory/',pa.IdProduct,N'/',pa.IdAccessory,N'/plan/',sp.Id),pa.IdProduct,sp.Id,N'accessory',pa.IdAccessory,pa.PriceAmount,0,1,N'Initial snapshot from current catalog'
    FROM [Store].[ProductAccessory] pa CROSS JOIN [Warehouse].[SalesPlanReference] sp
    WHERE sp.IsRegular=1 AND NOT EXISTS(SELECT 1 FROM [Store].[PriceVersion] h WHERE h.ScopeKey=CONCAT(N'accessory/',pa.IdProduct,N'/',pa.IdAccessory,N'/plan/',sp.Id));
    INSERT [Store].[PriceVersion]([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
    SELECT CONCAT(N'accessory/',o.IdProduct,N'/',oa.IdAccessory,N'/plan/',o.IdSalesPlan),o.IdProduct,o.IdSalesPlan,N'accessory',oa.IdAccessory,oa.PriceAmount,0,1,N'Initial snapshot from current catalog'
    FROM [Store].[ProductSalesPlanOfferAccessory] oa JOIN [Store].[ProductSalesPlanOffer] o ON o.Id=oa.IdProductSalesPlanOffer
    WHERE oa.IsAllowed=1 AND NOT EXISTS(SELECT 1 FROM [Store].[PriceVersion] h WHERE h.ScopeKey=CONCAT(N'accessory/',o.IdProduct,N'/',oa.IdAccessory,N'/plan/',o.IdSalesPlan));

    /* Legacy orders get an immutable order snapshot; no catalog history is fabricated. */
    INSERT [Store].[PriceVersion]([ScopeKey],[IdProduct],[IdSalesPlan],[PriceKind],[TargetId],[PriceAmount],[DiscountPercent],[IsActive],[ChangeReason])
    SELECT CONCAT(N'order-snapshot/',i.Id),i.IdProduct,i.IdSalesPlan,N'order-snapshot',i.Id,i.UnitAmount,0,0,N'Legacy order line snapshot'
    FROM [Store].[OrderItem] i
    WHERE NOT EXISTS(SELECT 1 FROM [Store].[PriceVersion] v WHERE v.ScopeKey=CONCAT(N'order-snapshot/',i.Id));
    INSERT [Store].[OrderItemPriceReference]([IdOrderItem],[IdPriceVersion])
    SELECT i.Id,v.Id FROM [Store].[OrderItem] i
    JOIN [Store].[PriceVersion] v ON v.ScopeKey=CONCAT(N'order-snapshot/',i.Id)
    WHERE NOT EXISTS(SELECT 1 FROM [Store].[OrderItemPriceReference] r WHERE r.IdOrderItem=i.Id);

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NOT NULL
       AND NOT EXISTS(SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey]=N'061-price-version-history')
        INSERT [Store].[SchemaMigration]([MigrationKey]) VALUES(N'061-price-version-history');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
