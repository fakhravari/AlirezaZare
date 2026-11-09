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

    IF SCHEMA_ID(N'Warehouse') IS NULL
        EXEC(N'CREATE SCHEMA [Warehouse]');

    IF OBJECT_ID(N'[Warehouse].[SchemaMigration]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Warehouse].[SchemaMigration]
        (
            [Id] BIGINT IDENTITY(1,1) NOT NULL,
            [MigrationKey] NVARCHAR(100) NOT NULL,
            [AppliedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_SchemaMigration_AppliedAtUtc] DEFAULT SYSUTCDATETIME(),
            CONSTRAINT [PK_Warehouse_SchemaMigration] PRIMARY KEY ([Id]),
            CONSTRAINT [UQ_Warehouse_SchemaMigration_MigrationKey] UNIQUE ([MigrationKey])
        );
    END;

    IF EXISTS (SELECT 1 FROM [Warehouse].[SchemaMigration] WHERE [MigrationKey] = N'001-initial-schema')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    IF OBJECT_ID(N'[Warehouse].[Company]', N'U') IS NOT NULL
       OR OBJECT_ID(N'[Warehouse].[Warehouse]', N'U') IS NOT NULL
       OR OBJECT_ID(N'[Warehouse].[WarehouseDocument]', N'U') IS NOT NULL
        THROW 51000, 'Warehouse schema has untracked objects; refusing to overwrite them.', 1;

    IF OBJECT_ID(N'[Warehouse].[DocumentNumberSequence]', N'SO') IS NULL
        EXEC(N'CREATE SEQUENCE [Warehouse].[DocumentNumberSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE');

    CREATE TABLE [Warehouse].[Company]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Company_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_Company_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_Company] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Warehouse_Company_Code] UNIQUE ([Code]),
        CONSTRAINT [CK_Warehouse_Company_Code_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
        CONSTRAINT [CK_Warehouse_Company_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Warehouse].[SalesPlanReference]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [StoreSalesPlanKey] NVARCHAR(128) NOT NULL,
        [DisplayName] NVARCHAR(200) NOT NULL,
        [IsRegular] BIT NOT NULL CONSTRAINT [DF_Warehouse_SalesPlanReference_IsRegular] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_SalesPlanReference_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_SalesPlanReference_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_SalesPlanReference] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Warehouse_SalesPlanReference_StoreSalesPlanKey] UNIQUE ([StoreSalesPlanKey]),
        CONSTRAINT [CK_Warehouse_SalesPlanReference_Key_NotEmpty] CHECK (LEN(LTRIM(RTRIM([StoreSalesPlanKey]))) > 0),
        CONSTRAINT [CK_Warehouse_SalesPlanReference_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([DisplayName]))) > 0)
    );

    CREATE UNIQUE INDEX [UX_Warehouse_SalesPlanReference_OneRegular]
        ON [Warehouse].[SalesPlanReference] ([IsRegular]) WHERE [IsRegular] = 1;

    CREATE TABLE [Warehouse].[Warehouse]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdCompany] BIGINT NOT NULL,
        [IdOwnerSalesPlanReference] BIGINT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [IsSalesWarehouse] BIT NOT NULL CONSTRAINT [DF_Warehouse_Warehouse_IsSalesWarehouse] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Warehouse_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_Warehouse_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_Warehouse] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_Warehouse_Company] FOREIGN KEY ([IdCompany]) REFERENCES [Warehouse].[Company] ([Id]),
        CONSTRAINT [FK_Warehouse_Warehouse_OwnerSalesPlan] FOREIGN KEY ([IdOwnerSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]),
        CONSTRAINT [UQ_Warehouse_Warehouse_Company_Code] UNIQUE ([IdCompany], [Code]),
        CONSTRAINT [CK_Warehouse_Warehouse_SalesOwnerRequired] CHECK ([IsSalesWarehouse] = 0 OR [IdOwnerSalesPlanReference] IS NOT NULL),
        CONSTRAINT [CK_Warehouse_Warehouse_Code_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
        CONSTRAINT [CK_Warehouse_Warehouse_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE UNIQUE INDEX [UX_Warehouse_Warehouse_OneSalesWarehousePerCompany]
        ON [Warehouse].[Warehouse] ([IdCompany]) WHERE [IsSalesWarehouse] = 1;

    CREATE TABLE [Warehouse].[SiteWarehouseSetting]
    (
        [Id] TINYINT NOT NULL,
        [IdWarehouse] BIGINT NOT NULL,
        [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_SiteWarehouseSetting_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedByUserKey] NVARCHAR(128) NULL,
        CONSTRAINT [PK_Warehouse_SiteWarehouseSetting] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_SiteWarehouseSetting_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id]),
        CONSTRAINT [CK_Warehouse_SiteWarehouseSetting_SingleRow] CHECK ([Id] = 1)
    );

    CREATE TABLE [Warehouse].[Unit]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Unit_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Warehouse_Unit] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Warehouse_Unit_Code] UNIQUE ([Code]),
        CONSTRAINT [CK_Warehouse_Unit_Code_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
        CONSTRAINT [CK_Warehouse_Unit_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Warehouse].[Product]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdBaseUnit] BIGINT NOT NULL,
        [StoreProductKey] NVARCHAR(128) NOT NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Product_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_Product_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_Product] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_Product_BaseUnit] FOREIGN KEY ([IdBaseUnit]) REFERENCES [Warehouse].[Unit] ([Id]),
        CONSTRAINT [UQ_Warehouse_Product_StoreProductKey] UNIQUE ([StoreProductKey]),
        CONSTRAINT [CK_Warehouse_Product_StoreKey_NotEmpty] CHECK (LEN(LTRIM(RTRIM([StoreProductKey]))) > 0),
        CONSTRAINT [CK_Warehouse_Product_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Warehouse].[ProductUnit]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdUnit] BIGINT NOT NULL,
        [ConversionFactor] DECIMAL(18,9) NOT NULL,
        [IsBaseUnit] BIT NOT NULL CONSTRAINT [DF_Warehouse_ProductUnit_IsBaseUnit] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_ProductUnit_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Warehouse_ProductUnit] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_ProductUnit_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]),
        CONSTRAINT [FK_Warehouse_ProductUnit_Unit] FOREIGN KEY ([IdUnit]) REFERENCES [Warehouse].[Unit] ([Id]),
        CONSTRAINT [UQ_Warehouse_ProductUnit_Product_Unit] UNIQUE ([IdProduct], [IdUnit]),
        CONSTRAINT [CK_Warehouse_ProductUnit_ConversionFactor_Positive] CHECK ([ConversionFactor] > 0),
        CONSTRAINT [CK_Warehouse_ProductUnit_BaseFactor_IsOne] CHECK ([IsBaseUnit] = 0 OR [ConversionFactor] = 1)
    );

    CREATE UNIQUE INDEX [UX_Warehouse_ProductUnit_OneBaseUnitPerProduct]
        ON [Warehouse].[ProductUnit] ([IdProduct]) WHERE [IsBaseUnit] = 1;

    CREATE TABLE [Warehouse].[ProductVariant]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [Code] NVARCHAR(100) NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [AttributesJson] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_ProductVariant_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_ProductVariant_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_ProductVariant] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_ProductVariant_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]),
        CONSTRAINT [UQ_Warehouse_ProductVariant_Id_IdProduct] UNIQUE ([Id], [IdProduct]),
        CONSTRAINT [CK_Warehouse_ProductVariant_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
        CONSTRAINT [CK_Warehouse_ProductVariant_AttributesJson] CHECK ([AttributesJson] IS NULL OR ISJSON([AttributesJson]) = 1)
    );

    CREATE UNIQUE INDEX [UX_Warehouse_ProductVariant_Product_Code]
        ON [Warehouse].[ProductVariant] ([IdProduct], [Code]) WHERE [Code] IS NOT NULL;

    CREATE TABLE [Warehouse].[InventoryItem]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdProductVariant] BIGINT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_InventoryItem_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Warehouse_InventoryItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_InventoryItem_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Warehouse].[Product] ([Id]),
        CONSTRAINT [FK_Warehouse_InventoryItem_ProductVariant] FOREIGN KEY ([IdProductVariant], [IdProduct]) REFERENCES [Warehouse].[ProductVariant] ([Id], [IdProduct])
    );

    CREATE UNIQUE INDEX [UX_Warehouse_InventoryItem_ProductWithoutVariant]
        ON [Warehouse].[InventoryItem] ([IdProduct]) WHERE [IdProductVariant] IS NULL;
    CREATE UNIQUE INDEX [UX_Warehouse_InventoryItem_ProductVariant]
        ON [Warehouse].[InventoryItem] ([IdProduct], [IdProductVariant]) WHERE [IdProductVariant] IS NOT NULL;

    CREATE TABLE [Warehouse].[Party]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdWarehouse] BIGINT NULL,
        [PartyType] NVARCHAR(30) NOT NULL,
        [ExternalPartyKey] NVARCHAR(128) NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_Party_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_Party_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_Party] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_Party_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id]),
        CONSTRAINT [CK_Warehouse_Party_Type] CHECK ([PartyType] IN (N'Person', N'Organization', N'Warehouse', N'Other')),
        CONSTRAINT [CK_Warehouse_Party_WarehouseLink] CHECK (([PartyType] = N'Warehouse' AND [IdWarehouse] IS NOT NULL) OR ([PartyType] <> N'Warehouse' AND [IdWarehouse] IS NULL)),
        CONSTRAINT [CK_Warehouse_Party_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE UNIQUE INDEX [UX_Warehouse_Party_Warehouse]
        ON [Warehouse].[Party] ([IdWarehouse]) WHERE [IdWarehouse] IS NOT NULL;
    CREATE UNIQUE INDEX [UX_Warehouse_Party_ExternalKey]
        ON [Warehouse].[Party] ([ExternalPartyKey]) WHERE [ExternalPartyKey] IS NOT NULL;

    CREATE TABLE [Warehouse].[DocumentType]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Code] NVARCHAR(50) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [Direction] CHAR(1) NOT NULL,
        [AffectsStock] BIT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Warehouse_DocumentType_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_DocumentType_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_DocumentType] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Warehouse_DocumentType_Code] UNIQUE ([Code]),
        CONSTRAINT [CK_Warehouse_DocumentType_Direction] CHECK ([Direction] IN ('R', 'I')),
        CONSTRAINT [CK_Warehouse_DocumentType_Code_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
        CONSTRAINT [CK_Warehouse_DocumentType_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Warehouse].[WarehouseDocument]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdWarehouse] BIGINT NOT NULL,
        [IdDocumentType] BIGINT NOT NULL,
        [IdParty] BIGINT NOT NULL,
        [IdSalesPlanReference] BIGINT NOT NULL,
        [DocumentNumber] BIGINT NOT NULL CONSTRAINT [DF_Warehouse_WarehouseDocument_DocumentNumber] DEFAULT (NEXT VALUE FOR [Warehouse].[DocumentNumberSequence]),
        [DocumentDate] DATE NOT NULL,
        [Status] CHAR(1) NOT NULL CONSTRAINT [DF_Warehouse_WarehouseDocument_Status] DEFAULT ('D'),
        [CustomerOrderKey] NVARCHAR(128) NULL,
        [Reason] NVARCHAR(1000) NULL,
        [CreatedByUserKey] NVARCHAR(128) NOT NULL,
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_WarehouseDocument_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedByUserKey] NVARCHAR(128) NULL,
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_WarehouseDocument] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocument_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocument_DocumentType] FOREIGN KEY ([IdDocumentType]) REFERENCES [Warehouse].[DocumentType] ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocument_Party] FOREIGN KEY ([IdParty]) REFERENCES [Warehouse].[Party] ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocument_SalesPlanReference] FOREIGN KEY ([IdSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]),
        CONSTRAINT [UQ_Warehouse_WarehouseDocument_DocumentNumber] UNIQUE ([DocumentNumber]),
        CONSTRAINT [CK_Warehouse_WarehouseDocument_Status] CHECK ([Status] IN ('D', 'F')),
        CONSTRAINT [CK_Warehouse_WarehouseDocument_CreatedBy_NotEmpty] CHECK (LEN(LTRIM(RTRIM([CreatedByUserKey]))) > 0)
    );

    CREATE UNIQUE INDEX [UX_Warehouse_WarehouseDocument_CustomerOrder]
        ON [Warehouse].[WarehouseDocument] ([CustomerOrderKey]) WHERE [CustomerOrderKey] IS NOT NULL;
    CREATE INDEX [IX_Warehouse_WarehouseDocument_Warehouse_Date]
        ON [Warehouse].[WarehouseDocument] ([IdWarehouse], [DocumentDate], [DocumentNumber]);

    CREATE TABLE [Warehouse].[WarehouseDocumentLine]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdDocument] BIGINT NOT NULL,
        [IdInventoryItem] BIGINT NOT NULL,
        [IdUnit] BIGINT NOT NULL,
        [Quantity] DECIMAL(18,3) NOT NULL,
        [ConversionFactor] DECIMAL(18,9) NOT NULL,
        [BaseQuantity] DECIMAL(18,3) NOT NULL,
        [ProductNameSnapshot] NVARCHAR(300) NOT NULL,
        [VariantNameSnapshot] NVARCHAR(300) NULL,
        [UnitNameSnapshot] NVARCHAR(100) NOT NULL,
        CONSTRAINT [PK_Warehouse_WarehouseDocumentLine] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocumentLine_Document] FOREIGN KEY ([IdDocument]) REFERENCES [Warehouse].[WarehouseDocument] ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocumentLine_InventoryItem] FOREIGN KEY ([IdInventoryItem]) REFERENCES [Warehouse].[InventoryItem] ([Id]),
        CONSTRAINT [FK_Warehouse_WarehouseDocumentLine_Unit] FOREIGN KEY ([IdUnit]) REFERENCES [Warehouse].[Unit] ([Id]),
        CONSTRAINT [CK_Warehouse_WarehouseDocumentLine_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_Warehouse_WarehouseDocumentLine_ConversionFactor_Positive] CHECK ([ConversionFactor] > 0),
        CONSTRAINT [CK_Warehouse_WarehouseDocumentLine_BaseQuantity_Positive] CHECK ([BaseQuantity] > 0)
    );

    CREATE INDEX [IX_Warehouse_WarehouseDocumentLine_Document]
        ON [Warehouse].[WarehouseDocumentLine] ([IdDocument]);

    CREATE TABLE [Warehouse].[StockBalance]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdWarehouse] BIGINT NOT NULL,
        [IdInventoryItem] BIGINT NOT NULL,
        [IdSalesPlanReference] BIGINT NOT NULL,
        [Quantity] DECIMAL(18,3) NOT NULL CONSTRAINT [DF_Warehouse_StockBalance_Quantity] DEFAULT (0),
        [MinimumQuantity] DECIMAL(18,3) NOT NULL CONSTRAINT [DF_Warehouse_StockBalance_MinimumQuantity] DEFAULT (0),
        [UpdatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_StockBalance_UpdatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [Version] ROWVERSION NOT NULL,
        CONSTRAINT [PK_Warehouse_StockBalance] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_StockBalance_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id]),
        CONSTRAINT [FK_Warehouse_StockBalance_InventoryItem] FOREIGN KEY ([IdInventoryItem]) REFERENCES [Warehouse].[InventoryItem] ([Id]),
        CONSTRAINT [FK_Warehouse_StockBalance_SalesPlanReference] FOREIGN KEY ([IdSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]),
        CONSTRAINT [UQ_Warehouse_StockBalance_Key] UNIQUE ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference]),
        CONSTRAINT [CK_Warehouse_StockBalance_Quantity_NonNegative] CHECK ([Quantity] >= 0),
        CONSTRAINT [CK_Warehouse_StockBalance_Minimum_NonNegative] CHECK ([MinimumQuantity] >= 0)
    );

    CREATE TABLE [Warehouse].[StockMovement]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdWarehouse] BIGINT NOT NULL,
        [IdInventoryItem] BIGINT NOT NULL,
        [IdSalesPlanReference] BIGINT NOT NULL,
        [IdDocument] BIGINT NOT NULL,
        [IdDocumentLine] BIGINT NOT NULL,
        [QuantityDelta] DECIMAL(18,3) NOT NULL,
        [OccurredAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_StockMovement_OccurredAtUtc] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Warehouse_StockMovement] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_StockMovement_Warehouse] FOREIGN KEY ([IdWarehouse]) REFERENCES [Warehouse].[Warehouse] ([Id]),
        CONSTRAINT [FK_Warehouse_StockMovement_InventoryItem] FOREIGN KEY ([IdInventoryItem]) REFERENCES [Warehouse].[InventoryItem] ([Id]),
        CONSTRAINT [FK_Warehouse_StockMovement_SalesPlanReference] FOREIGN KEY ([IdSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]),
        CONSTRAINT [FK_Warehouse_StockMovement_Document] FOREIGN KEY ([IdDocument]) REFERENCES [Warehouse].[WarehouseDocument] ([Id]),
        CONSTRAINT [FK_Warehouse_StockMovement_DocumentLine] FOREIGN KEY ([IdDocumentLine]) REFERENCES [Warehouse].[WarehouseDocumentLine] ([Id]),
        CONSTRAINT [UQ_Warehouse_StockMovement_DocumentLine] UNIQUE ([IdDocumentLine]),
        CONSTRAINT [CK_Warehouse_StockMovement_NonZero] CHECK ([QuantityDelta] <> 0)
    );

    CREATE INDEX [IX_Warehouse_StockMovement_StockKey]
        ON [Warehouse].[StockMovement] ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference]);

    CREATE TABLE [Warehouse].[PaidOrderInbox]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdSalesPlanReference] BIGINT NOT NULL,
        [IdWarehouseDocument] BIGINT NULL,
        [StoreOrderKey] NVARCHAR(128) NOT NULL,
        [CustomerKey] NVARCHAR(128) NOT NULL,
        [PaymentAtUtc] DATETIME2(3) NOT NULL,
        [Status] CHAR(1) NOT NULL CONSTRAINT [DF_Warehouse_PaidOrderInbox_Status] DEFAULT ('P'),
        [ReceivedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Warehouse_PaidOrderInbox_ReceivedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Warehouse_PaidOrderInbox] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_PaidOrderInbox_SalesPlanReference] FOREIGN KEY ([IdSalesPlanReference]) REFERENCES [Warehouse].[SalesPlanReference] ([Id]),
        CONSTRAINT [FK_Warehouse_PaidOrderInbox_WarehouseDocument] FOREIGN KEY ([IdWarehouseDocument]) REFERENCES [Warehouse].[WarehouseDocument] ([Id]),
        CONSTRAINT [UQ_Warehouse_PaidOrderInbox_StoreOrderKey] UNIQUE ([StoreOrderKey]),
        CONSTRAINT [CK_Warehouse_PaidOrderInbox_Status] CHECK ([Status] IN ('P', 'C', 'R')),
        CONSTRAINT [CK_Warehouse_PaidOrderInbox_OrderKey_NotEmpty] CHECK (LEN(LTRIM(RTRIM([StoreOrderKey]))) > 0)
    );

    CREATE INDEX [IX_Warehouse_PaidOrderInbox_Queue]
        ON [Warehouse].[PaidOrderInbox] ([Status], [PaymentAtUtc], [Id]);

    CREATE TABLE [Warehouse].[PaidOrderInboxLine]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdPaidOrderInbox] BIGINT NOT NULL,
        [IdInventoryItem] BIGINT NOT NULL,
        [IdUnit] BIGINT NOT NULL,
        [Quantity] DECIMAL(18,3) NOT NULL,
        [ConversionFactor] DECIMAL(18,9) NOT NULL,
        [BaseQuantity] DECIMAL(18,3) NOT NULL,
        CONSTRAINT [PK_Warehouse_PaidOrderInboxLine] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouse_PaidOrderInboxLine_PaidOrderInbox] FOREIGN KEY ([IdPaidOrderInbox]) REFERENCES [Warehouse].[PaidOrderInbox] ([Id]),
        CONSTRAINT [FK_Warehouse_PaidOrderInboxLine_InventoryItem] FOREIGN KEY ([IdInventoryItem]) REFERENCES [Warehouse].[InventoryItem] ([Id]),
        CONSTRAINT [FK_Warehouse_PaidOrderInboxLine_Unit] FOREIGN KEY ([IdUnit]) REFERENCES [Warehouse].[Unit] ([Id]),
        CONSTRAINT [CK_Warehouse_PaidOrderInboxLine_Quantity_Positive] CHECK ([Quantity] > 0),
        CONSTRAINT [CK_Warehouse_PaidOrderInboxLine_ConversionFactor_Positive] CHECK ([ConversionFactor] > 0),
        CONSTRAINT [CK_Warehouse_PaidOrderInboxLine_BaseQuantity_Positive] CHECK ([BaseQuantity] > 0)
    );

    CREATE INDEX [IX_Warehouse_PaidOrderInboxLine_Order]
        ON [Warehouse].[PaidOrderInboxLine] ([IdPaidOrderInbox]);

    INSERT INTO [Warehouse].[SchemaMigration] ([MigrationKey]) VALUES (N'001-initial-schema');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

