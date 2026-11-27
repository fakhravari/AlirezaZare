using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using WarehouseEntity = Shop.Data.Domain.Warehouse.Entities.Warehouse;

namespace Shop.Data.Infrastructure.Persistence;

public partial class ShopDbContext : DbContext
{
    public ShopDbContext()
    {
    }

    public ShopDbContext(DbContextOptions<ShopDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminSystemUser> AdminSystemUsers { get; set; }

    public virtual DbSet<AdminRole> AdminRoles { get; set; }

    public virtual DbSet<AdminPermission> AdminPermissions { get; set; }

    public virtual DbSet<AdminUserRole> AdminUserRoles { get; set; }

    public virtual DbSet<AdminRolePermission> AdminRolePermissions { get; set; }

    public virtual DbSet<AdminSystemUserWarehouse> AdminSystemUserWarehouses { get; set; }

    public virtual DbSet<VoucherType> VoucherTypes { get; set; }

    public virtual DbSet<WarehouseUnit> WarehouseUnits { get; set; }

    public virtual DbSet<WarehouseParty> WarehouseParties { get; set; }

    public virtual DbSet<WarehouseProductUnit> WarehouseProductUnits { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<VoucherLine> VoucherLines { get; set; }

    public virtual DbSet<WarehouseStockMovement> WarehouseStockMovements { get; set; }

    public virtual DbSet<VoucherAudit> VoucherAudits { get; set; }

    public virtual DbSet<WarehouseEntity> Warehouses { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<SalesPlanReference> SalesPlanReferences { get; set; }

    public virtual DbSet<WarehouseStockOwnerType> WarehouseStockOwnerTypes { get; set; }

    public virtual DbSet<WarehouseSchemaMigration> WarehouseSchemaMigrations { get; set; }

    public virtual DbSet<WarehouseStockOwner> WarehouseStockOwners { get; set; }

    public virtual DbSet<StockBalance> StockBalances { get; set; }

    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<VariantName> VariantNames { get; set; }

    public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }

    public virtual DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

    public virtual DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; set; }

    public virtual DbSet<WarehouseProduct> Products { get; set; }

    public virtual DbSet<StoreSlider> StoreSliders { get; set; }

    public virtual DbSet<StoreSiteIdentity> StoreSiteIdentities { get; set; }

    public virtual DbSet<MagazineArticle> MagazineArticles { get; set; }

    public virtual DbSet<MagazineArticleCategory> MagazineArticleCategories { get; set; }

    public virtual DbSet<MagazineArticleBranch> MagazineArticleBranches { get; set; }

    public virtual DbSet<MagazineArticleTopic> MagazineArticleTopics { get; set; }

    public virtual DbSet<MagazineArticleArea> MagazineArticleAreas { get; set; }

    public virtual DbSet<MagazineArticleSection> MagazineArticleSections { get; set; }

    public virtual DbSet<MagazineArticleSource> MagazineArticleSources { get; set; }

    public virtual DbSet<MagazineArticleTag> MagazineArticleTags { get; set; }

    public virtual DbSet<MagazineArticleRelated> MagazineArticleRelated { get; set; }

    public virtual DbSet<MagazineArticleMetric> MagazineArticleMetrics { get; set; }

    public virtual DbSet<AccountArticleInteraction> AccountArticleInteractions { get; set; }

    public virtual DbSet<StoreContentPage> StoreContentPages { get; set; }

    public virtual DbSet<StoreContentPageSection> StoreContentPageSections { get; set; }

    public virtual DbSet<StoreFaqItem> StoreFaqItems { get; set; }

    public virtual DbSet<ProductFeedback> ProductFeedback { get; set; }

    public virtual DbSet<ProductFeedbackStatus> ProductFeedbackStatuses { get; set; }

    public virtual DbSet<StoreHomeTestimonial> StoreHomeTestimonials { get; set; }

    public virtual DbSet<StoreAttributeDefinition> StoreAttributeDefinitions { get; set; }

    public virtual DbSet<StoreProductMediaType> StoreProductMediaTypes { get; set; }

    public virtual DbSet<StoreProductAsset> StoreProductAssets { get; set; }

    public virtual DbSet<StoreProductBenefit> StoreProductBenefits { get; set; }

    public virtual DbSet<StoreProductBadge> StoreProductBadges { get; set; }

    public virtual DbSet<StoreBrand> StoreBrands { get; set; }

    public virtual DbSet<StoreBank> StoreBanks { get; set; }

    public virtual DbSet<StoreBrandEditorial> StoreBrandEditorials { get; set; }

    public virtual DbSet<StoreColor> StoreColors { get; set; }

    public virtual DbSet<StoreSpecificationGroup> StoreSpecificationGroups { get; set; }

    public virtual DbSet<StoreSpecificationCategory> StoreSpecificationCategories { get; set; }

    public virtual DbSet<StoreSpecificationFeature> StoreSpecificationFeatures { get; set; }

    public virtual DbSet<StoreCatalogFeature> StoreCatalogFeatures { get; set; }

    public virtual DbSet<StoreCatalogFeatureValue> StoreCatalogFeatureValues { get; set; }

    public virtual DbSet<StoreCategoryCatalogFeature> StoreCategoryCatalogFeatures { get; set; }

    public virtual DbSet<StoreProductCatalogFeatureValue> StoreProductCatalogFeatureValues { get; set; }

    public virtual DbSet<StoreVariantCatalogFeatureValue> StoreVariantCatalogFeatureValues { get; set; }

    public virtual DbSet<StoreProductColor> StoreProductColors { get; set; }

    public virtual DbSet<StoreProductSpecificationValue> StoreProductSpecificationValues { get; set; }

    public virtual DbSet<StoreProductCollection> StoreProductCollections { get; set; }

    public virtual DbSet<StoreProductCollectionItem> StoreProductCollectionItems { get; set; }

    public virtual DbSet<StoreHomeCategoryTab> StoreHomeCategoryTabs { get; set; }

    public virtual DbSet<StoreCategory> StoreCategories { get; set; }

    public virtual DbSet<StoreProductSalesPlanOffer> StoreProductSalesPlanOffers { get; set; }

    public virtual DbSet<StoreProductVariantSalesPlanOffer> StoreProductVariantSalesPlanOffers { get; set; }

    public virtual DbSet<StoreAccessory> StoreAccessories { get; set; }

    public virtual DbSet<StoreProductAccessory> StoreProductAccessories { get; set; }

    public virtual DbSet<StoreProductSalesPlanOfferAccessory> StoreProductSalesPlanOfferAccessories { get; set; }

    public virtual DbSet<StoreProductSalesPlanOfferSpecificationOverride> StoreProductSalesPlanOfferSpecificationOverrides { get; set; }

    public virtual DbSet<StorePriceVersion> StorePriceVersions { get; set; }

    public virtual DbSet<StoreOrderAccessory> StoreOrderAccessories { get; set; }

    public virtual DbSet<GeoCountry> GeoCountries { get; set; }

    public virtual DbSet<GeoProvince> GeoProvinces { get; set; }

    public virtual DbSet<GeoCity> GeoCities { get; set; }

    public virtual DbSet<AccountProfile> AccountProfiles { get; set; }

    public virtual DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }

    public virtual DbSet<AccountMessage> AccountMessages { get; set; }

    public virtual DbSet<AccountGender> AccountGenders { get; set; }

    public virtual DbSet<AccountCoupon> AccountCoupons { get; set; }

    public virtual DbSet<StoreCouponUsage> StoreCouponUsages { get; set; }

    public virtual DbSet<AccountRecentSearch> AccountRecentSearches { get; set; }

    public virtual DbSet<AccountCart> AccountCarts { get; set; }

    public virtual DbSet<AccountCartItem> AccountCartItems { get; set; }

    public virtual DbSet<AccountCartItemAccessory> AccountCartItemAccessories { get; set; }

    public virtual DbSet<AccountAddress> AccountAddresses { get; set; }

    public virtual DbSet<AccountFavorite> AccountFavorites { get; set; }

    public virtual DbSet<AccountCompareItem> AccountCompareItems { get; set; }

    public virtual DbSet<AccountPriceAlert> AccountPriceAlerts { get; set; }

    public virtual DbSet<AccountArticleFeedback> AccountArticleFeedback { get; set; }

    public virtual DbSet<StoreCustomerOrderItem> StoreCustomerOrderItems { get; set; }

    public virtual DbSet<StorePaymentAttempt> StorePaymentAttempts { get; set; }

    public virtual DbSet<StoreCustomerOrderReference> StoreCustomerOrders { get; set; }

    public virtual DbSet<OrderStatusReference> OrderStatuses { get; set; }

    public virtual DbSet<PaymentAttemptStatusReference> PaymentAttemptStatuses { get; set; }

    public virtual DbSet<ContactMessageStatusReference> ContactMessageStatuses { get; set; }

    public virtual DbSet<ArticleFeedbackStatusReference> ArticleFeedbackStatuses { get; set; }

    public virtual DbSet<VoucherStatus> VoucherStatuses { get; set; }

    public virtual DbSet<StoreNewsletterSubscriber> StoreNewsletterSubscribers { get; set; }

    public virtual DbSet<StoreContactMessage> StoreContactMessages { get; set; }

    public virtual DbSet<StoreSchemaMigration> StoreSchemaMigrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Address");

