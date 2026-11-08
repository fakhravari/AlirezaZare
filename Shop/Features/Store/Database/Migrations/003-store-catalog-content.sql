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

    IF EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'003-store-catalog-content')
    BEGIN COMMIT TRANSACTION; RETURN; END;

    CREATE TABLE [Store].[Country]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [CountryCode] NVARCHAR(8) NOT NULL,
        [NameFa] NVARCHAR(150) NOT NULL,
        [NameEn] NVARCHAR(150) NOT NULL,
        [FlagPath] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Store_Country] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_Country_CountryCode] UNIQUE ([CountryCode])
    );

    CREATE TABLE [Store].[Brand]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdCountry] BIGINT NULL,
        [BrandKey] NVARCHAR(100) NOT NULL,
        [NameEn] NVARCHAR(200) NOT NULL,
        [NameFa] NVARCHAR(200) NOT NULL,
        [LogoPath] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Store_Brand] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_Brand_Country] FOREIGN KEY ([IdCountry]) REFERENCES [Store].[Country] ([Id]),
        CONSTRAINT [UQ_Store_Brand_BrandKey] UNIQUE ([BrandKey])
    );

    CREATE TABLE [Store].[BrandEditorial]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdBrand] BIGINT NOT NULL,
        [Established] NVARCHAR(40) NULL,
        [CountryName] NVARCHAR(150) NULL,
        [OfficialUrl] NVARCHAR(1000) NULL,
        [Specialization] NVARCHAR(MAX) NULL,
        [FeaturedProducts] NVARCHAR(MAX) NULL,
        [Audience] NVARCHAR(MAX) NULL,
        [ShoppingAdvice] NVARCHAR(MAX) NULL,
        [CompanyIntroductionsJson] NVARCHAR(MAX) NULL,
        [ProductFamiliesJson] NVARCHAR(MAX) NULL,
        [StrengthsJson] NVARCHAR(MAX) NULL,
        [FullPastedText] NVARCHAR(MAX) NULL,
        [FullDocumentText] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_Store_BrandEditorial] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_BrandEditorial_Brand] FOREIGN KEY ([IdBrand]) REFERENCES [Store].[Brand] ([Id]),
        CONSTRAINT [UQ_Store_BrandEditorial_IdBrand] UNIQUE ([IdBrand]),
        CONSTRAINT [CK_Store_BrandEditorial_Json] CHECK (([CompanyIntroductionsJson] IS NULL OR ISJSON([CompanyIntroductionsJson]) = 1) AND ([ProductFamiliesJson] IS NULL OR ISJSON([ProductFamiliesJson]) = 1) AND ([StrengthsJson] IS NULL OR ISJSON([StrengthsJson]) = 1))
    );

    CREATE TABLE [Store].[Color]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [ColorKey] NVARCHAR(80) NOT NULL,
        [NameEn] NVARCHAR(100) NOT NULL,
        [NameFa] NVARCHAR(100) NOT NULL,
        [HexCode] CHAR(7) NULL,
        CONSTRAINT [PK_Store_Color] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_Color_ColorKey] UNIQUE ([ColorKey]),
        CONSTRAINT [CK_Store_Color_HexCode] CHECK ([HexCode] IS NULL OR [HexCode] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
    );

    CREATE TABLE [Store].[Category]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [CategoryKey] NVARCHAR(150) NOT NULL,
        [ParentCategoryKey] NVARCHAR(150) NULL,
        [NameEn] NVARCHAR(200) NOT NULL,
        [NameFa] NVARCHAR(200) NOT NULL,
        [PathJson] NVARCHAR(MAX) NOT NULL,
        [SortOrder] INT NOT NULL CONSTRAINT [DF_Store_Category_SortOrder] DEFAULT (0),
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_Category_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Store_Category] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_Category_CategoryKey] UNIQUE ([CategoryKey]),
        CONSTRAINT [FK_Store_Category_Parent] FOREIGN KEY ([ParentCategoryKey]) REFERENCES [Store].[Category] ([CategoryKey]),
        CONSTRAINT [CK_Store_Category_PathJson] CHECK (ISJSON([PathJson]) = 1)
    );

    CREATE TABLE [Store].[SpecificationGroup]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [GroupKey] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_SpecificationGroup] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_SpecificationGroup_GroupKey] UNIQUE ([GroupKey])
    );

    CREATE TABLE [Store].[SpecificationCategory]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdSpecificationGroup] BIGINT NOT NULL,
        [CategoryKey] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_SpecificationCategory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_SpecificationCategory_Group] FOREIGN KEY ([IdSpecificationGroup]) REFERENCES [Store].[SpecificationGroup] ([Id]),
        CONSTRAINT [UQ_Store_SpecificationCategory_CategoryKey] UNIQUE ([CategoryKey])
    );

    CREATE TABLE [Store].[SpecificationFeature]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdSpecificationCategory] BIGINT NOT NULL,
        [FeatureKey] NVARCHAR(100) NOT NULL,
        [Label] NVARCHAR(200) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_SpecificationFeature] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_SpecificationFeature_Category] FOREIGN KEY ([IdSpecificationCategory]) REFERENCES [Store].[SpecificationCategory] ([Id]),
        CONSTRAINT [UQ_Store_SpecificationFeature_FeatureKey] UNIQUE ([FeatureKey])
    );

    CREATE TABLE [Store].[AttributeDefinition]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [AttributeKey] NVARCHAR(100) NOT NULL,
        [Label] NVARCHAR(200) NOT NULL,
        [Icon] NVARCHAR(100) NULL,
        [SourceKey] NVARCHAR(100) NULL,
        [FilterKey] NVARCHAR(100) NULL,
        [IsFilterable] BIT NOT NULL CONSTRAINT [DF_Store_AttributeDefinition_IsFilterable] DEFAULT (0),
        [IsHighlight] BIT NOT NULL CONSTRAINT [DF_Store_AttributeDefinition_IsHighlight] DEFAULT (0),
        CONSTRAINT [PK_Store_AttributeDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_AttributeDefinition_AttributeKey] UNIQUE ([AttributeKey])
    );

    CREATE TABLE [Store].[ProductColor]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdColor] BIGINT NOT NULL,
        CONSTRAINT [PK_Store_ProductColor] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductColor_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [FK_Store_ProductColor_Color] FOREIGN KEY ([IdColor]) REFERENCES [Store].[Color] ([Id]),
        CONSTRAINT [UQ_Store_ProductColor_Product_Color] UNIQUE ([IdProduct], [IdColor])
    );

    CREATE TABLE [Store].[ProductSpecificationValue]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdSpecificationFeature] BIGINT NOT NULL,
        [Value] NVARCHAR(1000) NOT NULL,
        CONSTRAINT [PK_Store_ProductSpecificationValue] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductSpecificationValue_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [FK_Store_ProductSpecificationValue_Feature] FOREIGN KEY ([IdSpecificationFeature]) REFERENCES [Store].[SpecificationFeature] ([Id]),
        CONSTRAINT [UQ_Store_ProductSpecificationValue_Product_Feature] UNIQUE ([IdProduct], [IdSpecificationFeature])
    );

    CREATE TABLE [Store].[ProductDetail]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [DetailKey] NVARCHAR(100) NOT NULL,
        [Intro] NVARCHAR(MAX) NULL,
        [HighlightsJson] NVARCHAR(MAX) NULL,
        [SpecsJson] NVARCHAR(MAX) NULL,
        [ReviewsJson] NVARCHAR(MAX) NULL,
        [QuestionsJson] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_Store_ProductDetail] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_ProductDetail_DetailKey] UNIQUE ([DetailKey]),
        CONSTRAINT [CK_Store_ProductDetail_Json] CHECK (([HighlightsJson] IS NULL OR ISJSON([HighlightsJson]) = 1) AND ([SpecsJson] IS NULL OR ISJSON([SpecsJson]) = 1) AND ([ReviewsJson] IS NULL OR ISJSON([ReviewsJson]) = 1) AND ([QuestionsJson] IS NULL OR ISJSON([QuestionsJson]) = 1))
    );

    CREATE TABLE [Store].[ProductMedia]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [MediaKey] NVARCHAR(100) NOT NULL,
        [MainPath] NVARCHAR(1000) NULL,
        [CardPath] NVARCHAR(1000) NULL,
        [ThumbnailPath] NVARCHAR(1000) NULL,
        [ImagesJson] NVARCHAR(MAX) NULL,
        [VideosJson] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_Store_ProductMedia] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductMedia_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [UQ_Store_ProductMedia_Product_MediaKey] UNIQUE ([IdProduct], [MediaKey]),
        CONSTRAINT [CK_Store_ProductMedia_Json] CHECK (([ImagesJson] IS NULL OR ISJSON([ImagesJson]) = 1) AND ([VideosJson] IS NULL OR ISJSON([VideosJson]) = 1))
    );

    CREATE TABLE [Store].[ProductPurchaseOption]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [OptionKey] NVARCHAR(100) NOT NULL,
        [Label] NVARCHAR(200) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_ProductPurchaseOption] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductPurchaseOption_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [UQ_Store_ProductPurchaseOption_Product_Key] UNIQUE ([IdProduct], [OptionKey])
    );

    CREATE TABLE [Store].[ProductPurchaseOptionValue]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProductPurchaseOption] BIGINT NOT NULL,
        [ValueKey] NVARCHAR(100) NOT NULL,
        [Label] NVARCHAR(200) NOT NULL,
        [PriceDelta] DECIMAL(19,2) NOT NULL CONSTRAINT [DF_Store_ProductPurchaseOptionValue_PriceDelta] DEFAULT (0),
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_ProductPurchaseOptionValue] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductPurchaseOptionValue_Option] FOREIGN KEY ([IdProductPurchaseOption]) REFERENCES [Store].[ProductPurchaseOption] ([Id]),
        CONSTRAINT [UQ_Store_ProductPurchaseOptionValue_Option_Key] UNIQUE ([IdProductPurchaseOption], [ValueKey])
    );

    CREATE TABLE [Store].[Accessory]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [AccessoryKey] NVARCHAR(150) NOT NULL,
        [Name] NVARCHAR(300) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_Accessory_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Store_Accessory] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_Accessory_AccessoryKey] UNIQUE ([AccessoryKey])
    );

    CREATE TABLE [Store].[ProductAccessory]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [IdAccessory] BIGINT NOT NULL,
        [IsRequired] BIT NOT NULL CONSTRAINT [DF_Store_ProductAccessory_IsRequired] DEFAULT (0),
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_ProductAccessory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductAccessory_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [FK_Store_ProductAccessory_Accessory] FOREIGN KEY ([IdAccessory]) REFERENCES [Store].[Accessory] ([Id]),
        CONSTRAINT [UQ_Store_ProductAccessory_Product_Accessory] UNIQUE ([IdProduct], [IdAccessory])
    );

    CREATE TABLE [Store].[ProductSalesPlanOfferAccessory]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProductSalesPlanOffer] BIGINT NOT NULL,
        [IdAccessory] BIGINT NOT NULL,
        [PriceAmount] DECIMAL(19,0) NOT NULL,
        [IsIncludedByDefault] BIT NOT NULL CONSTRAINT [DF_Store_ProductSalesPlanOfferAccessory_Included] DEFAULT (0),
        CONSTRAINT [PK_Store_ProductSalesPlanOfferAccessory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductSalesPlanOfferAccessory_Offer] FOREIGN KEY ([IdProductSalesPlanOffer]) REFERENCES [Store].[ProductSalesPlanOffer] ([Id]),
        CONSTRAINT [FK_Store_ProductSalesPlanOfferAccessory_Accessory] FOREIGN KEY ([IdAccessory]) REFERENCES [Store].[Accessory] ([Id]),
        CONSTRAINT [UQ_Store_ProductSalesPlanOfferAccessory_Offer_Accessory] UNIQUE ([IdProductSalesPlanOffer], [IdAccessory]),
        CONSTRAINT [CK_Store_ProductSalesPlanOfferAccessory_Price_NonNegative] CHECK ([PriceAmount] >= 0)
    );

    CREATE TABLE [Store].[ProductBenefit]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [Title] NVARCHAR(300) NOT NULL,
        [Detail] NVARCHAR(1000) NULL,
        [Icon] NVARCHAR(100) NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_ProductBenefit] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductBenefit_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id])
    );

    CREATE TABLE [Store].[ProductCollection]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [CollectionKey] NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        CONSTRAINT [PK_Store_ProductCollection] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_ProductCollection_CollectionKey] UNIQUE ([CollectionKey])
    );

    CREATE TABLE [Store].[ProductCollectionItem]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdProductCollection] BIGINT NOT NULL,
        [IdProduct] BIGINT NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_ProductCollectionItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ProductCollectionItem_Collection] FOREIGN KEY ([IdProductCollection]) REFERENCES [Store].[ProductCollection] ([Id]),
        CONSTRAINT [FK_Store_ProductCollectionItem_Product] FOREIGN KEY ([IdProduct]) REFERENCES [Store].[Product] ([Id]),
        CONSTRAINT [UQ_Store_ProductCollectionItem_Collection_Product] UNIQUE ([IdProductCollection], [IdProduct])
    );

    CREATE TABLE [Store].[ContentPage]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [PageKey] NVARCHAR(150) NOT NULL,
        [RoutePath] NVARCHAR(300) NOT NULL,
        [Title] NVARCHAR(300) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [ContentSource] NVARCHAR(30) NOT NULL,
        [IsPublished] BIT NOT NULL CONSTRAINT [DF_Store_ContentPage_IsPublished] DEFAULT (1),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_ContentPage] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_ContentPage_PageKey] UNIQUE ([PageKey]),
        CONSTRAINT [UQ_Store_ContentPage_RoutePath] UNIQUE ([RoutePath]),
        CONSTRAINT [CK_Store_ContentPage_ContentSource] CHECK ([ContentSource] IN (N'Razor', N'Database', N'Demo'))
    );

    CREATE TABLE [Store].[ContentPageSection]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdContentPage] BIGINT NOT NULL,
        [SectionKey] NVARCHAR(100) NOT NULL,
        [Heading] NVARCHAR(300) NULL,
        [Body] NVARCHAR(MAX) NULL,
        [DataJson] NVARCHAR(MAX) NULL,
        [SortOrder] INT NOT NULL,
        [IsPublished] BIT NOT NULL CONSTRAINT [DF_Store_ContentPageSection_IsPublished] DEFAULT (1),
        CONSTRAINT [PK_Store_ContentPageSection] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ContentPageSection_Page] FOREIGN KEY ([IdContentPage]) REFERENCES [Store].[ContentPage] ([Id]),
        CONSTRAINT [UQ_Store_ContentPageSection_Page_SectionKey] UNIQUE ([IdContentPage], [SectionKey]),
        CONSTRAINT [CK_Store_ContentPageSection_DataJson] CHECK ([DataJson] IS NULL OR ISJSON([DataJson]) = 1)
    );

    CREATE TABLE [Store].[FaqItem]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdContentPage] BIGINT NOT NULL,
        [Question] NVARCHAR(1000) NOT NULL,
        [Answer] NVARCHAR(MAX) NOT NULL,
        [LinkText] NVARCHAR(300) NULL,
        [LinkUrl] NVARCHAR(1000) NULL,
        [SortOrder] INT NOT NULL,
        [IsPublished] BIT NOT NULL CONSTRAINT [DF_Store_FaqItem_IsPublished] DEFAULT (1),
        CONSTRAINT [PK_Store_FaqItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_FaqItem_Page] FOREIGN KEY ([IdContentPage]) REFERENCES [Store].[ContentPage] ([Id])
    );

    CREATE TABLE [Store].[ContactMessage]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdContentPage] BIGINT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Phone] NVARCHAR(40) NOT NULL,
        [Email] NVARCHAR(254) NULL,
        [Subject] NVARCHAR(200) NULL,
        [Message] NVARCHAR(4000) NOT NULL,
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_Store_ContactMessage_Status] DEFAULT (N'New'),
        [ReceivedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_ContactMessage_ReceivedAtUtc] DEFAULT SYSUTCDATETIME(),
        [HandledAtUtc] DATETIME2(3) NULL,
        [HandledByUserKey] NVARCHAR(128) NULL,
        [AdminNote] NVARCHAR(2000) NULL,
        CONSTRAINT [PK_Store_ContactMessage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_ContactMessage_Page] FOREIGN KEY ([IdContentPage]) REFERENCES [Store].[ContentPage] ([Id]),
        CONSTRAINT [CK_Store_ContactMessage_Status] CHECK ([Status] IN (N'New', N'InProgress', N'Replied', N'Closed')),
        CONSTRAINT [CK_Store_ContactMessage_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([FullName]))) > 0),
        CONSTRAINT [CK_Store_ContactMessage_Message_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Message]))) > 0)
    );

    CREATE TABLE [Store].[ContactChannel]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [ChannelType] NVARCHAR(30) NOT NULL,
        [Label] NVARCHAR(100) NOT NULL,
        [Value] NVARCHAR(500) NOT NULL,
        [Uri] NVARCHAR(1000) NULL,
        [SortOrder] INT NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_Store_ContactChannel_IsActive] DEFAULT (1),
        CONSTRAINT [PK_Store_ContactChannel] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Store_ContactChannel_Type] CHECK ([ChannelType] IN (N'Phone', N'Email', N'WhatsApp', N'Address', N'Other'))
    );

    CREATE TABLE [Store].[MagazineArticle]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [ArticleKey] NVARCHAR(150) NOT NULL,
        [Category] NVARCHAR(150) NOT NULL,
        [Title] NVARCHAR(300) NOT NULL,
        [Summary] NVARCHAR(1500) NOT NULL,
        [Author] NVARCHAR(200) NULL,
        [PublishedAt] DATE NULL,
        [ReadTimeMinutes] INT NULL,
        [HeroImagePath] NVARCHAR(1000) NULL,
        [Lead] NVARCHAR(MAX) NULL,
        [Conclusion] NVARCHAR(MAX) NULL,
        [IsPublished] BIT NOT NULL CONSTRAINT [DF_Store_MagazineArticle_IsPublished] DEFAULT (1),
        [CreatedAtUtc] DATETIME2(3) NOT NULL CONSTRAINT [DF_Store_MagazineArticle_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2(3) NULL,
        CONSTRAINT [PK_Store_MagazineArticle] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Store_MagazineArticle_ArticleKey] UNIQUE ([ArticleKey])
    );

    CREATE TABLE [Store].[MagazineArticleSection]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdMagazineArticle] BIGINT NOT NULL,
        [Heading] NVARCHAR(300) NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_MagazineArticleSection] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_MagazineArticleSection_Article] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Store].[MagazineArticle] ([Id])
    );

    CREATE TABLE [Store].[MagazineArticleSource]
    (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [IdMagazineArticle] BIGINT NOT NULL,
        [Title] NVARCHAR(300) NOT NULL,
        [Url] NVARCHAR(2000) NOT NULL,
        [SortOrder] INT NOT NULL,
        CONSTRAINT [PK_Store_MagazineArticleSource] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Store_MagazineArticleSource_Article] FOREIGN KEY ([IdMagazineArticle]) REFERENCES [Store].[MagazineArticle] ([Id])
    );

    ALTER TABLE [Store].[Product] ADD [IdBrand] BIGINT NULL, [IdCategory] BIGINT NULL, [DetailKey] NVARCHAR(100) NULL, [EnglishName] NVARCHAR(300) NULL, [PowerSource] NVARCHAR(200) NULL, [UseCase] NVARCHAR(500) NULL, [PriceAmount] DECIMAL(19,0) NULL, [DiscountPercent] DECIMAL(5,2) NULL, [MaxPurchaseQuantity] INT NULL, [Slug] NVARCHAR(200) NULL, [Badge] NVARCHAR(100) NULL, [IsNewest] BIT NOT NULL CONSTRAINT [DF_Store_Product_IsNewest] DEFAULT (0), [NewestOrder] INT NULL;

    ALTER TABLE [Store].[Product] ADD CONSTRAINT [FK_Store_Product_Brand] FOREIGN KEY ([IdBrand]) REFERENCES [Store].[Brand] ([Id]);
    ALTER TABLE [Store].[Product] ADD CONSTRAINT [FK_Store_Product_Category] FOREIGN KEY ([IdCategory]) REFERENCES [Store].[Category] ([Id]);
    ALTER TABLE [Store].[Product] ADD CONSTRAINT [FK_Store_Product_Detail] FOREIGN KEY ([DetailKey]) REFERENCES [Store].[ProductDetail] ([DetailKey]);
    EXEC sys.sp_executesql N'CREATE UNIQUE INDEX [UX_Store_Product_Slug] ON [Store].[Product] ([Slug]) WHERE [Slug] IS NOT NULL;';
    EXEC sys.sp_executesql N'CREATE INDEX [IX_Store_Product_Brand_Category] ON [Store].[Product] ([IdBrand], [IdCategory]);';

    INSERT INTO [Store].[SchemaMigration] ([MigrationKey]) VALUES (N'003-store-catalog-content');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