CREATE OR ALTER PROCEDURE [Warehouse].[RebuildDocumentStock]
    @IdDocument BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartedTransaction BIT = 0;
    DECLARE @IdWarehouse BIGINT;
    DECLARE @IdDocumentType BIGINT;
    DECLARE @IdParty BIGINT;
    DECLARE @IdSalesPlanReference BIGINT;
    DECLARE @Status CHAR(1);
    DECLARE @Direction CHAR(1);
    DECLARE @AffectsStock BIT;
    DECLARE @DocumentExists BIT = 0;
    DECLARE @ErrorMessage NVARCHAR(2048);

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @StartedTransaction = 1;
        END;

        SELECT
            @IdWarehouse = d.[IdWarehouse],
            @IdDocumentType = d.[IdDocumentType],
            @IdParty = d.[IdParty],
            @IdSalesPlanReference = d.[IdSalesPlanReference],
            @Status = d.[Status],
            @Direction = t.[Direction],
            @AffectsStock = t.[AffectsStock],
            @DocumentExists = 1
        FROM [Warehouse].[WarehouseDocument] AS d WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN [Warehouse].[DocumentType] AS t ON t.[Id] = d.[IdDocumentType]
        WHERE d.[Id] = @IdDocument;

        IF @DocumentExists = 0
            THROW 51001, 'Warehouse document was not found.', 1;

        CREATE TABLE #StockDelta
        (
            [IdWarehouse] BIGINT NOT NULL,
            [IdInventoryItem] BIGINT NOT NULL,
            [IdSalesPlanReference] BIGINT NOT NULL,
            [QuantityDelta] DECIMAL(18,3) NOT NULL
        );

        INSERT INTO #StockDelta ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [QuantityDelta])
        SELECT m.[IdWarehouse], m.[IdInventoryItem], m.[IdSalesPlanReference], -SUM(m.[QuantityDelta])
        FROM [Warehouse].[StockMovement] AS m WITH (UPDLOCK, HOLDLOCK)
        WHERE m.[IdDocument] = @IdDocument
        GROUP BY m.[IdWarehouse], m.[IdInventoryItem], m.[IdSalesPlanReference];

        IF @AffectsStock = 1 AND @Status = 'F'
        BEGIN
            INSERT INTO #StockDelta ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [QuantityDelta])
            SELECT
                @IdWarehouse,
                l.[IdInventoryItem],
                @IdSalesPlanReference,
                SUM(CASE
                    WHEN @Direction = 'R' AND @Status = 'F' THEN l.[BaseQuantity]
                    WHEN @Direction = 'I' AND @Status = 'F' THEN -l.[BaseQuantity]
                    ELSE 0
                END)
            FROM [Warehouse].[WarehouseDocumentLine] AS l
            WHERE l.[IdDocument] = @IdDocument
            GROUP BY l.[IdInventoryItem]
            HAVING SUM(CASE
                    WHEN @Direction = 'R' AND @Status = 'F' THEN l.[BaseQuantity]
                    WHEN @Direction = 'I' AND @Status = 'F' THEN -l.[BaseQuantity]
                    ELSE 0
                END) <> 0;
        END;

        CREATE TABLE #AggregatedDelta
        (
            [IdWarehouse] BIGINT NOT NULL,
            [IdInventoryItem] BIGINT NOT NULL,
            [IdSalesPlanReference] BIGINT NOT NULL,
            [QuantityDelta] DECIMAL(18,3) NOT NULL,
            PRIMARY KEY ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference])
        );

        INSERT INTO #AggregatedDelta ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [QuantityDelta])
        SELECT [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], SUM([QuantityDelta])
        FROM #StockDelta
        GROUP BY [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference];

        INSERT INTO [Warehouse].[StockBalance] ([IdWarehouse], [IdInventoryItem], [IdSalesPlanReference])
        SELECT k.[IdWarehouse], k.[IdInventoryItem], k.[IdSalesPlanReference]
        FROM #AggregatedDelta AS k
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM [Warehouse].[StockBalance] AS b WITH (UPDLOCK, HOLDLOCK, INDEX([UQ_Warehouse_StockBalance_Key]))
            WHERE b.[IdWarehouse] = k.[IdWarehouse]
              AND b.[IdInventoryItem] = k.[IdInventoryItem]
              AND b.[IdSalesPlanReference] = k.[IdSalesPlanReference]
        );

        IF EXISTS
        (
            SELECT 1
            FROM [Warehouse].[StockBalance] AS b WITH (UPDLOCK, HOLDLOCK)
            INNER JOIN #AggregatedDelta AS d
                ON d.[IdWarehouse] = b.[IdWarehouse]
               AND d.[IdInventoryItem] = b.[IdInventoryItem]
               AND d.[IdSalesPlanReference] = b.[IdSalesPlanReference]
            WHERE b.[Quantity] + d.[QuantityDelta] < 0
        )
            THROW 51002, 'Stock change would make physical inventory negative.', 1;

        UPDATE b
        SET b.[Quantity] = b.[Quantity] + d.[QuantityDelta],
            b.[UpdatedAtUtc] = SYSUTCDATETIME()
        FROM [Warehouse].[StockBalance] AS b
        INNER JOIN #AggregatedDelta AS d
            ON d.[IdWarehouse] = b.[IdWarehouse]
           AND d.[IdInventoryItem] = b.[IdInventoryItem]
           AND d.[IdSalesPlanReference] = b.[IdSalesPlanReference]
        WHERE d.[QuantityDelta] <> 0;

        DELETE FROM [Warehouse].[StockMovement] WHERE [IdDocument] = @IdDocument;

        IF @AffectsStock = 1 AND @Status = 'F'
        BEGIN
            INSERT INTO [Warehouse].[StockMovement]
            (
                [IdWarehouse], [IdInventoryItem], [IdSalesPlanReference], [IdDocument], [IdDocumentLine], [QuantityDelta]
            )
            SELECT
                @IdWarehouse,
                l.[IdInventoryItem],
                @IdSalesPlanReference,
                @IdDocument,
                l.[Id],
                CASE
                    WHEN @Direction = 'R' AND @Status = 'F' THEN l.[BaseQuantity]
                    WHEN @Direction = 'I' AND @Status = 'F' THEN -l.[BaseQuantity]
                END
            FROM [Warehouse].[WarehouseDocumentLine] AS l
            WHERE l.[IdDocument] = @IdDocument;
        END;

        IF @StartedTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @StartedTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