            entity.ToTable("Address", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdCity)
                .HasColumnType("bigint");
            entity.Property(e => e.IdCountry)
                .HasColumnType("bigint");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProvince)
                .HasColumnType("bigint");
            entity.Property(e => e.IsDefault)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Phone)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.PostalCode)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.Recipient)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.Street)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.IdCountry)
                .HasDatabaseName("IX_Address_IdCountry");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_Address_IdProfile");

            entity.HasIndex(e => new { e.IdProvince, e.IdCountry })
                .HasDatabaseName("IX_Address_IdProvince_IdCountry");

            entity.HasIndex(e => new { e.IdCity, e.IdProvince, e.IdCountry })
                .HasDatabaseName("IX_Address_IdCity_IdProvince_IdCountry");

            entity.HasOne<GeoCity>()
                .WithMany()
                .HasForeignKey(e => new { e.IdCity, e.IdProvince, e.IdCountry })
                .HasPrincipalKey(e => new { e.Id, e.IdProvince, e.IdCountry })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_Address_City");

            entity.HasOne<GeoCountry>()
                .WithMany()
                .HasForeignKey(e => e.IdCountry)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_Address_Country");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_Address_Profile");

            entity.HasOne<GeoProvince>()
                .WithMany()
                .HasForeignKey(e => new { e.IdProvince, e.IdCountry })
                .HasPrincipalKey(e => new { e.Id, e.IdCountry })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_Address_Province");
        });

        modelBuilder.Entity<AccountArticleFeedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleFeedback");

            entity.ToTable("ArticleFeedback", "Account", table =>
            {
                table.HasCheckConstraint("CK_Account_ArticleFeedback_Rating", "([Rating] IS NULL OR [Rating]>=(1) AND [Rating]<=(5))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.FeedbackText)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdStatus)
                .HasColumnType("int");
            entity.Property(e => e.Rating)
                .HasColumnType("tinyint");

            entity.HasIndex(e => e.IdMagazineArticle)
                .HasDatabaseName("IX_ArticleFeedback_IdMagazineArticle");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_ArticleFeedback_IdProfile");

            entity.HasIndex(e => e.IdStatus)
                .HasDatabaseName("IX_ArticleFeedback_IdStatus");

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_ArticleFeedback_MagazineArticle");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_ArticleFeedback_Profile");

            entity.HasOne(e => e.StatusReference)
                .WithMany()
                .HasForeignKey(e => e.IdStatus)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ArticleFeedback_Status");
        });

        modelBuilder.Entity<AccountArticleInteraction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleInteraction");

            entity.ToTable("ArticleInteraction", "Account", table =>
            {
                table.HasCheckConstraint("CK_Account_ArticleInteraction_Type", "([InteractionType]=N'read' OR [InteractionType]=N'bookmark' OR [InteractionType]=N'like')");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.InteractionType)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);

            entity.HasIndex(e => e.IdMagazineArticle)
                .HasDatabaseName("IX_ArticleInteraction_IdMagazineArticle");

            entity.HasIndex(e => new { e.IdProfile, e.IdMagazineArticle, e.InteractionType })
                .HasDatabaseName("UQ_Account_ArticleInteraction")
                .IsUnique();

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_ArticleInteraction_MagazineArticle");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_ArticleInteraction_Profile");
        });

        modelBuilder.Entity<AccountCart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Cart");

            entity.ToTable("Cart", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("UQ_Account_Cart_Profile")
                .IsUnique();

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_Cart_Profile");
        });

        modelBuilder.Entity<AccountCartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CartItem");

            entity.ToTable("CartItem", "Account", table =>
            {
                table.HasCheckConstraint("CK_Account_CartItem_Quantity", "([Quantity]>=(1) AND [Quantity]<=(99))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdCart)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlan)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVariant)
                .HasColumnType("bigint");
            entity.Property(e => e.Quantity)
                .HasColumnType("int");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasIndex(e => e.IdSalesPlan)
                .HasDatabaseName("IX_CartItem_IdSalesPlan");

            entity.HasIndex(e => new { e.IdProduct, e.IdVariant })
                .HasDatabaseName("IX_CartItem_IdProduct_IdVariant");

            entity.HasIndex(e => new { e.IdCart, e.IdProduct, e.IdSalesPlan, e.IdVariant })
                .HasDatabaseName("UQ_Account_CartItem_Configuration")
                .IsUnique()
                .HasFilter("[IdVariant] IS NOT NULL");

            entity.HasOne<AccountCart>()
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.IdCart)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_CartItem_Cart");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CartItem_Product");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlan)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CartItem_SalesPlan");

            entity.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(e => new { e.IdProduct, e.IdVariant })
                .HasPrincipalKey(e => new { e.IdProduct, e.Id })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CartItem_WarehouseProductVariant");
        });

        modelBuilder.Entity<AccountCartItemAccessory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CartItemAccessory");

            entity.ToTable("CartItemAccessory", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdCartItem)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProductAccessory)
                .HasColumnType("numeric(30,0)");

            entity.HasIndex(e => e.IdProductAccessory)
                .HasDatabaseName("IX_CartItemAccessory_IdProductAccessory");

            entity.HasIndex(e => new { e.IdCartItem, e.IdProductAccessory })
                .HasDatabaseName("UX_Account_CartItemAccessory")
                .IsUnique();

            entity.HasOne<AccountCartItem>()
                .WithMany(e => e.Accessories)
                .HasForeignKey(e => e.IdCartItem)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_CartItemAccessory_CartItem");

            entity.HasOne<StoreProductAccessory>()
                .WithMany()
                .HasForeignKey(e => e.IdProductAccessory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CartItemAccessory_ProductAccessory");
        });

        modelBuilder.Entity<AccountCompareItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CompareItem");

            entity.ToTable("CompareItem", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_CompareItem_IdProduct");

            entity.HasIndex(e => new { e.IdProfile, e.IdProduct })
                .HasDatabaseName("UQ_Account_CompareItem_Profile_Product")
                .IsUnique();

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CompareItem_Product");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_CompareItem_Profile");
        });

        modelBuilder.Entity<AccountCoupon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Coupon");

            entity.ToTable("Coupon", "Store", table =>
            {
                table.HasCheckConstraint("CK_Account_Coupon_Discount", "([DiscountPercent]>=(1) AND [DiscountPercent]<=(99))");
                table.HasCheckConstraint("CK_Account_Coupon_UsageLimit", "([UsageLimit]>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(32)")
                .HasMaxLength(32);
            entity.Property(e => e.CouponKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("int");
            entity.Property(e => e.Enabled)
                .HasColumnType("bit");
            entity.Property(e => e.ExpiresOn)
                .HasColumnType("date");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.UsageLimit)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_Coupon_IdProfile");

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_Account_Coupon_Code")
                .IsUnique();

            entity.HasIndex(e => e.CouponKey)
                .HasDatabaseName("UQ_Account_Coupon_CouponKey")
                .IsUnique();

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Coupon_Profile");
        });

        modelBuilder.Entity<AccountFavorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Favorite");

            entity.ToTable("Favorite", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_Favorite_IdProduct");

            entity.HasIndex(e => new { e.IdProfile, e.IdProduct })
                .HasDatabaseName("UQ_Account_Favorite_Profile_Product")
                .IsUnique();

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_Favorite_Product");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_Favorite_Profile");
        });

        modelBuilder.Entity<AccountGender>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Gender");

            entity.ToTable("Gender", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .ValueGeneratedNever();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(40)")
                .HasMaxLength(40);

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_Account_Gender_Code")
                .IsUnique();
        });

        modelBuilder.Entity<AccountMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Message");

            entity.ToTable("Message", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Body)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000);
            entity.Property(e => e.Category)
                .HasColumnType("nvarchar(40)")
                .HasMaxLength(40)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'notice')");
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IsRead)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.ReadAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_Message_IdProfile");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_Message_Profile");
        });

        modelBuilder.Entity<AccountPriceAlert>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PriceAlert");

            entity.ToTable("PriceAlert", "Account", table =>
            {
                table.HasCheckConstraint("CK_Account_PriceAlert_Price", "([PriceAtRegistration]>=(0))");
                table.HasCheckConstraint("CK_Account_PriceAlert_Scope", "([Scope]=N'variant' OR [Scope]=N'all')");
                table.HasCheckConstraint("CK_Account_PriceAlert_Variants", "([VariantsJson] IS NULL OR isjson([VariantsJson])=(1))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.PriceAtRegistration)
                .HasColumnType("decimal(19,0)");
            entity.Property(e => e.Scope)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.StockAtRegistration)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.VariantSignature)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.VariantsJson)
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_PriceAlert_IdProduct");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_PriceAlert_IdProfile");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_PriceAlert_Product");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_PriceAlert_Profile");
        });

        modelBuilder.Entity<AccountProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Profile");

            entity.ToTable("Profile", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email)
                .HasColumnType("nvarchar(254)")
                .HasMaxLength(254);
            entity.Property(e => e.FirstName)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.IdGender)
                .HasColumnType("int");
            entity.Property(e => e.LastName)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.NewsletterEnabled)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Password)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.Phone)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdGender)
                .HasDatabaseName("IX_Profile_IdGender");

            entity.HasIndex(e => e.Email)
                .HasDatabaseName("UQ_Account_Profile_Email")
                .IsUnique();

            entity.HasOne(e => e.Gender)
                .WithMany()
                .HasForeignKey(e => e.IdGender)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Account_Profile_Gender_IdGender");
        });

        modelBuilder.Entity<AccountRecentSearch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_RecentSearch");

            entity.ToTable("RecentSearch", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.SearchKey)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.SearchText)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.SearchedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasIndex(e => new { e.IdProfile, e.SearchKey })
                .HasDatabaseName("UQ_Account_RecentSearch_Profile_Key")
                .IsUnique();

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_RecentSearch_Profile");
        });

        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.HasKey(e => e.Email).HasName("PK_EmailVerificationCode");

            entity.ToTable("EmailVerificationCode", "Account", table =>
            {
                table.HasCheckConstraint("CK_Account_EmailVerificationCode_Attempts", "([Attempts]>=(0) AND [Attempts]<=(5))");
                table.HasCheckConstraint("CK_Account_EmailVerificationCode_LifetimeMinutes", "([LifetimeMinutes]>=(1) AND [LifetimeMinutes]<=(1440))");
            });
            entity.Property(e => e.Email)
                .HasColumnType("nvarchar(254)")
                .HasMaxLength(254)
                .ValueGeneratedNever();
            entity.Property(e => e.Attempts)
                .HasColumnType("int");
            entity.Property(e => e.CodeHash)
                .HasColumnType("varbinary(32)")
                .HasMaxLength(32);
            entity.Property(e => e.ExpiresAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.LifetimeMinutes)
                .HasColumnType("int");
            entity.Property(e => e.SentAtUtc)
                .HasColumnType("datetime2(3)");
        });

        modelBuilder.Entity<StoreCouponUsage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CouponUsage");

            entity.ToTable("CouponUsage", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdCoupon)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdCustomerOrder)
                .HasColumnType("numeric(30,0)");

            entity.HasIndex(e => e.IdCustomerOrder)
                .HasDatabaseName("IX_CouponUsage_IdCustomerOrder");

            entity.HasIndex(e => new { e.IdCoupon, e.IdCustomerOrder })
                .HasDatabaseName("UQ_CouponUsage_Coupon_Order")
                .IsUnique();

            entity.HasOne<AccountCoupon>()
                .WithMany()
                .HasForeignKey(e => e.IdCoupon)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Account_CouponUsage_Coupon");

            entity.HasOne<StoreCustomerOrderReference>()
                .WithMany()
                .HasForeignKey(e => e.IdCustomerOrder)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_CouponUsage_Order");
        });

        modelBuilder.Entity<StoreCustomerOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderItem");

            entity.ToTable("OrderItem", "Store", table =>
            {
                table.HasCheckConstraint("CK_Account_CustomerOrderItem_Quantity", "([Quantity]>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdOrder)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdPriceVersion)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlan)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVariant)
                .HasColumnType("bigint");
            entity.Property(e => e.Quantity)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdOrder)
                .HasDatabaseName("IX_OrderItem_IdOrder");

            entity.HasIndex(e => e.IdPriceVersion)
                .HasDatabaseName("IX_OrderItem_IdPriceVersion");

            entity.HasIndex(e => e.IdSalesPlan)
                .HasDatabaseName("IX_OrderItem_IdSalesPlan");

            entity.HasIndex(e => new { e.IdProduct, e.IdVariant })
                .HasDatabaseName("IX_OrderItem_IdProduct_IdVariant");

            entity.HasOne<StoreCustomerOrderReference>()
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.IdOrder)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Account_CustomerOrderItem_Order");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CustomerOrderItem_Product");

            entity.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(e => new { e.IdProduct, e.IdVariant })
                .HasPrincipalKey(e => new { e.IdProduct, e.Id })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CustomerOrderItem_WarehouseProductVariant");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlan)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_OrderItem_SalesPlan");

            entity.HasOne(e => e.PriceVersion)
                .WithMany()
                .HasForeignKey(e => e.IdPriceVersion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_OrderItem_PriceVersion");
        });

        modelBuilder.Entity<StoreCustomerOrderReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Order");

            entity.ToTable("Order", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_Order_CouponAmounts", "([SubtotalAmount]>=[CouponDiscountAmount])");
                table.HasCheckConstraint("CK_Store_Order_CouponDiscountAmount", "([CouponDiscountAmount]>=(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CouponDiscountAmount)
                .HasColumnType("decimal(19,0)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdAddress)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdBank)
                .HasColumnType("bigint");
            entity.Property(e => e.IdCoupon)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdStatus)
                .HasColumnType("int");
            entity.Property(e => e.IdVoucher)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.ShippingCarrier)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.ShippingTrackingCode)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.SubtotalAmount)
                .HasColumnType("decimal(19,0)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(19,0)");

            entity.HasIndex(e => e.IdAddress)
                .HasDatabaseName("IX_Order_IdAddress");

            entity.HasIndex(e => e.IdBank)
                .HasDatabaseName("IX_Order_IdBank");

            entity.HasIndex(e => e.IdCoupon)
                .HasDatabaseName("IX_Order_IdCoupon");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_Order_IdProfile");

            entity.HasIndex(e => e.IdStatus)
                .HasDatabaseName("IX_Order_IdStatus");

            entity.HasIndex(e => e.IdVoucher)
                .HasDatabaseName("IX_Order_IdVoucher");

            entity.HasOne(e => e.Address)
                .WithMany()
                .HasForeignKey(e => e.IdAddress)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CustomerOrder_Address");

            entity.HasOne<AccountProfile>()
                .WithMany()
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Account_CustomerOrder_Profile");

            entity.HasOne(e => e.StatusReference)
                .WithMany()
                .HasForeignKey(e => e.IdStatus)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Order_OrderStatus");

            entity.HasOne(e => e.Bank)
                .WithMany()
                .HasForeignKey(e => e.IdBank)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_Order_Bank");

            entity.HasOne<AccountCoupon>()
                .WithMany()
                .HasForeignKey(e => e.IdCoupon)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_Order_Coupon");

            entity.HasOne<Voucher>()
                .WithMany()
                .HasForeignKey(e => e.IdVoucher)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_Order_WarehouseDocument");
        });

        modelBuilder.Entity<StorePaymentAttempt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PaymentAttempt");

            entity.ToTable("PaymentAttempt", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_PaymentAttempt_Amount", "([Amount]>(0))");
                table.HasCheckConstraint("CK_Store_PaymentAttempt_Verification", "([IdStatus]<>(5) OR [GatewayReference] IS NOT NULL AND [ReceiptReference] IS NOT NULL AND [PaidAtUtc] IS NOT NULL)");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(19,0)");
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GatewayReference)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.IdOrder)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdStatus)
                .HasColumnType("int");
            entity.Property(e => e.PaidAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.ReceiptReference)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");

            entity.HasIndex(e => e.IdStatus)
                .HasDatabaseName("IX_PaymentAttempt_IdStatus");

            entity.HasIndex(e => e.GatewayReference)
                .HasDatabaseName("UX_Store_PaymentAttempt_GatewayReference")
                .IsUnique()
                .HasFilter("([GatewayReference] IS NOT NULL)");

            entity.HasIndex(e => e.ReceiptReference)
                .HasDatabaseName("UX_Store_PaymentAttempt_ReceiptReference")
                .IsUnique()
                .HasFilter("([ReceiptReference] IS NOT NULL)");

            entity.HasIndex(e => e.IdOrder)
                .HasDatabaseName("UX_Store_PaymentAttempt_VerifiedOrder")
                .IsUnique()
                .HasFilter("([IdStatus]=(5))");

            entity.HasOne(e => e.StatusReference)
                .WithMany()
                .HasForeignKey(e => e.IdStatus)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PaymentAttempt_Status");

            entity.HasOne<StoreCustomerOrderReference>()
                .WithMany()
                .HasForeignKey(e => e.IdOrder)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_PaymentAttempt_Order");
        });

        modelBuilder.Entity<AdminPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Permission");

            entity.ToTable("Permission", "Admin", table =>
            {
                table.HasCheckConstraint("CK_Admin_Permission_Name_NotEmpty", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Action)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.Area)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.Controller)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.FeatureKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.ParentPermissionKey)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.PermissionKey)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160)
                .ValueGeneratedNever();
            entity.Property(e => e.SortOrder)
                .HasColumnType("int")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");

            entity.HasIndex(e => e.ParentPermissionKey)
                .HasDatabaseName("IX_Permission_ParentPermissionKey");

            entity.HasOne<AdminPermission>()
                .WithMany()
                .HasForeignKey(e => e.ParentPermissionKey)
                .HasPrincipalKey(e => e.PermissionKey)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Admin_Permission_Parent");
        });

        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Role");

            entity.ToTable("Role", "Admin", table =>
            {
                table.HasCheckConstraint("CK_Admin_Role_Name_NotEmpty", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.RoleKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.RoleKey)
                .HasDatabaseName("UQ_Admin_Role_RoleKey")
                .IsUnique();
        });

        modelBuilder.Entity<AdminRolePermission>(entity =>
        {
            entity.HasKey(e => new { e.IdRole, e.IdPermission }).HasName("PK_RolePermission");

            entity.ToTable("RolePermission", "Admin");
            entity.Property(e => e.IdRole)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdPermission)
                .HasColumnType("bigint")
                .ValueGeneratedNever();

            entity.HasIndex(e => e.IdPermission)
                .HasDatabaseName("IX_RolePermission_IdPermission");

            entity.HasOne(e => e.Permission)
                .WithMany()
                .HasForeignKey(e => e.IdPermission)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Admin_RolePermission_Permission");

            entity.HasOne(e => e.Role)
                .WithMany(e => e.Permissions)
                .HasForeignKey(e => e.IdRole)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Admin_RolePermission_Role");
        });

        modelBuilder.Entity<AdminSystemUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SystemUser");

            entity.ToTable("SystemUser", "Admin", table =>
            {
                table.HasCheckConstraint("CK_Admin_SystemUser_DisplayName_NotEmpty", "(len(ltrim(rtrim([DisplayName])))>(0))");
                table.HasCheckConstraint("CK_Admin_SystemUser_PasswordHash_NotEmpty", "(len([PasswordHash])>(0))");
                table.HasCheckConstraint("CK_Admin_SystemUser_UserName_NotEmpty", "(len(ltrim(rtrim([UserName])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.LastLoginAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.NormalizedUserName)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.SecurityStamp)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(newid())");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UserName)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.NormalizedUserName)
                .HasDatabaseName("UQ_Admin_SystemUser_NormalizedUserName")
                .IsUnique();
        });

        modelBuilder.Entity<AdminSystemUserWarehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SystemUserWarehouse");

            entity.ToTable("SystemUserWarehouse", "Admin");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.GrantedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdUser)
                .HasColumnType("bigint");
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");

            entity.HasIndex(e => e.IdWarehouse)
                .HasDatabaseName("IX_SystemUserWarehouse_IdWarehouse");

            entity.HasIndex(e => new { e.IdUser, e.IdWarehouse })
                .HasDatabaseName("UQ_Admin_SystemUserWarehouse_User_Warehouse")
                .IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(e => e.Warehouses)
                .HasForeignKey(e => e.IdUser)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Admin_SystemUserWarehouseAccess_User");

            entity.HasOne<WarehouseEntity>()
                .WithMany()
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Admin_SystemUserWarehouseAccess_Warehouse");
        });

        modelBuilder.Entity<AdminUserRole>(entity =>
        {
            entity.HasKey(e => new { e.IdUser, e.IdRole }).HasName("PK_SystemUserRole");

            entity.ToTable("SystemUserRole", "Admin");
            entity.Property(e => e.IdUser)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdRole)
                .HasColumnType("bigint")
                .ValueGeneratedNever();

            entity.HasIndex(e => e.IdRole)
                .HasDatabaseName("IX_SystemUserRole_IdRole");

            entity.HasOne(e => e.Role)
                .WithMany(e => e.Users)
                .HasForeignKey(e => e.IdRole)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Admin_SystemUserRole_Role");

            entity.HasOne(e => e.User)
                .WithMany(e => e.Roles)
                .HasForeignKey(e => e.IdUser)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Admin_SystemUserRole_User");
        });

        modelBuilder.Entity<ArticleFeedbackStatusReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleFeedbackStatus");

            entity.ToTable("ArticleFeedbackStatus", "Account");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_ArticleFeedbackStatus_Code")
                .IsUnique();
        });

        modelBuilder.Entity<ContactMessageStatusReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ContactMessageStatus");

            entity.ToTable("ContactMessageStatus", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_ContactMessageStatus_Code")
                .IsUnique();
        });

        modelBuilder.Entity<GeoCity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_City");

            entity.ToTable("City", "Geo");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdCountry)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdProvince)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IsProvinceCenter)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(12,8)");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(12,8)");
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.SourceCountryName)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);

            entity.HasIndex(e => e.IdCountry)
                .HasDatabaseName("IX_City_IdCountry");

            entity.HasIndex(e => e.IdProvince)
                .HasDatabaseName("IX_City_IdProvince");

            entity.HasOne<GeoCountry>()
                .WithMany()
                .HasForeignKey(e => e.IdCountry)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Geo_City_Country");

            entity.HasOne<GeoProvince>()
                .WithMany()
                .HasForeignKey(e => e.IdProvince)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Geo_City_Province");
        });

        modelBuilder.Entity<GeoCountry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Country");

            entity.ToTable("Country", "Geo");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.CountryCode)
                .HasColumnType("nvarchar(8)")
                .HasMaxLength(8);
            entity.Property(e => e.FlagPath)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);

            entity.HasIndex(e => e.CountryCode)
                .HasDatabaseName("UQ_Geo_Country_CountryCode")
                .IsUnique();
        });

        modelBuilder.Entity<GeoProvince>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Province");

            entity.ToTable("Province", "Geo");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdCountry)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(12,8)");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(12,8)");
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);

            entity.HasIndex(e => new { e.IdCountry, e.NameFa })
                .HasDatabaseName("UQ_Geo_Province_Country_Name")
                .IsUnique();

            entity.HasOne<GeoCountry>()
                .WithMany()
                .HasForeignKey(e => e.IdCountry)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Geo_Province_Country");
        });

        modelBuilder.Entity<MagazineArticle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MagazineArticle");

            entity.ToTable("MagazineArticle", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.ArticleKey)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.Author)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.Conclusion)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.ContentImagePath)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.HeroImagePath)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.IdArea)
                .HasColumnType("bigint");
            entity.Property(e => e.IdBranch)
                .HasColumnType("bigint");
            entity.Property(e => e.IdCategory)
                .HasColumnType("bigint");
            entity.Property(e => e.IdTopic)
                .HasColumnType("bigint");
            entity.Property(e => e.IsPublished)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Kicker)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.Lead)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.PublishedAt)
                .HasColumnType("date");
            entity.Property(e => e.ReadTimeMinutes)
                .HasColumnType("int");
            entity.Property(e => e.ReadTimeText)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.Summary)
                .HasColumnType("nvarchar(1500)")
                .HasMaxLength(1500);
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdArea)
                .HasDatabaseName("IX_MagazineArticle_IdArea");

            entity.HasIndex(e => e.IdBranch)
                .HasDatabaseName("IX_MagazineArticle_IdBranch");

            entity.HasIndex(e => e.IdCategory)
                .HasDatabaseName("IX_MagazineArticle_IdCategory");

            entity.HasIndex(e => e.IdTopic)
                .HasDatabaseName("IX_MagazineArticle_IdTopic");

            entity.HasIndex(e => e.ArticleKey)
                .HasDatabaseName("UQ_Store_MagazineArticle_ArticleKey")
                .IsUnique();

            entity.HasOne(e => e.Area)
                .WithMany()
                .HasForeignKey(e => e.IdArea)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mag_Article_Area");

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.IdBranch)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mag_Article_Branch");

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.IdCategory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mag_Article_Category");

            entity.HasOne(e => e.Topic)
                .WithMany()
                .HasForeignKey(e => e.IdTopic)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mag_Article_Topic");
        });

        modelBuilder.Entity<MagazineArticleArea>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleArea");

            entity.ToTable("ArticleArea", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.AreaKey)
                .HasColumnType("nvarchar(240)")
                .HasMaxLength(240);

            entity.HasIndex(e => e.AreaKey)
                .HasDatabaseName("UQ_Mag_ArticleArea_Key")
                .IsUnique();
        });

        modelBuilder.Entity<MagazineArticleBranch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleBranch");

            entity.ToTable("ArticleBranch", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.BranchKey)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);

            entity.HasIndex(e => e.BranchKey)
                .HasDatabaseName("UQ_Mag_ArticleBranch_Key")
                .IsUnique();
        });

        modelBuilder.Entity<MagazineArticleCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleCategory");

            entity.ToTable("ArticleCategory", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CategoryKey)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);

            entity.HasIndex(e => e.CategoryKey)
                .HasDatabaseName("UQ_Mag_ArticleCategory_Key")
                .IsUnique();
        });

        modelBuilder.Entity<MagazineArticleMetric>(entity =>
        {
            entity.HasKey(e => e.IdMagazineArticle).HasName("PK_MagazineArticleMetric");

            entity.ToTable("MagazineArticleMetric", "Mag");
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)")
                .ValueGeneratedNever();
            entity.Property(e => e.AverageRating)
                .HasColumnType("decimal(5,2)");
            entity.Property(e => e.Comments)
                .HasColumnType("int");
            entity.Property(e => e.Likes)
                .HasColumnType("int");
            entity.Property(e => e.Views)
                .HasColumnType("int");

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Mag_MagazineArticleMetric_Article");
        });

        modelBuilder.Entity<MagazineArticleRelated>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MagazineArticleRelated");

            entity.ToTable("MagazineArticleRelated", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdRelatedArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdMagazineArticle)
                .HasDatabaseName("IX_MagazineArticleRelated_IdMagazineArticle");

            entity.HasIndex(e => e.IdRelatedArticle)
                .HasDatabaseName("IX_MagazineArticleRelated_IdRelatedArticle");

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Mag_MagazineArticleRelated_Article");

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdRelatedArticle)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Mag_Related_Article");
        });

        modelBuilder.Entity<MagazineArticleSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MagazineArticleSection");

            entity.ToTable("MagazineArticleSection", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Body)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.Heading)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdMagazineArticle)
                .HasDatabaseName("IX_MagazineArticleSection_IdMagazineArticle");

            entity.HasOne<MagazineArticle>()
                .WithMany(e => e.Sections)
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_MagazineArticleSection_Article");
        });

        modelBuilder.Entity<MagazineArticleSource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MagazineArticleSource");

            entity.ToTable("MagazineArticleSource", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.Url)
                .HasColumnType("nvarchar(2000)")
                .HasMaxLength(2000);

            entity.HasIndex(e => e.IdMagazineArticle)
                .HasDatabaseName("IX_MagazineArticleSource_IdMagazineArticle");

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_MagazineArticleSource_Article");
        });

        modelBuilder.Entity<MagazineArticleTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_MagazineArticleTag");

            entity.ToTable("MagazineArticleTag", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Filter)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.IdMagazineArticle)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.Label)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdMagazineArticle)
                .HasDatabaseName("IX_MagazineArticleTag_IdMagazineArticle");

            entity.HasOne<MagazineArticle>()
                .WithMany()
                .HasForeignKey(e => e.IdMagazineArticle)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Mag_MagazineArticleTag_Article");
        });

        modelBuilder.Entity<MagazineArticleTopic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ArticleTopic");

            entity.ToTable("ArticleTopic", "Mag");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.TopicKey)
                .HasColumnType("nvarchar(240)")
                .HasMaxLength(240);

            entity.HasIndex(e => e.TopicKey)
                .HasDatabaseName("UQ_Mag_ArticleTopic_Key")
                .IsUnique();
        });

        modelBuilder.Entity<OrderStatusReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderStatus");

            entity.ToTable("OrderStatus", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
        });

        modelBuilder.Entity<PaymentAttemptStatusReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PaymentAttemptStatus");

            entity.ToTable("PaymentAttemptStatus", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_PaymentAttemptStatus_Code")
                .IsUnique();
        });

        modelBuilder.Entity<ProductFeedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductFeedback");

            entity.ToTable("ProductFeedback", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_ProductFeedback_Rating", "([FeedbackType]=N'reviews' AND [Rating] IS NOT NULL AND ([Rating]>=(1) AND [Rating]<=(5)) OR [FeedbackType]=N'questions' AND [Rating] IS NULL)");
                table.HasCheckConstraint("CK_Store_ProductFeedback_Type", "([FeedbackType]=N'questions' OR [FeedbackType]=N'reviews')");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.AdminReply)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.FeedbackText)
                .HasColumnType("nvarchar(800)")
                .HasMaxLength(800);
            entity.Property(e => e.FeedbackType)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProfile)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdStatus)
                .HasColumnType("int");
            entity.Property(e => e.Rating)
                .HasColumnType("tinyint");
            entity.Property(e => e.RepliedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_ProductFeedback_IdProduct");

            entity.HasIndex(e => e.IdProfile)
                .HasDatabaseName("IX_ProductFeedback_IdProfile");

            entity.HasIndex(e => e.IdStatus)
                .HasDatabaseName("IX_ProductFeedback_IdStatus");

            entity.HasOne(e => e.Status)
                .WithMany()
                .HasForeignKey(e => e.IdStatus)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_ProductFeedback_Status");

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_ProductFeedback_Product");

            entity.HasOne(e => e.Profile)
                .WithMany(e => e.ProductFeedback)
                .HasForeignKey(e => e.IdProfile)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Store_ProductFeedback_Profile");
        });

        modelBuilder.Entity<ProductFeedbackStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductFeedbackStatus");

            entity.ToTable("ProductFeedbackStatus", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
        });

        modelBuilder.Entity<StoreAccessory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Accessory");

            entity.ToTable("Accessory", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
        });

        modelBuilder.Entity<StoreAttributeDefinition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_AttributeDefinition");

            entity.ToTable("AttributeDefinition", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.AttributeKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.FilterKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.Icon)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsFilterable)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.IsHighlight)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Label)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.SourceKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.AttributeKey)
                .HasDatabaseName("UQ_Store_AttributeDefinition_AttributeKey")
                .IsUnique();
        });

        modelBuilder.Entity<StoreBank>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Bank");

            entity.ToTable("Bank", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_Bank_Name", "(len(ltrim(rtrim([NameFa])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("varchar(40)")
                .HasMaxLength(40);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.LogoPath)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_Store_Bank_Code")
                .IsUnique();

            entity.HasIndex(e => e.NameFa)
                .HasDatabaseName("UQ_Store_Bank_NameFa")
                .IsUnique();
        });

        modelBuilder.Entity<StoreBrand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Brand");

            entity.ToTable("Brand", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.BrandKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IdCountry)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.LogoPath)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.ShowInStore)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");

            entity.HasIndex(e => e.IdCountry)
                .HasDatabaseName("IX_Brand_IdCountry");

            entity.HasIndex(e => e.BrandKey)
                .HasDatabaseName("UQ_Store_Brand_BrandKey")
                .IsUnique();

            entity.HasOne<GeoCountry>()
                .WithMany()
                .HasForeignKey(e => e.IdCountry)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_Brand_Country");
        });

        modelBuilder.Entity<StoreBrandEditorial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_BrandEditorial");

            entity.ToTable("BrandEditorial", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_BrandEditorial_Json", "(([CompanyIntroductionsJson] IS NULL OR isjson([CompanyIntroductionsJson])=(1)) AND ([ProductFamiliesJson] IS NULL OR isjson([ProductFamiliesJson])=(1)) AND ([StrengthsJson] IS NULL OR isjson([StrengthsJson])=(1)))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Audience)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.CompanyIntroductionsJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.CountryName)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.Established)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.FeaturedProducts)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.FullDocumentText)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.FullPastedText)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.IdBrand)
                .HasColumnType("bigint");
            entity.Property(e => e.OfficialUrl)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.ProductFamiliesJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.ShoppingAdvice)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.Specialization)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.StrengthsJson)
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.IdBrand)
                .HasDatabaseName("UQ_Store_BrandEditorial_IdBrand")
                .IsUnique();

            entity.HasOne<StoreBrand>()
                .WithMany()
                .HasForeignKey(e => e.IdBrand)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_BrandEditorial_Brand");
        });

        modelBuilder.Entity<StoreCatalogFeature>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CatalogFeature");

            entity.ToTable("CatalogFeature", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_CatalogFeature_Kind", "([Kind]=(2) OR [Kind]=(1))");
                table.HasCheckConstraint("CK_Store_CatalogFeature_Name", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Kind)
                .HasColumnType("tinyint")
                .ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);

            entity.HasIndex(e => new { e.Kind, e.Name })
                .HasDatabaseName("UQ_Store_CatalogFeature_Kind_Name")
                .IsUnique();
        });

        modelBuilder.Entity<StoreCatalogFeatureValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CatalogFeatureValue");

            entity.ToTable("CatalogFeatureValue", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_CatalogFeatureValue_Name", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IdFeature)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);

            entity.HasIndex(e => new { e.IdFeature, e.Name })
                .HasDatabaseName("UQ_Store_CatalogFeatureValue_Feature_Name")
                .IsUnique();

            entity.HasOne<StoreCatalogFeature>()
                .WithMany()
                .HasForeignKey(e => e.IdFeature)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_CatalogFeatureValue_Feature");
        });

        modelBuilder.Entity<StoreCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Category");

            entity.ToTable("Category", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_Category_IconClass", "([IconClass] like N'bi-%' AND charindex(N' ',[IconClass])=(0))");
                table.HasCheckConstraint("CK_Store_Category_PathJson", "(isjson([PathJson])=(1))");
                table.HasCheckConstraint("CK_Store_Category_ProductContentJson", "(([ProductHighlightsJson] IS NULL OR isjson([ProductHighlightsJson])=(1)) AND ([ProductSpecsJson] IS NULL OR isjson([ProductSpecsJson])=(1)))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CategoryKey)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.IconClass)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'bi-grid-3x3-gap')");
            entity.Property(e => e.IdParentCategory)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.PathJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProductHighlightsJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProductIntro)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.ProductSpecsJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.TreePathIds)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.TreePathNames)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");

            entity.HasIndex(e => e.IdParentCategory)
                .HasDatabaseName("IX_Category_IdParentCategory");

            entity.HasIndex(e => e.CategoryKey)
                .HasDatabaseName("UQ_Store_Category_CategoryKey")
                .IsUnique();

            entity.HasOne<StoreCategory>()
                .WithMany()
                .HasForeignKey(e => e.IdParentCategory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_Category_Parent");
        });

        modelBuilder.Entity<StoreCategoryCatalogFeature>(entity =>
        {
            entity.HasKey(e => new { e.IdCategory, e.IdFeature }).HasName("PK_CategoryCatalogFeature");

            entity.ToTable("CategoryCatalogFeature", "Store");
            entity.Property(e => e.IdCategory)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdFeature)
                .HasColumnType("bigint")
                .ValueGeneratedNever();

            entity.HasIndex(e => e.IdFeature)
                .HasDatabaseName("IX_CategoryCatalogFeature_IdFeature");

            entity.HasOne<StoreCategory>()
                .WithMany()
                .HasForeignKey(e => e.IdCategory)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_CategoryCatalogFeature_Category");

            entity.HasOne<StoreCatalogFeature>()
                .WithMany()
                .HasForeignKey(e => e.IdFeature)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_CategoryCatalogFeature_Feature");
        });

        modelBuilder.Entity<StoreColor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Color");

            entity.ToTable("Color", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_Color_HexCode", "([HexCode] IS NULL OR [HexCode] like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.ColorKey)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.HexCode)
                .HasColumnType("char(7)")
                .HasMaxLength(7);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.ColorKey)
                .HasDatabaseName("UQ_Store_Color_ColorKey")
                .IsUnique();
        });

        modelBuilder.Entity<StoreContactMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ContactFormMessage");

            entity.ToTable("ContactFormMessage", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.AdminReply)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email)
                .HasColumnType("nvarchar(254)")
                .HasMaxLength(254);
            entity.Property(e => e.IdStatus)
                .HasColumnType("int");
            entity.Property(e => e.Message)
                .HasColumnType("nvarchar(3000)")
                .HasMaxLength(3000);
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.RepliedAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.ReplyEmailSentAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.SubjectKey)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdStatus)
                .HasDatabaseName("IX_ContactFormMessage_IdStatus");

            entity.HasOne(e => e.StatusReference)
                .WithMany()
                .HasForeignKey(e => e.IdStatus)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ContactFormMessage_Status");
        });

        modelBuilder.Entity<StoreContentPage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ContentPage");

            entity.ToTable("ContentPage", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_ContentPage_ContentSource", "([ContentSource]=N'Demo' OR [ContentSource]=N'Database' OR [ContentSource]=N'Razor')");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.ContentSource)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.IsPublished)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.PageKey)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.RoutePath)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.PageKey)
                .HasDatabaseName("UQ_Store_ContentPage_PageKey")
                .IsUnique();

            entity.HasIndex(e => e.RoutePath)
                .HasDatabaseName("UQ_Store_ContentPage_RoutePath")
                .IsUnique();
        });

        modelBuilder.Entity<StoreContentPageSection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ContentPageSection");

            entity.ToTable("ContentPageSection", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_ContentPageSection_DataJson", "([DataJson] IS NULL OR isjson([DataJson])=(1))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Body)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.DataJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.Heading)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.IdContentPage)
                .HasColumnType("bigint");
            entity.Property(e => e.IsPublished)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.SectionKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => new { e.IdContentPage, e.SectionKey })
                .HasDatabaseName("UQ_Store_ContentPageSection_Page_SectionKey")
                .IsUnique();

            entity.HasOne<StoreContentPage>()
                .WithMany(e => e.Sections)
                .HasForeignKey(e => e.IdContentPage)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Store_ContentPageSection_Page");
        });

        modelBuilder.Entity<StoreFaqItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_FaqItem");

            entity.ToTable("FaqItem", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Answer)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.IdContentPage)
                .HasColumnType("bigint");
            entity.Property(e => e.IsPublished)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.LinkText)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.LinkUrl)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.Question)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdContentPage)
                .HasDatabaseName("IX_FaqItem_IdContentPage");

            entity.HasOne<StoreContentPage>()
                .WithMany()
                .HasForeignKey(e => e.IdContentPage)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_FaqItem_Page");
        });

        modelBuilder.Entity<StoreHomeCategoryTab>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HomeCategoryTab");

            entity.ToTable("HomeCategoryTab", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IdCategory)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdCategory)
                .HasDatabaseName("UQ_HomeCategoryTab_Category")
                .IsUnique();

            entity.HasOne<StoreCategory>()
                .WithMany()
                .HasForeignKey(e => e.IdCategory)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HomeCategoryTab_Category");
        });

        modelBuilder.Entity<StoreHomeTestimonial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_HomeTestimonial");

            entity.ToTable("HomeTestimonial", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.DisplayName)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.FeedbackText)
                .HasColumnType("nvarchar(800)")
                .HasMaxLength(800);
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.ImagePath)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit");
            entity.Property(e => e.Rating)
                .HasColumnType("tinyint");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_HomeTestimonial_IdProduct");

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_HomeTestimonial_Independent_Product");
        });

        modelBuilder.Entity<StoreNewsletterSubscriber>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_NewsletterSubscriber");

            entity.ToTable("NewsletterSubscriber", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Email)
                .HasColumnType("nvarchar(254)")
                .HasMaxLength(254);
            entity.Property(e => e.SubscribedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UnsubscribedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.Email)
                .HasDatabaseName("IX_Store_NewsletterSubscriber_Email")
                .IsUnique();
        });

        modelBuilder.Entity<StoreOrderAccessory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_OrderAccessory");

            entity.ToTable("OrderAccessory", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdAccessory)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdOrder)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdPriceVersion)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlan)
                .HasColumnType("bigint");

            entity.HasIndex(e => e.IdAccessory)
                .HasDatabaseName("IX_OrderAccessory_IdAccessory");

            entity.HasIndex(e => e.IdPriceVersion)
                .HasDatabaseName("IX_OrderAccessory_IdPriceVersion");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_OrderAccessory_IdProduct");

            entity.HasIndex(e => e.IdSalesPlan)
                .HasDatabaseName("IX_OrderAccessory_IdSalesPlan");

            entity.HasIndex(e => new { e.IdOrder, e.IdProduct, e.IdSalesPlan, e.IdAccessory, e.IdPriceVersion })
                .HasDatabaseName("UQ_Store_OrderAccessory_Order_Product_Plan_Accessory_PriceVersion")
                .IsUnique();

            entity.HasOne(e => e.Accessory)
                .WithMany()
                .HasForeignKey(e => e.IdAccessory)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_OrderAccessory_Accessory");

            entity.HasOne<StoreCustomerOrderReference>()
                .WithMany()
                .HasForeignKey(e => e.IdOrder)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Store_OrderAccessory_Order");

            entity.HasOne(e => e.PriceVersion)
                .WithMany()
                .HasForeignKey(e => e.IdPriceVersion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_OrderAccessory_PriceVersion");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_OrderAccessory_Product");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlan)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_OrderAccessory_SalesPlan");
        });

        modelBuilder.Entity<StorePriceVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_PriceVersion");

            entity.ToTable("PriceVersion", "Store", table =>
            {
                table.HasCheckConstraint("CK_PriceVersion_AmountByComponent", "([PriceBeforeDiscountAmount]>=(0))");
                table.HasCheckConstraint("CK_Store_PriceVersion_DiscountRange", "([DiscountPercent]>=(0) AND [DiscountPercent]<(100))");
                table.HasCheckConstraint("CK_Store_PriceVersion_SingleTypedReference", "((case when [IdVariant] IS NULL then (0) else (1) end+case when [IdAccessory] IS NULL then (0) else (1) end)<=(1))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.ChangeReason)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DiscountPercent)
                .HasColumnType("decimal(5,2)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.FinalPriceAmount)
                .HasColumnType("decimal(19,0)");
            entity.Property(e => e.IdAccessory)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdChangedBy)
                .HasColumnType("bigint");
            entity.Property(e => e.IdPreviousVersion)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlan)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVariant)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit");
            entity.Property(e => e.PriceBeforeDiscountAmount)
                .HasColumnType("decimal(19,0)");

            entity.HasIndex(e => e.IdAccessory)
                .HasDatabaseName("IX_PriceVersion_IdAccessory");

            entity.HasIndex(e => e.IdChangedBy)
                .HasDatabaseName("IX_PriceVersion_IdChangedBy");

            entity.HasIndex(e => e.IdPreviousVersion)
                .HasDatabaseName("IX_PriceVersion_IdPreviousVersion");

            entity.HasIndex(e => e.IdSalesPlan)
                .HasDatabaseName("IX_PriceVersion_IdSalesPlan");

            entity.HasIndex(e => new { e.IdVariant, e.IdProduct })
                .HasDatabaseName("IX_PriceVersion_IdVariant_IdProduct");

            entity.HasIndex(e => new { e.IdProduct, e.IdSalesPlan, e.IdVariant, e.IdAccessory })
                .HasDatabaseName("UX_Store_PriceVersion_ActiveTypedScope")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.HasOne<AdminSystemUser>()
                .WithMany()
                .HasForeignKey(e => e.IdChangedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_PriceVersion_ChangedByUser");

            entity.HasOne<StoreAccessory>()
                .WithMany()
                .HasForeignKey(e => e.IdAccessory)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_PriceVersion_Accessory");

            entity.HasOne(e => e.PreviousVersion)
                .WithMany()
                .HasForeignKey(e => e.IdPreviousVersion)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_PriceVersion_Previous");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_PriceVersion_Product");

            entity.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(e => new { e.IdVariant, e.IdProduct })
                .HasPrincipalKey(e => new { e.Id, e.IdProduct })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_PriceVersion_ProductVariant_Product");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlan)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_PriceVersion_SalesPlan");
        });

        modelBuilder.Entity<StoreProductAccessory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductAccessory");

            entity.ToTable("ProductAccessory", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_ProductAccessory_DisplayDiscountPercent", "([DisplayDiscountPercent] IS NULL OR [DisplayDiscountPercent]>=(0) AND [DisplayDiscountPercent]<(100))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.DisplayDiscountPercent)
                .HasColumnType("decimal(5,2)");
            entity.Property(e => e.IdAccessory)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IsRequired)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdAccessory)
                .HasDatabaseName("IX_ProductAccessory_IdAccessory");

            entity.HasIndex(e => new { e.IdProduct, e.IdAccessory })
                .HasDatabaseName("UQ_Store_ProductAccessory_Product_Accessory")
                .IsUnique();

            entity.HasOne<StoreAccessory>()
                .WithMany()
                .HasForeignKey(e => e.IdAccessory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductAccessory_Accessory");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductAccessory_Product");
        });

        modelBuilder.Entity<StoreProductAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductAsset");

            entity.ToTable("ProductAsset", "Store", table =>
            {
                table.HasCheckConstraint("CK_ProductAsset_DefaultImage", "([IsDefault]=(0) OR [IdMediaType]=(1))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CardPath)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdMediaType)
                .HasColumnType("int");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IsDefault)
                .HasColumnType("bit");
            entity.Property(e => e.Path)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");
            entity.Property(e => e.ThumbnailPath)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);

            entity.HasIndex(e => e.IdMediaType)
                .HasDatabaseName("IX_ProductAsset_IdMediaType");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("UX_ProductAsset_Default")
                .IsUnique()
                .HasFilter("([IsDefault]=(1))");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProductAsset_Product");

            entity.HasOne<StoreProductMediaType>()
                .WithMany()
                .HasForeignKey(e => e.IdMediaType)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_ProductAsset_Type");
        });

        modelBuilder.Entity<StoreProductBadge>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductBadge");

            entity.ToTable("ProductBadge", "Store", table =>
            {
                table.HasCheckConstraint("CK_ProductBadge_BackgroundColor", "(len([BackgroundColor])=(7) AND left([BackgroundColor],(1))='#' AND NOT (substring([BackgroundColor],(2),(6))) collate Latin1_General_100_BIN2 like '%[^0-9A-Fa-f]%')");
                table.HasCheckConstraint("CK_ProductBadge_BorderColor", "(len([BorderColor])=(7) AND left([BorderColor],(1))='#' AND NOT (substring([BorderColor],(2),(6))) collate Latin1_General_100_BIN2 like '%[^0-9A-Fa-f]%')");
                table.HasCheckConstraint("CK_ProductBadge_TextColor", "(len([TextColor])=(7) AND left([TextColor],(1))='#' AND NOT (substring([TextColor],(2),(6))) collate Latin1_General_100_BIN2 like '%[^0-9A-Fa-f]%')");
                table.HasCheckConstraint("CK_ProductBadge_Title_NotBlank", "(len(ltrim(rtrim([Title])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.BackgroundColor)
                .HasColumnType("varchar(7)")
                .HasMaxLength(7)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("('#102A43')");
            entity.Property(e => e.BorderColor)
                .HasColumnType("varchar(7)")
                .HasMaxLength(7)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("('#FFFFFF')");
            entity.Property(e => e.TextColor)
                .HasColumnType("varchar(7)")
                .HasMaxLength(7)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("('#FFFFFF')");
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);

            entity.HasIndex(e => e.Title)
                .HasDatabaseName("UQ_ProductBadge_Title")
                .IsUnique();
        });

        modelBuilder.Entity<StoreProductBenefit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductBenefit");

            entity.ToTable("ProductBenefit", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.Detail)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.Icon)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_ProductBenefit_IdProduct");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductBenefit_Product");
        });

        modelBuilder.Entity<StoreProductCatalogFeatureValue>(entity =>
        {
            entity.HasKey(e => new { e.IdProduct, e.IdFeature, e.IdFeatureValue }).HasName("PK_ProductCatalogFeatureValue");

            entity.ToTable("ProductCatalogFeatureValue", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_ProductCatalogFeatureValue_Kind", "([Kind]=(2))");
            });
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)")
                .ValueGeneratedNever();
            entity.Property(e => e.IdFeature)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdFeatureValue)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.Kind)
                .HasColumnType("tinyint")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((2))");

            entity.HasIndex(e => new { e.IdFeature, e.Kind })
                .HasDatabaseName("IX_ProductCatalogFeatureValue_IdFeature_Kind");

            entity.HasIndex(e => new { e.IdFeatureValue, e.IdFeature })
                .HasDatabaseName("IX_ProductCatalogFeatureValue_IdFeatureValue_IdFeature");

            entity.HasOne<StoreCatalogFeature>()
                .WithMany()
                .HasForeignKey(e => new { e.IdFeature, e.Kind })
                .HasPrincipalKey(e => new { e.Id, e.Kind })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_ProductCatalogFeatureValue_Feature");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_ProductCatalogFeatureValue_Product");

            entity.HasOne<StoreCatalogFeatureValue>()
                .WithMany()
                .HasForeignKey(e => new { e.IdFeatureValue, e.IdFeature })
                .HasPrincipalKey(e => new { e.Id, e.IdFeature })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_ProductCatalogFeatureValue_Value");
        });

        modelBuilder.Entity<StoreProductCollection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCollection");

            entity.ToTable("ProductCollection", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CollectionKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);

            entity.HasIndex(e => e.CollectionKey)
                .HasDatabaseName("UQ_Store_ProductCollection_CollectionKey")
                .IsUnique();
        });

        modelBuilder.Entity<StoreProductCollectionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductCollectionItem");

            entity.ToTable("ProductCollectionItem", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdCollection)
                .HasColumnType("bigint");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("IX_ProductCollectionItem_IdProduct");

            entity.HasIndex(e => new { e.IdCollection, e.IdProduct })
                .HasDatabaseName("UQ_Store_ProductCollectionItem_Collection_Product")
                .IsUnique();

            entity.HasOne<StoreProductCollection>()
                .WithMany()
                .HasForeignKey(e => e.IdCollection)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductCollectionItem_Collection");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductCollectionItem_Product");
        });

        modelBuilder.Entity<StoreProductColor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductColor");

            entity.ToTable("ProductColor", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdColor)
                .HasColumnType("bigint");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");

            entity.HasIndex(e => e.IdColor)
                .HasDatabaseName("IX_ProductColor_IdColor");

            entity.HasIndex(e => new { e.IdProduct, e.IdColor })
                .HasDatabaseName("UQ_Store_ProductColor_Product_Color")
                .IsUnique();

            entity.HasOne<StoreColor>()
                .WithMany()
                .HasForeignKey(e => e.IdColor)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductColor_Color");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductColor_Product");
        });

        modelBuilder.Entity<StoreProductMediaType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductMediaType");

            entity.ToTable("ProductMediaType", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .ValueGeneratedNever();
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(50)")
                .HasMaxLength(50);

            entity.HasIndex(e => e.Title)
                .HasDatabaseName("UQ_ProductMediaType_Key")
                .IsUnique();
        });

        modelBuilder.Entity<StoreProductSalesPlanOffer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductSalesPlanOffer");

            entity.ToTable("ProductSalesPlanOffer", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_ProductSalesPlanOffer_MaxPurchase_Positive", "([MaxPurchaseQuantity]>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlan)
                .HasColumnType("bigint");
            entity.Property(e => e.IsEnabled)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.MaxPurchaseQuantity)
                .HasColumnType("int");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdSalesPlan)
                .HasDatabaseName("IX_ProductSalesPlanOffer_IdSalesPlan");

            entity.HasIndex(e => new { e.IdProduct, e.IdSalesPlan })
                .HasDatabaseName("UQ_Store_ProductSalesPlanOffer_Product_Plan")
                .IsUnique();

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSalesPlanOffer_Product");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlan)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSalesPlanOffer_SalesPlan");
        });

        modelBuilder.Entity<StoreProductSalesPlanOfferAccessory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductSalesPlanOfferAccessory");

            entity.ToTable("ProductSalesPlanOfferAccessory", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdAccessory)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdOffer)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IsAllowed)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsIncludedByDefault)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");

            entity.HasIndex(e => e.IdAccessory)
                .HasDatabaseName("IX_ProductSalesPlanOfferAccessory_IdAccessory");

            entity.HasIndex(e => new { e.IdOffer, e.IdAccessory })
                .HasDatabaseName("UQ_Store_ProductSalesPlanOfferAccessory_Offer_Accessory")
                .IsUnique();

            entity.HasOne<StoreAccessory>()
                .WithMany()
                .HasForeignKey(e => e.IdAccessory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSalesPlanOfferAccessory_Accessory");

            entity.HasOne<StoreProductSalesPlanOffer>()
                .WithMany()
                .HasForeignKey(e => e.IdOffer)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSalesPlanOfferAccessory_Offer");
        });

        modelBuilder.Entity<StoreProductSalesPlanOfferSpecificationOverride>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductSalesPlanOfferSpecificationOverride");

            entity.ToTable("ProductSalesPlanOfferSpecificationOverride", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IdOffer)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.SpecificationKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.Value)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);

            entity.HasIndex(e => new { e.IdOffer, e.SpecificationKey })
                .HasDatabaseName("UQ_Store_ProductSalesPlanOfferSpecificationOverride_Offer_Key")
                .IsUnique();

            entity.HasOne<StoreProductSalesPlanOffer>()
                .WithMany()
                .HasForeignKey(e => e.IdOffer)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSalesPlanOfferSpecificationOverride_Offer");
        });

        modelBuilder.Entity<StoreProductSpecificationValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductSpecificationValue");

            entity.ToTable("ProductSpecificationValue", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSpecificationFeature)
                .HasColumnType("bigint");
            entity.Property(e => e.Value)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);

            entity.HasIndex(e => e.IdSpecificationFeature)
                .HasDatabaseName("IX_ProductSpecificationValue_IdSpecificationFeature");

            entity.HasIndex(e => new { e.IdProduct, e.IdSpecificationFeature })
                .HasDatabaseName("UQ_Store_ProductSpecificationValue_Product_Feature")
                .IsUnique();

            entity.HasOne<StoreSpecificationFeature>()
                .WithMany()
                .HasForeignKey(e => e.IdSpecificationFeature)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSpecificationValue_Feature");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductSpecificationValue_Product");
        });

        modelBuilder.Entity<StoreProductVariantSalesPlanOffer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductVariantSalesPlanOffer");

            entity.ToTable("ProductVariantSalesPlanOffer", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdSalesPlan)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVariant)
                .HasColumnType("bigint");
            entity.Property(e => e.IsEnabled)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdSalesPlan)
                .HasDatabaseName("IX_ProductVariantSalesPlanOffer_IdSalesPlan");

            entity.HasIndex(e => new { e.IdVariant, e.IdSalesPlan })
                .HasDatabaseName("UQ_Store_ProductVariantSalesPlanOffer_Variant_Plan")
                .IsUnique();

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlan)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductVariantSalesPlanOffer_SalesPlan");

            entity.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(e => e.IdVariant)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_ProductVariantSalesPlanOffer_WarehouseVariant");
        });

        modelBuilder.Entity<StoreSchemaMigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SchemaMigration");

            entity.ToTable("SchemaMigration", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.AppliedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MigrationKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.MigrationKey)
                .HasDatabaseName("UQ_Store_SchemaMigration_MigrationKey")
                .IsUnique();
        });

        modelBuilder.Entity<StoreSiteIdentity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SiteSetting");

            entity.ToTable("SiteSetting", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_SiteSetting_DisplayName", "(len(ltrim(rtrim([DisplayName])))>(0))");
                table.HasCheckConstraint("CK_Store_SiteSetting_Singleton", "([Id]=(1))");
                table.HasCheckConstraint("CK_Store_SiteSetting_StoreName", "(len(ltrim(rtrim([StoreName])))>(0))");
                table.HasCheckConstraint("CK_Store_SiteSetting_VisualBrandName", "(len(ltrim(rtrim([VisualBrandName])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("tinyint")
                .ValueGeneratedNever();
            entity.Property(e => e.DisplayName)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.EmailVerificationCodeLifetimeMinutes)
                .HasColumnType("int")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((15))");
            entity.Property(e => e.FooterDescription)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.FtpHost)
                .HasColumnType("nvarchar(255)")
                .HasMaxLength(255);
            entity.Property(e => e.FtpPassword)
                .HasColumnType("nvarchar(1024)")
                .HasMaxLength(1024);
            entity.Property(e => e.FtpPasswordProtected)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.FtpPort)
                .HasColumnType("int");
            entity.Property(e => e.FtpUsername)
                .HasColumnType("nvarchar(320)")
                .HasMaxLength(320);
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");
            entity.Property(e => e.IdentityUpdatedAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.InstagramUrl)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.LinkedInUrl)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.MediaCdnBaseUrl)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'https://cdn.bs-shop.ir/')");
            entity.Property(e => e.PublicEmail)
                .HasColumnType("nvarchar(254)")
                .HasMaxLength(254);
            entity.Property(e => e.SmtpEnableSsl)
                .HasColumnType("bit");
            entity.Property(e => e.SmtpFrom)
                .HasColumnType("nvarchar(320)")
                .HasMaxLength(320);
            entity.Property(e => e.SmtpHost)
                .HasColumnType("nvarchar(255)")
                .HasMaxLength(255);
            entity.Property(e => e.SmtpPassword)
                .HasColumnType("nvarchar(1024)")
                .HasMaxLength(1024);
            entity.Property(e => e.SmtpPort)
                .HasColumnType("int");
            entity.Property(e => e.SmtpUsername)
                .HasColumnType("nvarchar(320)")
                .HasMaxLength(320);
            entity.Property(e => e.StoreName)
                .HasColumnType("nvarchar(240)")
                .HasMaxLength(240);
            entity.Property(e => e.SupportPhoneDisplay)
                .HasColumnType("nvarchar(32)")
                .HasMaxLength(32);
            entity.Property(e => e.SupportPhoneUri)
                .HasColumnType("nvarchar(40)")
                .HasMaxLength(40);
            entity.Property(e => e.TelegramUrl)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedByUserKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.VisualBrandName)
                .HasColumnType("nvarchar(160)")
                .HasMaxLength(160);
            entity.Property(e => e.WhatsAppNumber)
                .HasColumnType("nvarchar(32)")
                .HasMaxLength(32);

            entity.HasIndex(e => e.IdWarehouse)
                .HasDatabaseName("IX_SiteSetting_IdWarehouse");

            entity.HasOne<WarehouseEntity>()
                .WithMany()
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_SiteSetting_Warehouse");
        });

        modelBuilder.Entity<StoreSlider>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Slider");

            entity.ToTable("Slider", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.ImagePath)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.ShowOnDesktop)
                .HasColumnType("bit");
            entity.Property(e => e.ShowOnMobile)
                .HasColumnType("bit");
            entity.Property(e => e.Title)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.Url)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
        });

        modelBuilder.Entity<StoreSpecificationCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SpecificationCategory");

            entity.ToTable("SpecificationCategory", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CategoryKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IdSpecificationGroup)
                .HasColumnType("bigint");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdSpecificationGroup)
                .HasDatabaseName("IX_SpecificationCategory_IdSpecificationGroup");

            entity.HasIndex(e => e.CategoryKey)
                .HasDatabaseName("UQ_Store_SpecificationCategory_CategoryKey")
                .IsUnique();

            entity.HasOne<StoreSpecificationGroup>()
                .WithMany()
                .HasForeignKey(e => e.IdSpecificationGroup)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_SpecificationCategory_Group");
        });

        modelBuilder.Entity<StoreSpecificationFeature>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SpecificationFeature");

            entity.ToTable("SpecificationFeature", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.FeatureKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IdSpecificationCategory)
                .HasColumnType("bigint");
            entity.Property(e => e.Label)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.IdSpecificationCategory)
                .HasDatabaseName("IX_SpecificationFeature_IdSpecificationCategory");

            entity.HasIndex(e => e.FeatureKey)
                .HasDatabaseName("UQ_Store_SpecificationFeature_FeatureKey")
                .IsUnique();

            entity.HasOne<StoreSpecificationCategory>()
                .WithMany()
                .HasForeignKey(e => e.IdSpecificationCategory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Store_SpecificationFeature_Category");
        });

        modelBuilder.Entity<StoreSpecificationGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SpecificationGroup");

            entity.ToTable("SpecificationGroup", "Store");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.GroupKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int");

            entity.HasIndex(e => e.GroupKey)
                .HasDatabaseName("UQ_Store_SpecificationGroup_GroupKey")
                .IsUnique();
        });

        modelBuilder.Entity<StoreVariantCatalogFeatureValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VariantCatalogFeatureValue");

            entity.ToTable("VariantCatalogFeatureValue", "Store", table =>
            {
                table.HasCheckConstraint("CK_Store_VariantCatalogFeatureValue_Kind", "([Kind]=(1))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdFeature)
                .HasColumnType("bigint");
            entity.Property(e => e.IdFeatureValue)
                .HasColumnType("bigint");
            entity.Property(e => e.IdProductVariant)
                .HasColumnType("bigint");
            entity.Property(e => e.Kind)
                .HasColumnType("tinyint")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");

            entity.HasIndex(e => new { e.IdFeature, e.Kind })
                .HasDatabaseName("IX_VariantCatalogFeatureValue_IdFeature_Kind");

            entity.HasIndex(e => new { e.IdFeatureValue, e.IdFeature })
                .HasDatabaseName("IX_VariantCatalogFeatureValue_IdFeatureValue_IdFeature");

            entity.HasIndex(e => new { e.IdProductVariant, e.IdFeature })
                .HasDatabaseName("UQ_VariantCatalogFeatureValue_Variant_Feature")
                .IsUnique();

            entity.HasOne<StoreCatalogFeature>()
                .WithMany()
                .HasForeignKey(e => new { e.IdFeature, e.Kind })
                .HasPrincipalKey(e => new { e.Id, e.Kind })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_VariantCatalogFeatureValue_Feature");

            entity.HasOne<StoreCatalogFeatureValue>()
                .WithMany()
                .HasForeignKey(e => new { e.IdFeatureValue, e.IdFeature })
                .HasPrincipalKey(e => new { e.Id, e.IdFeature })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_VariantCatalogFeatureValue_Value");

            entity.HasOne<ProductVariant>()
                .WithMany()
                .HasForeignKey(e => e.IdProductVariant)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Store_VariantCatalogFeatureValue_Variant");
        });

        modelBuilder.Entity<VoucherStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherStatus");

            entity.ToTable("VoucherStatus", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("int")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("char(1)")
                .HasMaxLength(1);
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_DocumentStatus_Code")
                .IsUnique();
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Company");

            entity.ToTable("Company", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_Company_Code_NotEmpty", "(len(ltrim(rtrim([Code])))>(0))");
                table.HasCheckConstraint("CK_Warehouse_Company_Name_NotEmpty", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Address)
                .HasColumnType("nvarchar(500)")
                .HasMaxLength(500);
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(50)")
                .HasMaxLength(50);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description)
                .HasColumnType("nvarchar(2000)")
                .HasMaxLength(2000);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.Phone)
                .HasColumnType("nvarchar(50)")
                .HasMaxLength(50);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.Code)
                .HasDatabaseName("UQ_Warehouse_Company_Code")
                .IsUnique();
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_InventoryItem");

            entity.ToTable("InventoryItem", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdVariant)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");

            entity.HasIndex(e => new { e.IdVariant, e.IdProduct })
                .HasDatabaseName("IX_InventoryItem_IdVariant_IdProduct");

            entity.HasIndex(e => new { e.IdProduct, e.IdVariant })
                .HasDatabaseName("UX_Warehouse_InventoryItem_ProductVariant")
                .IsUnique()
                .HasFilter("([IdVariant] IS NOT NULL)");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("UX_Warehouse_InventoryItem_ProductWithoutVariant")
                .IsUnique()
                .HasFilter("([IdVariant] IS NULL)");

            entity.HasOne(e => e.Product)
                .WithMany(e => e.InventoryItems)
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_InventoryItem_Product");

            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => new { e.IdVariant, e.IdProduct })
                .HasPrincipalKey(e => new { e.Id, e.IdProduct })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Warehouse_InventoryItem_ProductVariant");
        });

        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductAttribute");

            entity.ToTable("ProductAttribute", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)")
                .ValueGeneratedNever();
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Key)
                .HasColumnType("varchar(64)")
                .HasMaxLength(64);
            entity.Property(e => e.Label)
                .HasColumnType("nvarchar(80)")
                .HasMaxLength(80);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");

            entity.HasIndex(e => new { e.IdProduct, e.Key })
                .HasDatabaseName("UQ_Warehouse_ProductAttribute_Product_Key")
                .IsUnique();

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Warehouse_ProductAttribute_Product");
        });

        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductAttributeValue");

            entity.ToTable("ProductAttributeValue", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_ProductAttributeValue_Swatch", "([Swatch] IS NULL OR len([Swatch])=(7) AND left([Swatch],(1))='#' AND NOT (substring([Swatch],(2),(6))) collate Latin1_General_100_BIN2 like '%[^0-9A-Fa-f]%')");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.DisplayValue)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)")
                .ValueGeneratedNever();
            entity.Property(e => e.IdProductAttribute)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Label)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.Swatch)
                .HasColumnType("varchar(7)")
                .HasMaxLength(7);
            entity.Property(e => e.ValueKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => new { e.IdProductAttribute, e.IdProduct })
                .HasDatabaseName("IX_ProductAttributeValue_IdProductAttribute_IdProduct");

            entity.HasIndex(e => new { e.IdProductAttribute, e.ValueKey })
                .HasDatabaseName("UQ_Warehouse_ProductAttributeValue_Attribute_Value")
                .IsUnique();

            entity.HasOne(e => e.Attribute)
                .WithMany(e => e.Values)
                .HasForeignKey(e => new { e.IdProductAttribute, e.IdProduct })
                .HasPrincipalKey(e => new { e.Id, e.IdProduct })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Warehouse_ProductAttributeValue_Attribute");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductVariant");

            entity.ToTable("ProductVariant", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)")
                .ValueGeneratedNever();
            entity.Property(e => e.IdVariantName)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsDefault)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdVariantName)
                .HasDatabaseName("IX_ProductVariant_IdVariantName");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("UX_Warehouse_ProductVariant_DefaultPerProduct")
                .IsUnique()
                .HasFilter("([IsDefault]=(1))");

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_ProductVariant_Product");

            entity.HasOne(e => e.VariantName)
                .WithMany()
                .HasForeignKey(e => e.IdVariantName)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Warehouse_ProductVariant_VariantName");
        });

        modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
        {
            entity.HasKey(e => new { e.IdProductVariant, e.IdProductAttribute }).HasName("PK_ProductVariantAttributeValue");

            entity.ToTable("ProductVariantAttributeValue", "Warehouse");
            entity.Property(e => e.IdProductVariant)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdProductAttribute)
                .HasColumnType("bigint")
                .ValueGeneratedNever();
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdProductAttributeValue)
                .HasColumnType("bigint");

            entity.HasIndex(e => new { e.IdProductVariant, e.IdProduct })
                .HasDatabaseName("IX_ProductVariantAttributeValue_IdProductVariant_IdProduct");

            entity.HasIndex(e => new { e.IdProductAttributeValue, e.IdProduct, e.IdProductAttribute })
                .HasDatabaseName("IX_ProductVariantAttributeValue_IdProductAttributeValue_IdProduct_IdProductAttribute");

            entity.HasOne(e => e.AttributeValue)
                .WithMany(e => e.Variants)
                .HasForeignKey(e => new { e.IdProductAttributeValue, e.IdProduct, e.IdProductAttribute })
                .HasPrincipalKey(e => new { e.Id, e.IdProduct, e.IdProductAttribute })
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_ProductVariantAttributeValue_Value");

            entity.HasOne(e => e.ProductVariant)
                .WithMany(e => e.AttributeValues)
                .HasForeignKey(e => new { e.IdProductVariant, e.IdProduct })
                .HasPrincipalKey(e => new { e.Id, e.IdProduct })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Warehouse_ProductVariantAttributeValue_Variant");
        });

        modelBuilder.Entity<SalesPlanReference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SalesPlanReference");

            entity.ToTable("SalesPlanReference", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_SalesPlanReference_Key_NotEmpty", "(len(ltrim(rtrim([StoreSalesPlanKey])))>(0))");
                table.HasCheckConstraint("CK_Warehouse_SalesPlanReference_Name_NotEmpty", "(len(ltrim(rtrim([DisplayName])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(64)")
                .HasMaxLength(64);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DisplayName)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.EndsAtUtc)
                .HasColumnType("datetimeoffset(3)");
            entity.Property(e => e.IdStockOwnerType)
                .HasColumnType("tinyint")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsRegular)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Label)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.PlanType)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.ShowCountdown)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.StartsAtUtc)
                .HasColumnType("datetimeoffset(3)");
            entity.Property(e => e.StoreSalesPlanKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdStockOwnerType)
                .HasDatabaseName("IX_SalesPlanReference_IdStockOwnerType");

            entity.HasIndex(e => e.StoreSalesPlanKey)
                .HasDatabaseName("UQ_Warehouse_SalesPlanReference_StoreSalesPlanKey")
                .IsUnique();

            entity.HasIndex(e => e.IsRegular)
                .HasDatabaseName("UX_Warehouse_SalesPlanReference_OneRegular")
                .IsUnique()
                .HasFilter("([IsRegular]=(1))");

            entity.HasOne<WarehouseStockOwnerType>()
                .WithMany()
                .HasForeignKey(e => e.IdStockOwnerType)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_SalesPlanReference_OwnerType");
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_StockBalance");

            entity.ToTable("StockBalance", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_StockBalance_Minimum_NonNegative", "([MinimumQuantity]>=(0))");
                table.HasCheckConstraint("CK_Warehouse_StockBalance_Quantity_NonNegative", "([Quantity]>=(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdInventoryItem)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlanReference)
                .HasColumnType("bigint");
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");
            entity.Property(e => e.MinimumQuantity)
                .HasColumnType("decimal(18,3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18,3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Version)
                .HasColumnType("timestamp");

            entity.HasIndex(e => e.IdInventoryItem)
                .HasDatabaseName("IX_StockBalance_IdInventoryItem");

            entity.HasIndex(e => e.IdSalesPlanReference)
                .HasDatabaseName("IX_StockBalance_IdSalesPlanReference");

            entity.HasIndex(e => new { e.IdWarehouse, e.IdInventoryItem, e.IdSalesPlanReference })
                .HasDatabaseName("UQ_Warehouse_StockBalance_Key")
                .IsUnique();

            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.IdInventoryItem)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockBalance_InventoryItem");

            entity.HasOne(e => e.SalesPlanReference)
                .WithMany(e => e.StockBalances)
                .HasForeignKey(e => e.IdSalesPlanReference)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockBalance_SalesPlanReference");

            entity.HasOne(e => e.Warehouse)
                .WithMany(e => e.StockBalances)
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockBalance_Warehouse");
        });

        modelBuilder.Entity<VariantName>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VariantName");

            entity.ToTable("VariantName", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_VariantName_Name", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IdParent)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.TreePathIds)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.TreePathNames)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");

            entity.HasIndex(e => e.IdParent)
                .HasDatabaseName("IX_VariantName_IdParent");

            entity.HasOne(e => e.Parent)
                .WithMany()
                .HasForeignKey(e => e.IdParent)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Warehouse_VariantName_Parent");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Voucher");

            entity.ToTable("Voucher", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_WarehouseDocument_CreatedBy_NotEmpty", "(len(ltrim(rtrim([CreatedByUserKey])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedByUserKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.CustomerOrderKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.ExternalReference)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.IdParty)
                .HasColumnType("bigint");
            entity.Property(e => e.IdSalesPlanReference)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVoucherStatus)
                .HasColumnType("int");
            entity.Property(e => e.IdVoucherType)
                .HasColumnType("bigint");
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");
            entity.Property(e => e.Reason)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");
            entity.Property(e => e.UpdatedByUserKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.VoucherDate)
                .HasColumnType("date");
            entity.Property(e => e.VoucherNumber)
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(NEXT VALUE FOR [Warehouse].[VoucherNumberSequence])");

            entity.HasIndex(e => e.IdParty)
                .HasDatabaseName("IX_Voucher_IdParty");

            entity.HasIndex(e => e.IdSalesPlanReference)
                .HasDatabaseName("IX_Voucher_IdSalesPlanReference");

            entity.HasIndex(e => e.IdVoucherStatus)
                .HasDatabaseName("IX_Voucher_IdVoucherStatus");

            entity.HasIndex(e => e.IdVoucherType)
                .HasDatabaseName("IX_Voucher_IdVoucherType");

            entity.HasIndex(e => e.IdWarehouse)
                .HasDatabaseName("IX_Voucher_IdWarehouse");

            entity.HasIndex(e => e.VoucherNumber)
                .HasDatabaseName("UQ_Warehouse_WarehouseDocument_DocumentNumber")
                .IsUnique();

            entity.HasIndex(e => new { e.CustomerOrderKey, e.IdSalesPlanReference })
                .HasDatabaseName("UX_Warehouse_WarehouseDocument_Order_Plan")
                .IsUnique()
                .HasFilter("([CustomerOrderKey] IS NOT NULL)");

            entity.HasOne<VoucherType>()
                .WithMany()
                .HasForeignKey(e => e.IdVoucherType)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocument_DocumentType");

            entity.HasOne<WarehouseParty>()
                .WithMany()
                .HasForeignKey(e => e.IdParty)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocument_Party");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlanReference)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocument_SalesPlanReference");

            entity.HasOne<WarehouseEntity>()
                .WithMany()
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocument_Warehouse");

            entity.HasOne(e => e.StatusReference)
                .WithMany()
                .HasForeignKey(e => e.IdVoucherStatus)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WarehouseDocument_Status");
        });

        modelBuilder.Entity<VoucherAudit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherAudit");

            entity.ToTable("VoucherAudit", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_DocumentAudit_Action", "([Action]=N'Deleted' OR [Action]=N'Updated' OR [Action]=N'Created')");
                table.HasCheckConstraint("CK_Warehouse_DocumentAudit_Snapshots", "(([BeforeJson] IS NULL OR isjson([BeforeJson])=(1)) AND ([AfterJson] IS NULL OR isjson([AfterJson])=(1)))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Action)
                .HasColumnType("nvarchar(20)")
                .HasMaxLength(20);
            entity.Property(e => e.ActorUserKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.AfterJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.BeforeJson)
                .HasColumnType("nvarchar(max)");
            entity.Property(e => e.OccurredAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Reason)
                .HasColumnType("nvarchar(1000)")
                .HasMaxLength(1000);
            entity.Property(e => e.VoucherId)
                .HasColumnType("bigint");
            entity.Property(e => e.VoucherNumber)
                .HasColumnType("bigint");
            entity.Property(e => e.WarehouseId)
                .HasColumnType("bigint");
        });

        modelBuilder.Entity<VoucherLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherLine");

            entity.ToTable("VoucherLine", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_WarehouseDocumentLine_BaseQuantity_Positive", "([BaseQuantity]>(0))");
                table.HasCheckConstraint("CK_Warehouse_WarehouseDocumentLine_ConversionFactor_Positive", "([ConversionFactor]>(0))");
                table.HasCheckConstraint("CK_Warehouse_WarehouseDocumentLine_Quantity_Positive", "([Quantity]>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.BaseQuantity)
                .HasColumnType("decimal(18,3)");
            entity.Property(e => e.ConversionFactor)
                .HasColumnType("decimal(18,9)");
            entity.Property(e => e.IdInventoryItem)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlanReference)
                .HasColumnType("bigint");
            entity.Property(e => e.IdUnit)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVoucher)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.ProductNameSnapshot)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.Quantity)
                .HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitNameSnapshot)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.VariantNameSnapshot)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);

            entity.HasIndex(e => e.IdInventoryItem)
                .HasDatabaseName("IX_VoucherLine_IdInventoryItem");

            entity.HasIndex(e => e.IdSalesPlanReference)
                .HasDatabaseName("IX_VoucherLine_IdSalesPlanReference");

            entity.HasIndex(e => e.IdUnit)
                .HasDatabaseName("IX_VoucherLine_IdUnit");

            entity.HasIndex(e => e.IdVoucher)
                .HasDatabaseName("IX_VoucherLine_IdVoucher");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlanReference)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_DocumentLine_SalesPlan");

            entity.HasOne<Voucher>()
                .WithMany(e => e.Lines)
                .HasForeignKey(e => e.IdVoucher)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocumentLine_Document");

            entity.HasOne<InventoryItem>()
                .WithMany()
                .HasForeignKey(e => e.IdInventoryItem)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocumentLine_InventoryItem");

            entity.HasOne<WarehouseUnit>()
                .WithMany()
                .HasForeignKey(e => e.IdUnit)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseDocumentLine_Unit");
        });

        modelBuilder.Entity<VoucherType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_VoucherType");

            entity.ToTable("VoucherType", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_DocumentType_Direction", "([Direction]='I' OR [Direction]='R')");
                table.HasCheckConstraint("CK_Warehouse_DocumentType_Name_NotEmpty", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.AffectsStock)
                .HasColumnType("bit");
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Direction)
                .HasColumnType("char(1)")
                .HasMaxLength(1);
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsReturn)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.IsSystemSalesDispatch)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(150)")
                .HasMaxLength(150);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IsSystemSalesDispatch)
                .HasDatabaseName("UX_Warehouse_DocumentType_SystemSalesDispatch")
                .IsUnique()
                .HasFilter("([IsSystemSalesDispatch]=(1))");
        });

        modelBuilder.Entity<WarehouseEntity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Warehouse");

            entity.ToTable("Warehouse", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_Warehouse_Code_NotEmpty", "(len(ltrim(rtrim([Code])))>(0))");
                table.HasCheckConstraint("CK_Warehouse_Warehouse_Name_NotEmpty", "(len(ltrim(rtrim([Name])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.Code)
                .HasColumnType("nvarchar(50)")
                .HasMaxLength(50);
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdCompany)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsSalesWarehouse)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => new { e.IdCompany, e.Code })
                .HasDatabaseName("UQ_Warehouse_Warehouse_Company_Code")
                .IsUnique();

            entity.HasIndex(e => new { e.IdCompany, e.Name })
                .HasDatabaseName("UX_Warehouse_Company_Name")
                .IsUnique();

            entity.HasIndex(e => e.IdCompany)
                .HasDatabaseName("UX_Warehouse_Warehouse_OneSalesWarehousePerCompany")
                .IsUnique()
                .HasFilter("([IsSalesWarehouse]=(1))");

            entity.HasOne(e => e.Company)
                .WithMany(e => e.Warehouses)
                .HasForeignKey(e => e.IdCompany)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_Warehouse_Company");
        });

        modelBuilder.Entity<WarehouseParty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Party");

            entity.ToTable("Party", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_Party_Name_NotEmpty", "(len(ltrim(rtrim([Name])))>(0))");
                table.HasCheckConstraint("CK_Warehouse_Party_Type", "([PartyType]=N'Other' OR [PartyType]=N'Warehouse' OR [PartyType]=N'Organization' OR [PartyType]=N'Person')");
                table.HasCheckConstraint("CK_Warehouse_Party_WarehouseLink", "([PartyType]=N'Warehouse' AND [IdWarehouse] IS NOT NULL OR [PartyType]<>N'Warehouse' AND [IdWarehouse] IS NULL)");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ExternalPartyKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.PartyType)
                .HasColumnType("nvarchar(30)")
                .HasMaxLength(30);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.ExternalPartyKey)
                .HasDatabaseName("UX_Warehouse_Party_ExternalKey")
                .IsUnique()
                .HasFilter("([ExternalPartyKey] IS NOT NULL)");

            entity.HasIndex(e => e.IdWarehouse)
                .HasDatabaseName("UX_Warehouse_Party_Warehouse")
                .IsUnique()
                .HasFilter("([IdWarehouse] IS NOT NULL)");

            entity.HasOne<WarehouseEntity>()
                .WithMany()
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_Party_Warehouse");
        });

        modelBuilder.Entity<WarehouseProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Product");

            entity.ToTable("Product", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Product_ProductKey_NotBlank_105", "(len(ltrim(rtrim([ProductKey])))>(0))");
                table.HasCheckConstraint("CK_Warehouse_Product_Name_NotEmpty", "(len(ltrim(rtrim([NameFa])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .ValueGeneratedNever();
            entity.Property(e => e.CreatedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IdBadge)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdBrand)
                .HasColumnType("bigint");
            entity.Property(e => e.IdCategory)
                .HasColumnType("bigint");
            entity.Property(e => e.IsStoreEnabled)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.IsWarehouseEnabled)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.MaxPurchaseQuantity)
                .HasColumnType("int");
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(300)")
                .HasMaxLength(300);
            entity.Property(e => e.ProductKey)
                .HasColumnType("nvarchar(128)")
                .HasMaxLength(128);
            entity.Property(e => e.Slug)
                .HasColumnType("nvarchar(200)")
                .HasMaxLength(200);
            entity.Property(e => e.UpdatedAtUtc)
                .HasColumnType("datetime2(3)");

            entity.HasIndex(e => e.IdBadge)
                .HasDatabaseName("IX_Product_IdBadge");

            entity.HasIndex(e => e.IdBrand)
                .HasDatabaseName("IX_Product_IdBrand");

            entity.HasIndex(e => e.IdCategory)
                .HasDatabaseName("IX_Product_IdCategory");

            entity.HasIndex(e => e.ProductKey)
                .HasDatabaseName("UX_Product_ProductKey_105")
                .IsUnique();

            entity.HasIndex(e => e.ProductKey)
                .HasDatabaseName("UX_Warehouse_Product_ProductKey")
                .IsUnique();

            entity.HasIndex(e => e.Slug)
                .HasDatabaseName("UX_Warehouse_Product_Slug")
                .IsUnique()
                .HasFilter("([Slug] IS NOT NULL)");

            entity.HasOne(e => e.ProductBadge)
                .WithMany()
                .HasForeignKey(e => e.IdBadge)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Product_ProductBadge");

            entity.HasOne<StoreBrand>()
                .WithMany()
                .HasForeignKey(e => e.IdBrand)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_Product_Brand");

            entity.HasOne<StoreCategory>()
                .WithMany()
                .HasForeignKey(e => e.IdCategory)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_Product_Category");
        });

        modelBuilder.Entity<WarehouseProductUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ProductUnit");

            entity.ToTable("ProductUnit", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_ProductUnit_BaseFactor_IsOne", "([IsBaseUnit]=(0) OR [ConversionFactor]=(1))");
                table.HasCheckConstraint("CK_Warehouse_ProductUnit_ConversionFactor_Positive", "([ConversionFactor]>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.ConversionFactor)
                .HasColumnType("decimal(18,9)");
            entity.Property(e => e.IdProduct)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdUnit)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.IsBaseUnit)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");

            entity.HasIndex(e => e.IdUnit)
                .HasDatabaseName("IX_ProductUnit_IdUnit");

            entity.HasIndex(e => new { e.IdProduct, e.IdUnit })
                .HasDatabaseName("UQ_Warehouse_ProductUnit_Product_Unit")
                .IsUnique();

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("UX_ProductUnit_OneActiveBasePerProduct")
                .IsUnique()
                .HasFilter("([IsBaseUnit]=(1) AND [IsActive]=(1))");

            entity.HasIndex(e => e.IdProduct)
                .HasDatabaseName("UX_Warehouse_ProductUnit_OneBaseUnitPerProduct")
                .IsUnique()
                .HasFilter("([IsBaseUnit]=(1))");

            entity.HasOne<WarehouseProduct>()
                .WithMany()
                .HasForeignKey(e => e.IdProduct)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_ProductUnit_Product");

            entity.HasOne<WarehouseUnit>()
                .WithMany()
                .HasForeignKey(e => e.IdUnit)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_ProductUnit_Unit");
        });

        modelBuilder.Entity<WarehouseSchemaMigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SchemaMigration");

            entity.ToTable("SchemaMigration", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.AppliedAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MigrationKey)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);

            entity.HasIndex(e => e.MigrationKey)
                .HasDatabaseName("UQ_Warehouse_SchemaMigration_MigrationKey")
                .IsUnique();
        });

        modelBuilder.Entity<WarehouseStockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_StockMovement");

            entity.ToTable("StockMovement", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_StockMovement_NonZero", "([QuantityDelta]<>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdInventoryItem)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdSalesPlanReference)
                .HasColumnType("bigint");
            entity.Property(e => e.IdVoucher)
                .HasColumnType("numeric(30,0)");
            entity.Property(e => e.IdVoucherLine)
                .HasColumnType("bigint");
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");
            entity.Property(e => e.OccurredAtUtc)
                .HasColumnType("datetime2(3)")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.QuantityDelta)
                .HasColumnType("decimal(18,3)");

            entity.HasIndex(e => e.IdInventoryItem)
                .HasDatabaseName("IX_StockMovement_IdInventoryItem");

            entity.HasIndex(e => e.IdSalesPlanReference)
                .HasDatabaseName("IX_StockMovement_IdSalesPlanReference");

            entity.HasIndex(e => e.IdVoucher)
                .HasDatabaseName("IX_StockMovement_IdVoucher");

            entity.HasIndex(e => e.IdWarehouse)
                .HasDatabaseName("IX_StockMovement_IdWarehouse");

            entity.HasIndex(e => e.IdVoucherLine)
                .HasDatabaseName("UQ_Warehouse_StockMovement_DocumentLine")
                .IsUnique();

            entity.HasOne<Voucher>()
                .WithMany()
                .HasForeignKey(e => e.IdVoucher)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockMovement_Document");

            entity.HasOne<VoucherLine>()
                .WithMany()
                .HasForeignKey(e => e.IdVoucherLine)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockMovement_DocumentLine");

            entity.HasOne<InventoryItem>()
                .WithMany()
                .HasForeignKey(e => e.IdInventoryItem)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockMovement_InventoryItem");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdSalesPlanReference)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockMovement_SalesPlanReference");

            entity.HasOne<WarehouseEntity>()
                .WithMany()
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_StockMovement_Warehouse");
        });

        modelBuilder.Entity<WarehouseStockOwner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_WarehouseStockOwner");

            entity.ToTable("WarehouseStockOwner", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("numeric(30,0)")
                .UseIdentityColumn();
            entity.Property(e => e.IdStockOwner)
                .HasColumnType("bigint");
            entity.Property(e => e.IdWarehouse)
                .HasColumnType("bigint");
            entity.Property(e => e.IsDefault)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");

            entity.HasIndex(e => e.IdStockOwner)
                .HasDatabaseName("IX_WarehouseStockOwner_IdStockOwner");

            entity.HasIndex(e => new { e.IdWarehouse, e.IdStockOwner })
                .HasDatabaseName("UQ_WarehouseStockOwner_Warehouse_StockOwner")
                .IsUnique();

            entity.HasIndex(e => e.IdWarehouse)
                .HasDatabaseName("UX_WarehouseStockOwner_OneDefaultPerWarehouse")
                .IsUnique()
                .HasFilter("([IsDefault]=(1))");

            entity.HasOne<SalesPlanReference>()
                .WithMany()
                .HasForeignKey(e => e.IdStockOwner)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseStockOwner_Owner");

            entity.HasOne<WarehouseEntity>()
                .WithMany()
                .HasForeignKey(e => e.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Warehouse_WarehouseStockOwner_Warehouse");
        });

        modelBuilder.Entity<WarehouseStockOwnerType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_StockOwnerType");

            entity.ToTable("StockOwnerType", "Warehouse");
            entity.Property(e => e.Id)
                .HasColumnType("tinyint")
                .ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.TypeKey)
                .HasColumnType("nvarchar(32)")
                .HasMaxLength(32);

            entity.HasIndex(e => e.TypeKey)
                .HasDatabaseName("UQ_Warehouse_StockOwnerType_Key")
                .IsUnique();
        });

        modelBuilder.Entity<WarehouseUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Unit");

            entity.ToTable("Unit", "Warehouse", table =>
            {
                table.HasCheckConstraint("CK_Warehouse_Unit_NameFa_NotEmpty", "(len(ltrim(rtrim([NameFa])))>(0))");
            });
            entity.Property(e => e.Id)
                .HasColumnType("bigint")
                .UseIdentityColumn();
            entity.Property(e => e.IdParent)
                .HasColumnType("bigint");
            entity.Property(e => e.IsActive)
                .HasColumnType("bit")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((1))");
            entity.Property(e => e.NameEn)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.NameFa)
                .HasColumnType("nvarchar(100)")
                .HasMaxLength(100);
            entity.Property(e => e.SortOrder)
                .HasColumnType("int")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("((0))");
            entity.Property(e => e.TreePathIds)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");
            entity.Property(e => e.TreePathNames)
                .HasColumnType("nvarchar(4000)")
                .HasMaxLength(4000)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("(N'')");

            entity.HasIndex(e => e.IdParent)
                .HasDatabaseName("IX_Unit_IdParent");

            entity.HasOne<WarehouseUnit>()
                .WithMany()
                .HasForeignKey(e => e.IdParent)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Warehouse_Unit_Parent");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
