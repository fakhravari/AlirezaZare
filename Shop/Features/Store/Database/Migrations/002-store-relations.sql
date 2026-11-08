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

    IF SCHEMA_ID(N'Store') IS NULL EXEC(N'CREATE SCHEMA [Store]');

    IF OBJECT_ID(N'[Store].[SchemaMigration]', N'U') IS NULL
    BEGIN
        CREATE TABLE [Store].[SchemaMigration] ([Id] BIGINT IDENTITY(1,1) NOT NULL, [MigrationKey] NVARCHAR(100) NOT NULL, [AppliedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_SchemaMigration_AppliedAtUtc] DEFAULT SYSUTCDATETIME(), CONSTRAINT [PK_Store_SchemaMigration] PRIMARY KEY ([Id]), CONSTRAINT [UQ_Store_SchemaMigration_MigrationKey] UNIQUE ([MigrationKey]));
    END;

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'002-store-relations')
    BEGIN COMMIT TRANSACTION; RETURN; END;

    CREATE TABLE [Store].[Product]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [StoreProductKey] NVARCHAR(128) NOT NULL,
        [Code] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_Product_IsActive] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_Product_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_Product] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_Product_StoreProductKey] UNIQUE ([StoreProductKey]),
        CONSTRAINT [CK_Store_Product_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
    );

    CREATE TABLE [Store].[SalesPlan]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [SalesPlanKey] NVARCHAR(64) NOT NULL,
        [Code] NVARCHAR(64) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [PlanType] NVARCHAR(30) NOT NULL,
        [IsDefault] BIT NOT NULL CONSTRAINT [DF_Store_SalesPlan_IsDefault] DEFAULT (0),
        [IsEnabled] BIT NOT NULL CONSTRAINT [DF_Store_SalesPlan_IsEnabled] DEFAULT (1),
        [StartsAtUtc] DATETIMEOFFSET(3) NULL,
        [EndsAtUtc] DATETIMEOFFSET(3) NULL,
        [Label] NVARCHAR(100) NULL,
        [ShowCountdown] BIT NOT NULL CONSTRAINT [DF_Store_SalesPlan_ShowCountdown] DEFAULT (0),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_SalesPlan_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_SalesPlan] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_SalesPlan_SalesPlanKey] UNIQUE ([SalesPlanKey]),
        CONSTRAINT [CK_Store_SalesPlan_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
        CONSTRAINT [CK_Store_SalesPlan_DateRange] CHECK ([StartsAtUtc] IS NULL OR [EndsAtUtc] IS NULL OR [EndsAtUtc] > [StartsAtUtc])
    );

    CREATE UNIQUE INDEX [UX_Store_SalesPlan_OneDefault] ON [Store].[SalesPlan] ([IsDefault]) WHERE [IsDefault] = 1;

    CREATE TABLE [Store].[ProductSalesPlanOffer]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdSalesPlan] BIGINT NOT NULL,
        [PriceAmount] DECIMAL(19,0) NOT NULL,
        [DiscountPercent] DECIMAL(5,2) NOT NULL CONSTRAINT [DF_Store_ProductSalesPlanOffer_DiscountPercent] DEFAULT (0),
        [MaxPurchaseQuantity] INT NOT NULL,
        [IsEnabled] BIT NOT NULL CONSTRAINT [DF_Store_ProductSalesPlanOffer_IsEnabled] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_ProductSalesPlanOffer_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_ProductSalesPlanOffer] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductSalesPlanOffer_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [FK_Store_ProductSalesPlanOffer_SalesPlan] FOREIGN KEY ([IdSalesPlan]) REFERENCES [Store].[SalesPlan] ([Id]),
        CONSTRAINT [UQ_Store_ProductSalesPlanOffer_Product_Plan] UNIQUE ([IdProduct], [IdSalesPlan]),
        CONSTRAINT [CK_Store_ProductSalesPlanOffer_Price_NonNegative] CHECK ([PriceAmount] >= 0),
        CONSTRAINT [CK_Store_ProductSalesPlanOffer_Discount_Range] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100),
        CONSTRAINT [CK_Store_ProductSalesPlanOffer_MaxPurchase_Positive] CHECK ([MaxPurchaseQuantity] > 0)
    );

    INSERT INTO [Store].[Product] ([StoreProductKey], [Code], [Name], [IsActive]) VALUES
        (N'1', N'HN-001', N'دریل چکشی شارژی بوش با سه‌نظام اتوماتیک مناسب نصب و کارگاه', 1),
        (N'2', N'HN-002', N'دریل پیچ‌گوشتی شارژی', 1),
        (N'3', N'HN-003', N'دریل چکشی برقی', 1),
        (N'4', N'HN-004', N'دریل پیچ‌گوشتی شارژی دوسرعته', 1),
        (N'5', N'HN-005', N'دریل چکشی برقی سه‌نظام اتوماتیک', 1),
        (N'6', N'HN-006', N'مولتی‌متر دیجیتال دستی یونی‌تی برای تست و عیب‌یابی مدار', 1),
        (N'7', N'HN-007', N'مولتی‌متر دیجیتال کارگاهی', 1),
        (N'8', N'HN-008', N'مولتی‌متر دستی با نمایشگر بزرگ', 1),
        (N'9', N'HN-009', N'کلمپ‌متر دیجیتال', 1),
        (N'10', N'HN-010', N'مولتی‌متر دیجیتال تأسیساتی', 1),
        (N'11', N'HN-011', N'ایستگاه هویه دیجیتال یوهوا با کنترل دما برای تعمیر برد', 1),
        (N'12', N'HN-012', N'هویه قلمی با قابلیت تنظیم دما', 1),
        (N'13', N'HN-013', N'ایستگاه هیتر هوای گرم تعمیر برد', 1),
        (N'14', N'HN-014', N'ایستگاه لحیم‌کاری رومیزی', 1),
        (N'15', N'HN-015', N'هویه قلمی سبک', 1),
        (N'16', N'HN-016', N'چراغ کار LED شارژی قابل‌حمل فیلیپس برای بازرسی و تعمیر', 1),
        (N'17', N'HN-017', N'پروژکتور LED کارگاهی', 1),
        (N'18', N'HN-018', N'چراغ بازرسی شارژی', 1),
        (N'19', N'HN-019', N'چراغ کار LED پایه‌دار', 1),
        (N'20', N'HN-020', N'نورافکن LED قابل‌حمل', 1),
        (N'21', N'HN-021', N'ست پیچ‌گوشتی عایق ویها برای نصب و برق‌کاری', 1),
        (N'22', N'HN-022', N'ست انبر برق‌کاری', 1),
        (N'23', N'HN-023', N'مجموعه ابزار عایق برق‌کار استنلی برای تعمیر و نگهداری', 1),
        (N'24', N'HN-024', N'ست ابزار نصب و سیم‌کشی', 1),
        (N'25', N'HN-025', N'کیف ابزار برق‌کاری', 1),
        (N'26', N'HN-026', N'فازمتر دیجیتال', 1),
        (N'27', N'HN-027', N'تستر ولتاژ غیرتماسی', 1),
        (N'28', N'HN-028', N'فازمتر القایی', 1),
        (N'29', N'HN-029', N'تستر پیوستگی مدار', 1),
        (N'30', N'HN-030', N'قلم تست ولتاژ', 1);

    INSERT INTO [Store].[SalesPlan] ([SalesPlanKey], [Code], [Name], [PlanType], [IsDefault], [IsEnabled], [StartsAtUtc], [EndsAtUtc], [Label], [ShowCountdown]) VALUES
        (N'regular', N'REGULAR', N'فروش عادی', N'regular', 1, 1, NULL, NULL, NULL, 0),
        (N'digi-time', N'DIGI-TIME-DEMO', N'دیجی‌تایم', N'digi-time', 0, 1, N'2026-10-31T21:34:39.000+00:00', N'2027-01-31T18:14:58.000+00:00', N'فرصت خرید ویژه با مهلت محدود', 1),
        (N'digi-af', N'DIGI-AF-DEMO', N'دیجی‌اف', N'digi-af', 0, 1, N'2026-10-31T21:34:39.000+00:00', N'2027-02-28T21:34:39.000+00:00', N'فرصتی ویژه برای خریدی به‌صرفه', 0);

    INSERT INTO [Store].[ProductSalesPlanOffer] ([IdProduct], [IdSalesPlan], [PriceAmount], [DiscountPercent], [MaxPurchaseQuantity], [IsEnabled])
    SELECT p.[Id], sp.[Id], o.[PriceAmount], o.[DiscountPercent], o.[MaxPurchaseQuantity], o.[IsEnabled]
    FROM (VALUES
        (1, N'digi-time', 28823529, 20, 2, 1),
        (6, N'digi-time', 34117647, 20, 2, 1),
        (11, N'digi-time', 51764706, 20, 2, 1),
        (16, N'digi-time', 14000000, 20, 2, 1),
        (21, N'digi-time', 29000000, 20, 2, 1),
        (26, N'digi-time', 44000000, 20, 2, 1),
        (30, N'digi-time', 56000000, 20, 2, 1),
        (3, N'digi-af', 20000000, 12, 1, 1),
        (1, N'digi-af', 30117647, 12, 1, 1),
        (6, N'digi-af', 35882353, 12, 1, 1),
        (11, N'digi-af', 54823529, 12, 1, 1),
        (4, N'digi-af', 23000000, 12, 1, 1),
        (7, N'digi-af', 32000000, 10, 1, 1),
        (9, N'digi-af', 38000000, 12, 1, 1),
        (10, N'digi-af', 41000000, 10, 2, 1),
        (13, N'digi-af', 50000000, 12, 1, 1),
        (14, N'digi-af', 53000000, 12, 1, 1),
        (17, N'digi-af', 17000000, 10, 1, 1),
        (19, N'digi-af', 23000000, 12, 1, 1),
        (23, N'digi-af', 35000000, 10, 1, 1),
        (24, N'digi-af', 38000000, 12, 1, 1),
        (27, N'digi-af', 47000000, 10, 1, 1),
        (29, N'digi-af', 53000000, 12, 1, 1)
    ) AS o([SourceProductKey], [SalesPlanKey], [PriceAmount], [DiscountPercent], [MaxPurchaseQuantity], [IsEnabled])
    INNER JOIN [Store].[Product] AS p ON p.[StoreProductKey] = CONVERT(NVARCHAR(128), o.[SourceProductKey])
    INNER JOIN [Store].[SalesPlan] AS sp ON sp.[SalesPlanKey] = o.[SalesPlanKey];

    EXEC(N'ALTER TABLE [Warehouse].[Product] ADD [IdStoreProduct] BIGINT NULL;');
    EXEC(N'UPDATE wp SET wp.[IdStoreProduct] = sp.[Id] FROM [Warehouse].[Product] AS wp INNER JOIN [Store].[Product] AS sp ON sp.[StoreProductKey] = wp.[StoreProductKey];');
    EXEC(N'IF EXISTS (SELECT 1 FROM [Warehouse].[Product] WHERE [IdStoreProduct] IS NULL) THROW 51020, ''Warehouse product has no matching store product; refusing to add incomplete relation.'', 1;');
    EXEC(N'ALTER TABLE [Warehouse].[Product] ALTER COLUMN [IdStoreProduct] BIGINT NOT NULL;');
    EXEC(N'ALTER TABLE [Warehouse].[Product] ADD CONSTRAINT [FK_Warehouse_Product_StoreProduct] FOREIGN KEY ([IdStoreProduct]) REFERENCES [Store].[Product] ([Id]);');
    EXEC(N'CREATE UNIQUE INDEX [UX_Warehouse_Product_IdStoreProduct] ON [Warehouse].[Product] ([IdStoreProduct]);');

    EXEC(N'ALTER TABLE [Warehouse].[SalesPlanReference] ADD [IdStoreSalesPlan] BIGINT NULL;');
    EXEC(N'UPDATE wr SET wr.[IdStoreSalesPlan] = sp.[Id] FROM [Warehouse].[SalesPlanReference] AS wr INNER JOIN [Store].[SalesPlan] AS sp ON sp.[SalesPlanKey] = wr.[StoreSalesPlanKey];');
    EXEC(N'IF EXISTS (SELECT 1 FROM [Warehouse].[SalesPlanReference] WHERE [IdStoreSalesPlan] IS NULL) THROW 51021, ''Warehouse sales plan reference has no matching store sales plan; refusing to add incomplete relation.'', 1;');
    EXEC(N'ALTER TABLE [Warehouse].[SalesPlanReference] ALTER COLUMN [IdStoreSalesPlan] BIGINT NOT NULL;');
    EXEC(N'ALTER TABLE [Warehouse].[SalesPlanReference] ADD CONSTRAINT [FK_Warehouse_SalesPlanReference_StoreSalesPlan] FOREIGN KEY ([IdStoreSalesPlan]) REFERENCES [Store].[SalesPlan] ([Id]);');
    EXEC(N'CREATE UNIQUE INDEX [UX_Warehouse_SalesPlanReference_IdStoreSalesPlan] ON [Warehouse].[SalesPlanReference] ([IdStoreSalesPlan]);');

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'002-store-relations');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
