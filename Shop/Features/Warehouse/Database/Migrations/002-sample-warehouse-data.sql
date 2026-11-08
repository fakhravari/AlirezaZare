SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'002-sample-warehouse-data')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    DECLARE @IdCompany BIGINT;
    DECLARE @IdUnit BIGINT;
    DECLARE @IdDocumentType BIGINT;
    DECLARE @IdSupplier BIGINT;

    INSERT INTO [Warehouse].[Company] ([Code], [Name])
    SELECT N'SHOP-DEMO', N'شرکت نمونه فروشگاه'
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[Company] WHERE [Code] = N'SHOP-DEMO');
    SELECT @IdCompany = [Id] FROM [Warehouse].[Company] WHERE [Code] = N'SHOP-DEMO';

    INSERT INTO [Warehouse].[SalesPlanReference] ([StoreSalesPlanKey], [DisplayName], [IsRegular], [IsActive], [IdStoreSalesPlan])
    SELECT sp.[SalesPlanKey], sp.[Name], sp.[IsDefault], sp.[IsEnabled], sp.[Id]
    FROM [Store].[SalesPlan] sp
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[SalesPlanReference] r WHERE r.[StoreSalesPlanKey] = sp.[SalesPlanKey]);

    INSERT INTO [Warehouse].[Warehouse] ([IdCompany], [IdOwnerSalesPlanReference], [Code], [Name], [IsSalesWarehouse], [IsActive])
    SELECT @IdCompany, r.[Id], N'SALES-MAIN', N'انبار فروش اصلی', 1, 1
    FROM [Warehouse].[SalesPlanReference] r
    WHERE r.[StoreSalesPlanKey] = N'regular'
      AND NOT EXISTS (SELECT 1 FROM [Warehouse].[Warehouse] WHERE [IdCompany] = @IdCompany AND [Code] = N'SALES-MAIN');

    INSERT INTO [Warehouse].[Warehouse] ([IdCompany], [IdOwnerSalesPlanReference], [Code], [Name], [IsSalesWarehouse], [IsActive])
    SELECT @IdCompany, NULL, N'RESERVE', N'انبار ذخیره مرکزی', 0, 1
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[Warehouse] WHERE [IdCompany] = @IdCompany AND [Code] = N'RESERVE');

    INSERT INTO [Warehouse].[SiteWarehouseSetting] ([Id], [IdWarehouse])
    SELECT 1, w.[Id] FROM [Warehouse].[Warehouse] w
    WHERE w.[IdCompany] = @IdCompany AND w.[Code] = N'SALES-MAIN'
      AND NOT EXISTS (SELECT 1 FROM [Warehouse].[SiteWarehouseSetting] WHERE [Id] = 1);

    INSERT INTO [Warehouse].[Unit] ([Code], [Name])
    SELECT N'EA', N'عدد' WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[Unit] WHERE [Code] = N'EA');
    SELECT @IdUnit = [Id] FROM [Warehouse].[Unit] WHERE [Code] = N'EA';

    INSERT INTO [Warehouse].[Product] ([IdBaseUnit], [StoreProductKey], [Name], [IsActive], [IdStoreProduct])
    SELECT @IdUnit, p.[StoreProductKey], p.[Name], p.[IsActive], p.[Id]
    FROM [Store].[Product] p
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[Product] wp WHERE wp.[StoreProductKey] = p.[StoreProductKey]);

    INSERT INTO [Warehouse].[ProductUnit] ([IdProduct], [IdUnit], [ConversionFactor], [IsBaseUnit], [IsActive])
    SELECT p.[Id], @IdUnit, 1, 1, 1
    FROM [Warehouse].[Product] p
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[ProductUnit] pu WHERE pu.[IdProduct] = p.[Id] AND pu.[IdUnit] = @IdUnit);

    INSERT INTO [Warehouse].[InventoryItem] ([IdProduct], [IdProductVariant], [IsActive])
    SELECT p.[Id], NULL, 1
    FROM [Warehouse].[Product] p
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[InventoryItem] i WHERE i.[IdProduct] = p.[Id] AND i.[IdProductVariant] IS NULL);

    INSERT INTO [Warehouse].[Party] ([PartyType], [ExternalPartyKey], [Name], [IsActive])
    SELECT N'Organization', N'SAMPLE-SUPPLIER', N'تأمین‌کننده نمونه', 1
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[Party] WHERE [ExternalPartyKey] = N'SAMPLE-SUPPLIER');
    SELECT @IdSupplier = [Id] FROM [Warehouse].[Party] WHERE [ExternalPartyKey] = N'SAMPLE-SUPPLIER';

    INSERT INTO [Warehouse].[DocumentType] ([Code], [Name], [Direction], [AffectsStock], [IsActive])
    SELECT N'OPENING-STOCK', N'رسید موجودی اولیه نمونه', 'R', 1, 1
    WHERE NOT EXISTS (SELECT 1 FROM [Warehouse].[DocumentType] WHERE [Code] = N'OPENING-STOCK');
    SELECT @IdDocumentType = [Id] FROM [Warehouse].[DocumentType] WHERE [Code] = N'OPENING-STOCK';

    DECLARE @Documents TABLE ([Id] BIGINT NOT NULL, [IdWarehouse] BIGINT NOT NULL, [IdSalesPlanReference] BIGINT NOT NULL);
    INSERT INTO [Warehouse].[WarehouseDocument]
        ([IdWarehouse], [IdDocumentType], [IdParty], [IdSalesPlanReference], [DocumentDate], [Status], [Reason], [CreatedByUserKey])
    OUTPUT inserted.[Id], inserted.[IdWarehouse], inserted.[IdSalesPlanReference]
        INTO @Documents ([Id], [IdWarehouse], [IdSalesPlanReference])
    SELECT w.[Id], @IdDocumentType, @IdSupplier, r.[Id], CONVERT(date, SYSUTCDATETIME()), 'F', N'رسید نمونه برای نمایش اتصال انبار و فروشگاه', N'system:sample-data'
    FROM [Warehouse].[Warehouse] w
    CROSS JOIN [Warehouse].[SalesPlanReference] r
    WHERE w.[IdCompany] = @IdCompany AND w.[Code] IN (N'SALES-MAIN', N'RESERVE') AND r.[IsActive] = 1
      AND NOT EXISTS
      (
          SELECT 1 FROM [Warehouse].[WarehouseDocument] d
          WHERE d.[IdWarehouse] = w.[Id] AND d.[IdSalesPlanReference] = r.[Id]
            AND d.[Reason] = N'رسید نمونه برای نمایش اتصال انبار و فروشگاه'
      );

    INSERT INTO [Warehouse].[WarehouseDocumentLine]
        ([IdDocument], [IdInventoryItem], [IdUnit], [Quantity], [ConversionFactor], [BaseQuantity], [ProductNameSnapshot], [VariantNameSnapshot], [UnitNameSnapshot])
    SELECT d.[Id], i.[Id], @IdUnit,
           CASE WHEN w.[Code] = N'SALES-MAIN' THEN 12 ELSE 30 END,
           1,
           CASE WHEN w.[Code] = N'SALES-MAIN' THEN 12 ELSE 30 END,
           p.[Name], NULL, N'عدد'
    FROM @Documents d
    INNER JOIN [Warehouse].[Warehouse] w ON w.[Id] = d.[IdWarehouse]
    CROSS JOIN [Warehouse].[InventoryItem] i
    INNER JOIN [Warehouse].[Product] p ON p.[Id] = i.[IdProduct]
    WHERE i.[IdProductVariant] IS NULL
      AND NOT EXISTS (SELECT 1 FROM [Warehouse].[WarehouseDocumentLine] l WHERE l.[IdDocument] = d.[Id] AND l.[IdInventoryItem] = i.[Id]);

    DECLARE @IdDocument BIGINT;
    DECLARE DocumentCursor CURSOR LOCAL FAST_FORWARD FOR SELECT [Id] FROM @Documents;
    OPEN DocumentCursor;
    FETCH NEXT FROM DocumentCursor INTO @IdDocument;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC [Warehouse].[RebuildDocumentStock] @IdDocument = @IdDocument;
        FETCH NEXT FROM DocumentCursor INTO @IdDocument;
    END;
    CLOSE DocumentCursor;
    DEALLOCATE DocumentCursor;

    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'002-sample-warehouse-data');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
