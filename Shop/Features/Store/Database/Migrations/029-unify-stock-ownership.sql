SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'029-unify-stock-ownership')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM [Store].[SalesPlan] p LEFT JOIN [Warehouse].[SalesPlanReference] r ON r.[IdStoreSalesPlan]=p.[Id] WHERE r.[Id] IS NULL)
       OR EXISTS (SELECT 1 FROM [Warehouse].[SalesPlanReference] r LEFT JOIN [Store].[SalesPlan] p ON p.[Id]=r.[IdStoreSalesPlan] WHERE p.[Id] IS NULL)
        THROW 51101, 'Every existing sales plan must have one warehouse reference before migration.', 1;

    CREATE TABLE [Warehouse].[StockOwnerType]
    (
        [Id] TINYINT NOT NULL CONSTRAINT [PK_Warehouse_StockOwnerType] PRIMARY KEY,
        [TypeKey] NVARCHAR(32) NOT NULL CONSTRAINT [UQ_Warehouse_StockOwnerType_Key] UNIQUE,
        [Name] NVARCHAR(100) NOT NULL
    );
    INSERT INTO [Warehouse].[StockOwnerType] ([Id], [TypeKey], [Name]) VALUES
        (1, N'sales-plan', N'طرح فروش'), (2, N'owner', N'مالک');

    ALTER TABLE [Warehouse].[SalesPlanReference] ADD
        [IdStockOwnerType] TINYINT NOT NULL CONSTRAINT [DF_Warehouse_SalesPlanReference_OwnerType] DEFAULT (1),
        [Code] NVARCHAR(64) NULL,
        [PlanType] NVARCHAR(30) NULL,
        [StartsAtUtc] DATETIMEOFFSET(3) NULL,
        [EndsAtUtc] DATETIMEOFFSET(3) NULL,
        [Label] NVARCHAR(100) NULL,
        [ShowCountdown] BIT NOT NULL CONSTRAINT [DF_Warehouse_SalesPlanReference_Countdown] DEFAULT (0),
        [SalesPlanKey] AS ([StoreSalesPlanKey]),
        [Name] AS ([DisplayName]),
        [IsDefault] AS ([IsRegular]),
        [IsEnabled] AS ([IsActive]);
    EXEC(N'UPDATE r SET r.[Code]=p.[Code], r.[PlanType]=p.[PlanType], r.[StartsAtUtc]=p.[StartsAtUtc], r.[EndsAtUtc]=p.[EndsAtUtc], r.[Label]=p.[Label], r.[ShowCountdown]=p.[ShowCountdown]
           FROM [Warehouse].[SalesPlanReference] r JOIN [Store].[SalesPlan] p ON p.[Id]=r.[IdStoreSalesPlan];
           ALTER TABLE [Warehouse].[SalesPlanReference] ALTER COLUMN [Code] NVARCHAR(64) NOT NULL;
           ALTER TABLE [Warehouse].[SalesPlanReference] ALTER COLUMN [PlanType] NVARCHAR(30) NOT NULL;
           ALTER TABLE [Warehouse].[SalesPlanReference] ADD CONSTRAINT [FK_Warehouse_SalesPlanReference_OwnerType] FOREIGN KEY ([IdStockOwnerType]) REFERENCES [Warehouse].[StockOwnerType]([Id]);');

    SELECT p.[Id] AS [OldId], r.[Id] AS [NewId] INTO #PlanMap
    FROM [Store].[SalesPlan] p JOIN [Warehouse].[SalesPlanReference] r ON r.[IdStoreSalesPlan]=p.[Id];

    ALTER TABLE [Account].[CartItem] DROP CONSTRAINT [FK_Account_CartItem_SalesPlan];
    ALTER TABLE [Account].[CustomerOrderItem] DROP CONSTRAINT [FK_Account_OrderItem_SalesPlan];
    ALTER TABLE [Store].[ProductSalesPlanOffer] DROP CONSTRAINT [FK_Store_ProductSalesPlanOffer_SalesPlan];
    ALTER TABLE [Store].[ProductVariantSalesPlanOffer] DROP CONSTRAINT [FK_Store_ProductVariantSalesPlanOffer_SalesPlan];
    -- Negative temporary identifiers avoid unique-key collisions if the two identity sequences differ.
    UPDATE t SET [IdSalesPlan]=-m.[NewId] FROM [Account].[CartItem] t JOIN #PlanMap m ON m.[OldId]=t.[IdSalesPlan];
    UPDATE t SET [IdSalesPlan]=-m.[NewId] FROM [Account].[CustomerOrderItem] t JOIN #PlanMap m ON m.[OldId]=t.[IdSalesPlan];
    UPDATE t SET [IdSalesPlan]=-m.[NewId] FROM [Store].[ProductSalesPlanOffer] t JOIN #PlanMap m ON m.[OldId]=t.[IdSalesPlan];
    UPDATE t SET [IdSalesPlan]=-m.[NewId] FROM [Store].[ProductVariantSalesPlanOffer] t JOIN #PlanMap m ON m.[OldId]=t.[IdSalesPlan];
    UPDATE [Account].[CartItem] SET [IdSalesPlan]=-[IdSalesPlan] WHERE [IdSalesPlan]<0;
    UPDATE [Account].[CustomerOrderItem] SET [IdSalesPlan]=-[IdSalesPlan] WHERE [IdSalesPlan]<0;
    UPDATE [Store].[ProductSalesPlanOffer] SET [IdSalesPlan]=-[IdSalesPlan] WHERE [IdSalesPlan]<0;
    UPDATE [Store].[ProductVariantSalesPlanOffer] SET [IdSalesPlan]=-[IdSalesPlan] WHERE [IdSalesPlan]<0;
    ALTER TABLE [Account].[CartItem] WITH CHECK ADD CONSTRAINT [FK_Account_CartItem_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Warehouse].[SalesPlanReference]([Id]);
    ALTER TABLE [Account].[CustomerOrderItem] WITH CHECK ADD CONSTRAINT [FK_Account_OrderItem_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Warehouse].[SalesPlanReference]([Id]);
    ALTER TABLE [Store].[ProductSalesPlanOffer] WITH CHECK ADD CONSTRAINT [FK_Store_ProductSalesPlanOffer_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Warehouse].[SalesPlanReference]([Id]);
    ALTER TABLE [Store].[ProductVariantSalesPlanOffer] WITH CHECK ADD CONSTRAINT [FK_Store_ProductVariantSalesPlanOffer_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Warehouse].[SalesPlanReference]([Id]);
    ALTER TABLE [Warehouse].[SalesPlanReference] DROP CONSTRAINT [FK_Warehouse_SalesPlanReference_StoreSalesPlan];
    DECLARE @indexName sysname;
    SELECT @indexName=i.name FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
    JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
    WHERE i.object_id=OBJECT_ID(N'[Warehouse].[SalesPlanReference]') AND c.name=N'IdStoreSalesPlan' AND i.is_primary_key=0 AND i.is_unique_constraint=0;
    IF @indexName IS NOT NULL
    BEGIN
        DECLARE @dropIndex NVARCHAR(MAX) = N'DROP INDEX '+QUOTENAME(@indexName)+N' ON [Warehouse].[SalesPlanReference]';
        EXEC sp_executesql @dropIndex;
    END;
    ALTER TABLE [Warehouse].[SalesPlanReference] DROP COLUMN [IdStoreSalesPlan];
    DROP TABLE [Store].[SalesPlan];

    EXEC(N'
        IF EXISTS (SELECT 1 FROM [Warehouse].[Warehouse] w JOIN [Warehouse].[SalesPlanReference] r ON r.[StoreSalesPlanKey]=CONCAT(N''warehouse-'',w.[Id]) WHERE w.[IsSalesWarehouse]=0 AND w.[IdOwnerSalesPlanReference] IS NULL)
            THROW 51102, ''Generated warehouse owner key already exists.'', 1;
        INSERT INTO [Warehouse].[SalesPlanReference] ([StoreSalesPlanKey],[DisplayName],[IsRegular],[IsActive],[IdStockOwnerType],[Code],[PlanType])
        SELECT CONCAT(N''warehouse-'',w.[Id]), w.[Name], 0, w.[IsActive], 2, CONCAT(N''warehouse-'',w.[Id]), N''owner''
        FROM [Warehouse].[Warehouse] w WHERE w.[IsSalesWarehouse]=0 AND w.[IdOwnerSalesPlanReference] IS NULL;
        UPDATE w SET [IdOwnerSalesPlanReference]=r.[Id] FROM [Warehouse].[Warehouse] w
        JOIN [Warehouse].[SalesPlanReference] r ON r.[StoreSalesPlanKey]=CONCAT(N''warehouse-'',w.[Id])
        WHERE w.[IsSalesWarehouse]=0 AND w.[IdOwnerSalesPlanReference] IS NULL;');

    CREATE TABLE [Warehouse].[WarehouseStockOwner]
    (
        [IdWarehouse] BIGINT NOT NULL,
        [IdStockOwner] BIGINT NOT NULL,
        CONSTRAINT [PK_Warehouse_WarehouseStockOwner] PRIMARY KEY ([IdWarehouse], [IdStockOwner]),
        CONSTRAINT [FK_Warehouse_WarehouseStockOwner_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse]([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseStockOwner_Owner] FOREIGN KEY ([IdStockOwner]) REFERENCES [Warehouse].[SalesPlanReference]([Id])
    );
    INSERT INTO [Warehouse].[WarehouseStockOwner] ([IdWarehouse], [IdStockOwner])
    SELECT [IdWarehouse], [IdSalesPlanReference] FROM [Warehouse].[WarehouseDocument]
    UNION SELECT [IdWarehouse], [IdSalesPlanReference] FROM [Warehouse].[StockBalance]
    UNION SELECT [Id], [IdOwnerSalesPlanReference] FROM [Warehouse].[Warehouse] WHERE [IdOwnerSalesPlanReference] IS NOT NULL;
    INSERT INTO [Warehouse].[WarehouseStockOwner] ([IdWarehouse], [IdStockOwner])
    SELECT w.[Id], r.[Id] FROM [Warehouse].[Warehouse] w CROSS JOIN [Warehouse].[SalesPlanReference] r
    WHERE r.[IsRegular]=1 AND NOT EXISTS (SELECT 1 FROM [Warehouse].[WarehouseStockOwner] x WHERE x.[IdWarehouse]=w.[Id]);

    IF OBJECT_ID(N'[Admin].[Permission]', N'U') IS NOT NULL
        EXEC(N'IF NOT EXISTS (SELECT 1 FROM [Admin].[Permission] WHERE [PermissionKey]=N''warehouse.definitions.stock-owners'')
            INSERT INTO [Admin].[Permission] ([PermissionKey],[Name],[ParentPermissionKey],[Area],[Controller],[Action],[FeatureKey],[SortOrder])
            VALUES (N''warehouse.definitions.stock-owners'',N''صفحهٔ مالک کالا'',N''warehouse.definitions'',N''Warehouse'',N''Features'',N''Index'',N''stock-owners'',86);');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'029-unify-stock-ownership');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
