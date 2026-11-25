using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using WarehouseEntity = Shop.Data.Domain.Warehouse.Entities.Warehouse;

namespace Shop.Data.Infrastructure.Persistence;

public sealed partial class ShopDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDatabaseTables(modelBuilder);
        ConfigureDatabaseRelationships(modelBuilder);
    }

    private static void ConfigureDatabaseTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreSlider>(entity =>
        {
            entity.ToTable("Slider", "Store");
        });

        modelBuilder.Entity<MagazineArticle>(entity =>
        {
            entity.ToTable("MagazineArticle", "Mag");
        });

        modelBuilder.Entity<MagazineArticleSource>(entity =>
        {
            entity.ToTable("MagazineArticleSource", "Mag");
        });

        modelBuilder.Entity<MagazineArticleTag>(entity =>
        {
            entity.ToTable("MagazineArticleTag", "Mag");
        });

        modelBuilder.Entity<MagazineArticleRelated>(entity =>
        {
            entity.ToTable("MagazineArticleRelated", "Mag");
        });

        modelBuilder.Entity<MagazineArticleMetric>(entity =>
        {
            entity.ToTable("MagazineArticleMetric", "Mag");
            entity.HasKey(x => x.IdMagazineArticle);
        });

        modelBuilder.Entity<MagazineArticleCategory>(entity =>
        {
            entity.ToTable("ArticleCategory", "Mag");
        });

        modelBuilder.Entity<MagazineArticleBranch>(entity =>
        {
            entity.ToTable("ArticleBranch", "Mag");
        });

        modelBuilder.Entity<MagazineArticleTopic>(entity =>
        {
            entity.ToTable("ArticleTopic", "Mag");
        });

        modelBuilder.Entity<MagazineArticleArea>(entity =>
        {
            entity.ToTable("ArticleArea", "Mag");
        });

        modelBuilder.Entity<MagazineArticleSection>(entity =>
        {
            entity.ToTable("MagazineArticleSection", "Mag");
        });

        modelBuilder.Entity<AccountArticleInteraction>(entity =>
        {
            entity.ToTable("ArticleInteraction", "Account");
        });

        modelBuilder.Entity<AccountArticleFeedback>(entity =>
        {
            entity.ToTable("ArticleFeedback", "Account");
        });

        modelBuilder.Entity<AccountCart>(entity =>
        {
            entity.ToTable("Cart", "Account");
        });

        modelBuilder.Entity<AccountCartItem>(entity =>
        {
            entity.ToTable("CartItem", "Account");
        });

        modelBuilder.Entity<AccountCartItemAccessory>(entity =>
        {
            entity.ToTable("CartItemAccessory", "Account");
        });

        modelBuilder.Entity<AccountAddress>(entity =>
        {
            entity.ToTable("Address", "Account");
        });

        modelBuilder.Entity<AccountFavorite>(entity =>
        {
            entity.ToTable("Favorite", "Account");
        });

        modelBuilder.Entity<AccountCompareItem>(entity =>
        {
            entity.ToTable("CompareItem", "Account");
        });

        modelBuilder.Entity<AccountPriceAlert>(entity =>
        {
            entity.ToTable("PriceAlert", "Account");
        });

        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.ToTable("EmailVerificationCode", "Account");
            entity.HasKey(x => x.Email);
        });

        modelBuilder.Entity<StoreContentPage>(entity =>
        {
            entity.ToTable("ContentPage", "Store");
        });

        modelBuilder.Entity<StoreContentPageSection>(entity =>
        {
            entity.ToTable("ContentPageSection", "Store");
        });

        modelBuilder.Entity<StoreFaqItem>(entity =>
        {
            entity.ToTable("FaqItem", "Store");
        });

        modelBuilder.Entity<AdminSystemUser>(entity =>
        {
            entity.ToTable("SystemUser", "Admin");
        });

        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.ToTable("Role", "Admin");
        });

        modelBuilder.Entity<AdminPermission>(entity =>
        {
            entity.ToTable("Permission", "Admin");
        });

        modelBuilder.Entity<AdminUserRole>(entity =>
        {
            entity.ToTable("SystemUserRole", "Admin");
            entity.HasKey(x => new { x.IdUser, x.IdRole });
        });

        modelBuilder.Entity<AdminRolePermission>(entity =>
        {
            entity.ToTable("RolePermission", "Admin");
            entity.HasKey(x => new { x.IdRole, x.IdPermission });
        });

        modelBuilder.Entity<AdminUserWarehouseAccess>(entity =>
        {
            entity.ToTable("SystemUserWarehouseAccess", "Admin");
            entity.HasKey(x => new { x.IdUser, x.IdWarehouse });
        });

        modelBuilder.Entity<WarehouseEntity>(entity =>
        {
            entity.ToTable("Warehouse", "Warehouse");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company", "Warehouse");
        });

        modelBuilder.Entity<VoucherType>(entity =>
        {
            entity.ToTable("VoucherType", "Warehouse");
        });

        modelBuilder.Entity<WarehouseUnit>(entity =>
        {
            entity.ToTable("Unit", "Warehouse");
        });

        modelBuilder.Entity<WarehouseParty>(entity =>
        {
            entity.ToTable("Party", "Warehouse");
        });

        modelBuilder.Entity<WarehouseProductUnit>(entity =>
        {
            entity.ToTable("ProductUnit", "Warehouse");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("Voucher", "Warehouse");
        });

        modelBuilder.Entity<VoucherLine>(entity =>
        {
            entity.ToTable("VoucherLine", "Warehouse");
        });

        modelBuilder.Entity<WarehouseStockMovement>(entity =>
        {
            entity.ToTable("StockMovement", "Warehouse");
        });

        modelBuilder.Entity<VoucherAudit>(entity =>
        {
            entity.ToTable("VoucherAudit", "Warehouse");
        });

        modelBuilder.Entity<SalesPlanReference>(entity =>
        {
            entity.ToTable("SalesPlanReference", "Warehouse");
        });

        modelBuilder.Entity<WarehouseStockOwnerType>(entity =>
        {
            entity.ToTable("StockOwnerType", "Warehouse");
        });

        modelBuilder.Entity<WarehouseSchemaMigration>(entity =>
        {
            entity.ToTable("SchemaMigration", "Warehouse");
        });

        modelBuilder.Entity<StoreSchemaMigration>(entity =>
        {
            entity.ToTable("SchemaMigration", "Store");
        });

        modelBuilder.Entity<WarehouseStockOwner>(entity =>
        {
            entity.ToTable("WarehouseStockOwner", "Warehouse");
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("StockBalance", "Warehouse");
            entity.Property(x => x.Version).IsRowVersion();
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItem", "Warehouse");
        });

        modelBuilder.Entity<WarehouseProduct>(entity =>
        {
            entity.ToTable("Product", "Warehouse");
            entity.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<StoreProductBadge>(entity =>
        {
            entity.ToTable("ProductBadge", "Store");
        });

        modelBuilder.Entity<VariantName>(entity =>
        {
            entity.ToTable("VariantName", "Warehouse");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariant", "Warehouse");
        });

        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.ToTable("ProductAttribute", "Warehouse");
        });

        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.ToTable("ProductAttributeValue", "Warehouse");
        });

        modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
        {
            entity.ToTable("ProductVariantAttributeValue", "Warehouse");
            entity.HasKey(x => new { x.IdProductVariant, x.IdProductAttribute });
        });

        modelBuilder.Entity<StoreSiteIdentity>(entity =>
        {
            entity.ToTable("SiteSetting", "Store");
        });

        modelBuilder.Entity<AccountCoupon>(entity =>
        {
            entity.ToTable("Coupon", "Store");
        });

        modelBuilder.Entity<StoreCouponUsage>(entity =>
        {
            entity.ToTable("CouponUsage", "Store");
        });

        modelBuilder.Entity<AccountProfile>(entity =>
        {
            entity.ToTable("Profile", "Account");
        });

        modelBuilder.Entity<AccountMessage>(entity =>
        {
            entity.ToTable("Message", "Account");
        });

        modelBuilder.Entity<AccountGender>(entity =>
        {
            entity.ToTable("Gender", "Account");
        });

        modelBuilder.Entity<StoreOrderPreviousNumber>(entity =>
        {
            entity.ToTable("OrderPreviousNumber", "Store");
            entity.HasKey(x => x.IdOrder);
        });

        modelBuilder.Entity<StoreCustomerOrderReference>(entity =>
        {
            entity.ToTable("Order", "Store");
        });

        modelBuilder.Entity<StoreNewsletterSubscriber>(entity =>
        {
            entity.ToTable("NewsletterSubscriber", "Store");
        });

        modelBuilder.Entity<StoreContactMessage>(entity =>
        {
            entity.ToTable("ContactFormMessage", "Store");
        });

        modelBuilder.Entity<StoreCustomerOrderItem>(entity =>
        {
            entity.ToTable("OrderItem", "Store");
        });

        modelBuilder.Entity<StorePriceVersion>(entity =>
        {
            entity.ToTable("PriceVersion", "Store");
        });

        modelBuilder.Entity<StoreOrderItemAmount>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("OrderItemAmount", "Store");
        });

        modelBuilder.Entity<StoreOrderAccessory>(entity =>
        {
            entity.ToTable("OrderAccessory", "Store");
            entity.HasKey(x => new { x.IdOrder, x.IdProduct, x.IdSalesPlan, x.IdAccessory });
        });

        modelBuilder.Entity<StoreOrderShipmentTrackingHistory>(entity =>
        {
            entity.ToTable("OrderShipmentTrackingHistory", "Store");
        });

        modelBuilder.Entity<StorePaymentAttempt>(entity =>
        {
            entity.ToTable("PaymentAttempt", "Store");
        });

        modelBuilder.Entity<AccountRecentSearch>(entity =>
        {
            entity.ToTable("RecentSearch", "Account");
        });

        modelBuilder.Entity<ProductFeedback>(entity =>
        {
            entity.ToTable("ProductFeedback", "Store");
        });

        modelBuilder.Entity<ProductFeedbackStatus>(entity =>
        {
            entity.ToTable("ProductFeedbackStatus", "Store");
        });

        modelBuilder.Entity<OrderStatusReference>(entity =>
        {
            entity.ToTable("OrderStatus", "Store");
        });

        modelBuilder.Entity<PaymentAttemptStatusReference>(entity =>
        {
            entity.ToTable("PaymentAttemptStatus", "Store");
        });

        modelBuilder.Entity<ContactMessageStatusReference>(entity =>
        {
            entity.ToTable("ContactMessageStatus", "Store");
        });

        modelBuilder.Entity<ArticleFeedbackStatusReference>(entity =>
        {
            entity.ToTable("ArticleFeedbackStatus", "Account");
        });

        modelBuilder.Entity<VoucherStatus>(entity =>
        {
            entity.ToTable("VoucherStatus", "Warehouse");
        });

        modelBuilder.Entity<StoreHomeTestimonial>(entity =>
        {
            entity.ToTable("HomeTestimonial", "Store");
        });

        modelBuilder.Entity<StoreAttributeDefinition>(entity =>
        {
            entity.ToTable("AttributeDefinition", "Store");
        });

        modelBuilder.Entity<StoreProductSalesPlanOffer>(entity =>
        {
            entity.ToTable("ProductSalesPlanOffer", "Store");
        });

        modelBuilder.Entity<StoreProductVariantSalesPlanOffer>(entity =>
        {
            entity.ToTable("ProductVariantSalesPlanOffer", "Store");
        });

        modelBuilder.Entity<StoreAccessory>(entity =>
        {
            entity.ToTable("Accessory", "Store");
        });

        modelBuilder.Entity<StoreProductAccessory>(entity =>
        {
            entity.ToTable("ProductAccessory", "Store");
        });

        modelBuilder.Entity<StoreProductSalesPlanOfferAccessory>(entity =>
        {
            entity.ToTable("ProductSalesPlanOfferAccessory", "Store");
        });

        modelBuilder.Entity<StoreProductSalesPlanOfferSpecificationOverride>(entity =>
        {
            entity.ToTable("ProductSalesPlanOfferSpecificationOverride", "Store");
        });

        modelBuilder.Entity<StoreProductMediaType>(entity =>
        {
            entity.ToTable("ProductMediaType", "Store");
        });

        modelBuilder.Entity<StoreProductAsset>(entity =>
        {
            entity.ToTable("ProductAsset", "Store");
        });

        modelBuilder.Entity<StoreProductBenefit>(entity =>
        {
            entity.ToTable("ProductBenefit", "Store");
        });

        modelBuilder.Entity<StoreColor>(entity =>
        {
            entity.ToTable("Color", "Store");
        });

        modelBuilder.Entity<StoreSpecificationGroup>(entity =>
        {
            entity.ToTable("SpecificationGroup", "Store");
        });

        modelBuilder.Entity<StoreSpecificationCategory>(entity =>
        {
            entity.ToTable("SpecificationCategory", "Store");
        });

        modelBuilder.Entity<StoreSpecificationFeature>(entity =>
        {
            entity.ToTable("SpecificationFeature", "Store");
        });

        modelBuilder.Entity<StoreCatalogFeature>(entity =>
        {
            entity.ToTable("CatalogFeature", "Store");
        });

        modelBuilder.Entity<StoreCatalogFeatureValue>(entity =>
        {
            entity.ToTable("CatalogFeatureValue", "Store");
        });

        modelBuilder.Entity<StoreCategoryCatalogFeature>(entity =>
        {
            entity.ToTable("CategoryCatalogFeature", "Store");
            entity.HasKey(x => new { x.IdCategory, x.IdFeature });
        });

        modelBuilder.Entity<StoreProductCatalogFeatureValue>(entity =>
        {
            entity.ToTable("ProductCatalogFeatureValue", "Store");
            entity.HasKey(x => new { x.IdProduct, x.IdFeature, x.IdFeatureValue });
        });

        modelBuilder.Entity<StoreVariantCatalogFeatureValue>(entity =>
        {
            entity.ToTable("VariantCatalogFeatureValue", "Store");
        });

        modelBuilder.Entity<StoreProductColor>(entity =>
        {
            entity.ToTable("ProductColor", "Store");
        });

        modelBuilder.Entity<StoreProductSpecificationValue>(entity =>
        {
            entity.ToTable("ProductSpecificationValue", "Store");
        });

        modelBuilder.Entity<StoreProductCollection>(entity =>
        {
            entity.ToTable("ProductCollection", "Store");
        });

        modelBuilder.Entity<StoreProductCollectionItem>(entity =>
        {
            entity.ToTable("ProductCollectionItem", "Store");
        });

        modelBuilder.Entity<StoreHomeCategoryTab>(entity =>
        {
            entity.ToTable("HomeCategoryTab", "Store");
        });

        modelBuilder.Entity<StoreBrandEditorial>(entity =>
        {
            entity.ToTable("BrandEditorial", "Store");
        });

        modelBuilder.Entity<StoreBrand>(entity =>
        {
            entity.ToTable("Brand", "Store");
        });

        modelBuilder.Entity<StoreBank>(entity =>
        {
            entity.ToTable("Bank", "Store");
        });

        modelBuilder.Entity<StoreCategory>(entity =>
        {
            entity.ToTable("Category", "Store");
        });

        modelBuilder.Entity<GeoCountry>(entity =>
        {
            entity.ToTable("Country", "Geo");
            entity.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<GeoProvince>(entity =>
        {
            entity.ToTable("Province", "Geo");
            entity.Property(x => x.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<GeoCity>(entity =>
        {
            entity.ToTable("City", "Geo");
            entity.Property(x => x.Id).ValueGeneratedNever();
        });
    }

    private static void ConfigureDatabaseRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreCategory>().HasOne<StoreCategory>().WithMany().HasForeignKey(x => x.IdParentCategory).OnDelete(DeleteBehavior.NoAction); // Store.Category -> Store.Category
        modelBuilder.Entity<Voucher>().HasOne<Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.Warehouse
        modelBuilder.Entity<Voucher>().HasOne<VoucherType>().WithMany().HasForeignKey(x => x.IdVoucherType).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.VoucherType
        modelBuilder.Entity<Voucher>().HasOne<WarehouseParty>().WithMany().HasForeignKey(x => x.IdParty).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.Party
        modelBuilder.Entity<Voucher>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StoreProductSalesPlanOffer>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOffer -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StoreBrand>().HasOne<GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Store.Brand -> Geo.Country
        modelBuilder.Entity<StoreProductSalesPlanOfferSpecificationOverride>().HasOne<StoreProductSalesPlanOffer>().WithMany().HasForeignKey(x => x.IdOffer).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOfferSpecificationOverride -> Store.ProductSalesPlanOffer
        modelBuilder.Entity<StoreSpecificationCategory>().HasOne<StoreSpecificationGroup>().WithMany().HasForeignKey(x => x.IdSpecificationGroup).OnDelete(DeleteBehavior.NoAction); // Store.SpecificationCategory -> Store.SpecificationGroup
        modelBuilder.Entity<StoreProductSalesPlanOfferAccessory>().HasOne<StoreProductSalesPlanOffer>().WithMany().HasForeignKey(x => x.IdOffer).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOfferAccessory -> Store.ProductSalesPlanOffer
        modelBuilder.Entity<GeoProvince>().HasOne<GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Geo.Province -> Geo.Country
        modelBuilder.Entity<StoreSpecificationFeature>().HasOne<StoreSpecificationCategory>().WithMany().HasForeignKey(x => x.IdSpecificationCategory).OnDelete(DeleteBehavior.NoAction); // Store.SpecificationFeature -> Store.SpecificationCategory
        modelBuilder.Entity<StoreProductSalesPlanOfferAccessory>().HasOne<StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOfferAccessory -> Store.Accessory
        modelBuilder.Entity<AccountAddress>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.Address -> Account.Profile
        modelBuilder.Entity<GeoCity>().HasOne<GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Geo.City -> Geo.Country
        modelBuilder.Entity<AccountAddress>().HasOne<GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Account.Address -> Geo.Country
        modelBuilder.Entity<GeoCity>().HasOne<GeoProvince>().WithMany().HasForeignKey(x => x.IdProvince).OnDelete(DeleteBehavior.NoAction); // Geo.City -> Geo.Province
        modelBuilder.Entity<AccountAddress>().HasOne<GeoProvince>().WithMany().HasForeignKey(x => new { x.IdProvince, x.IdCountry }).HasPrincipalKey(x => new { x.Id, x.IdCountry }).OnDelete(DeleteBehavior.NoAction); // Account.Address -> Geo.Province
        modelBuilder.Entity<AccountAddress>().HasOne<GeoCity>().WithMany().HasForeignKey(x => new { x.IdCity, x.IdProvince, x.IdCountry }).HasPrincipalKey(x => new { x.Id, x.IdProvince, x.IdCountry }).OnDelete(DeleteBehavior.NoAction); // Account.Address -> Geo.City
        modelBuilder.Entity<AccountPriceAlert>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.PriceAlert -> Account.Profile
        modelBuilder.Entity<StoreProductColor>().HasOne<StoreColor>().WithMany().HasForeignKey(x => x.IdColor).OnDelete(DeleteBehavior.NoAction); // Store.ProductColor -> Store.Color
        modelBuilder.Entity<StoreCustomerOrderReference>().HasOne<Voucher>().WithMany().HasForeignKey(x => x.IdVoucher).OnDelete(DeleteBehavior.NoAction); // Store.Order -> Warehouse.Voucher
        modelBuilder.Entity<StoreProductCollectionItem>().HasOne<StoreProductCollection>().WithMany().HasForeignKey(x => x.IdCollection).OnDelete(DeleteBehavior.NoAction); // Store.ProductCollectionItem -> Store.ProductCollection
        modelBuilder.Entity<StoreCustomerOrderReference>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.NoAction); // Store.Order -> Account.Profile
        modelBuilder.Entity<StoreCustomerOrderReference>().HasOne<AccountAddress>().WithMany().HasForeignKey(x => x.IdAddress).OnDelete(DeleteBehavior.NoAction); // Store.Order -> Account.Address
        modelBuilder.Entity<StorePaymentAttempt>().HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder).OnDelete(DeleteBehavior.NoAction); // Store.PaymentAttempt -> Store.Order
        modelBuilder.Entity<AccountCartItem>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Account.CartItem -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StoreOrderShipmentTrackingHistory>().HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder).OnDelete(DeleteBehavior.NoAction); // Store.OrderShipmentTrackingHistory -> Store.Order
        modelBuilder.Entity<AccountCartItem>().HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.IdProduct, x.IdVariant }).HasPrincipalKey(x => new { x.IdProduct, x.Id }).OnDelete(DeleteBehavior.NoAction); // Account.CartItem -> Warehouse.ProductVariant
        modelBuilder.Entity<WarehouseStockOwner>().HasOne<Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.WarehouseStockOwner -> Warehouse.Warehouse
        modelBuilder.Entity<WarehouseStockOwner>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdStockOwner).OnDelete(DeleteBehavior.NoAction); // Warehouse.WarehouseStockOwner -> Warehouse.SalesPlanReference
        modelBuilder.Entity<AccountArticleInteraction>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.ArticleInteraction -> Account.Profile
        modelBuilder.Entity<AccountArticleInteraction>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Account.ArticleInteraction -> Mag.MagazineArticle
        modelBuilder.Entity<SalesPlanReference>().HasOne<WarehouseStockOwnerType>().WithMany().HasForeignKey(x => x.IdStockOwnerType).OnDelete(DeleteBehavior.NoAction); // Warehouse.SalesPlanReference -> Warehouse.StockOwnerType
        modelBuilder.Entity<WarehouseProduct>().HasOne<StoreBrand>().WithMany().HasForeignKey(x => x.IdBrand).OnDelete(DeleteBehavior.NoAction); // Warehouse.Product -> Store.Brand
        modelBuilder.Entity<WarehouseProduct>().HasOne<StoreCategory>().WithMany().HasForeignKey(x => x.IdCategory).OnDelete(DeleteBehavior.NoAction); // Warehouse.Product -> Store.Category
        modelBuilder.Entity<MagazineArticleMetric>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle); // Mag.MagazineArticleMetric -> Mag.MagazineArticle
        modelBuilder.Entity<StoreProductSpecificationValue>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductSpecificationValue -> Warehouse.Product
        modelBuilder.Entity<AccountFavorite>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.Favorite -> Warehouse.Product
        modelBuilder.Entity<StoreCustomerOrderItem>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.OrderItem -> Warehouse.Product
        modelBuilder.Entity<AccountCompareItem>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.CompareItem -> Warehouse.Product
        modelBuilder.Entity<AccountArticleFeedback>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.ArticleFeedback -> Account.Profile
        modelBuilder.Entity<AccountArticleFeedback>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Account.ArticleFeedback -> Mag.MagazineArticle
        modelBuilder.Entity<AccountArticleFeedback>().HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus).OnDelete(DeleteBehavior.NoAction); // Account.ArticleFeedback -> Account.ArticleFeedbackStatus
        modelBuilder.Entity<StoreContactMessage>().HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus).OnDelete(DeleteBehavior.NoAction); // Store.ContactFormMessage -> Store.ContactMessageStatus
        modelBuilder.Entity<AccountCartItem>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.CartItem -> Warehouse.Product
        modelBuilder.Entity<StoreProductBenefit>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductBenefit -> Warehouse.Product
        modelBuilder.Entity<StoreProductCollectionItem>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductCollectionItem -> Warehouse.Product
        modelBuilder.Entity<StoreProductColor>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductColor -> Warehouse.Product
        modelBuilder.Entity<MagazineArticleRelated>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle); // Mag.MagazineArticleRelated -> Mag.MagazineArticle
        modelBuilder.Entity<AccountPriceAlert>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.PriceAlert -> Warehouse.Product
        modelBuilder.Entity<Shop.Data.Domain.Admin.Entities.AdminPermission>().HasOne<Shop.Data.Domain.Admin.Entities.AdminPermission>().WithMany().HasForeignKey(x => x.ParentPermissionKey).HasPrincipalKey(x => x.PermissionKey).OnDelete(DeleteBehavior.NoAction); // Admin.Permission -> Admin.Permission
        modelBuilder.Entity<StoreProductSalesPlanOffer>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOffer -> Warehouse.Product
        modelBuilder.Entity<MagazineArticleSource>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticleSource -> Mag.MagazineArticle
        modelBuilder.Entity<MagazineArticleTag>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle); // Mag.MagazineArticleTag -> Mag.MagazineArticle
        modelBuilder.Entity<StoreProductAsset>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductAsset -> Warehouse.Product
        modelBuilder.Entity<Shop.Data.Domain.Admin.Entities.AdminUserWarehouseAccess>().HasOne<Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Admin.SystemUserWarehouseAccess -> Warehouse.Warehouse
        modelBuilder.Entity<StoreProductAccessory>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductAccessory -> Warehouse.Product
        modelBuilder.Entity<VoucherLine>().HasOne<WarehouseUnit>().WithMany().HasForeignKey(x => x.IdUnit).OnDelete(DeleteBehavior.NoAction); // Warehouse.VoucherLine -> Warehouse.Unit
        modelBuilder.Entity<AccountCompareItem>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.CompareItem -> Account.Profile
        modelBuilder.Entity<StoreProductAccessory>().HasOne<StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory).OnDelete(DeleteBehavior.NoAction); // Store.ProductAccessory -> Store.Accessory
        modelBuilder.Entity<VoucherLine>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.VoucherLine -> Warehouse.SalesPlanReference
        modelBuilder.Entity<VoucherLine>().HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.IdInventoryItem).OnDelete(DeleteBehavior.NoAction); // Warehouse.VoucherLine -> Warehouse.InventoryItem
        modelBuilder.Entity<StoreCustomerOrderItem>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Store.OrderItem -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StoreCustomerOrderItem>().HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.IdProduct, x.IdVariant }).HasPrincipalKey(x => new { x.IdProduct, x.Id }).OnDelete(DeleteBehavior.NoAction); // Store.OrderItem -> Warehouse.ProductVariant
        modelBuilder.Entity<WarehouseStockMovement>().HasOne<Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.Warehouse
        modelBuilder.Entity<WarehouseStockMovement>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.SalesPlanReference
        modelBuilder.Entity<WarehouseStockMovement>().HasOne<Voucher>().WithMany().HasForeignKey(x => x.IdVoucher).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.Voucher
        modelBuilder.Entity<WarehouseStockMovement>().HasOne<VoucherLine>().WithMany().HasForeignKey(x => x.IdVoucherLine).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.VoucherLine
        modelBuilder.Entity<WarehouseStockMovement>().HasOne<InventoryItem>().WithMany().HasForeignKey(x => x.IdInventoryItem).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.InventoryItem
        modelBuilder.Entity<AccountFavorite>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.Favorite -> Account.Profile
        modelBuilder.Entity<AccountCart>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.NoAction); // Account.Cart -> Account.Profile
        modelBuilder.Entity<StoreSiteIdentity>().HasOne<Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Store.SiteSetting -> Warehouse.Warehouse
        modelBuilder.Entity<StoreProductVariantSalesPlanOffer>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Store.ProductVariantSalesPlanOffer -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StoreProductVariantSalesPlanOffer>().HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.IdVariant).OnDelete(DeleteBehavior.NoAction); // Store.ProductVariantSalesPlanOffer -> Warehouse.ProductVariant
        modelBuilder.Entity<StoreProductSpecificationValue>().HasOne<StoreSpecificationFeature>().WithMany().HasForeignKey(x => x.IdSpecificationFeature).OnDelete(DeleteBehavior.NoAction); // Store.ProductSpecificationValue -> Store.SpecificationFeature
        modelBuilder.Entity<StoreBrandEditorial>().HasOne<StoreBrand>().WithMany().HasForeignKey(x => x.IdBrand).OnDelete(DeleteBehavior.NoAction); // Store.BrandEditorial -> Store.Brand

        modelBuilder.Entity<MagazineArticle>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.IdCategory).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticle -> Mag.ArticleCategory
        modelBuilder.Entity<MagazineArticle>().HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.IdBranch).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticle -> Mag.ArticleBranch
        modelBuilder.Entity<MagazineArticle>().HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.IdTopic).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticle -> Mag.ArticleTopic
        modelBuilder.Entity<MagazineArticle>().HasOne(x => x.Area).WithMany().HasForeignKey(x => x.IdArea).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticle -> Mag.ArticleArea
        modelBuilder.Entity<MagazineArticle>().HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticle -> Mag.MagazineArticleSection
        modelBuilder.Entity<MagazineArticleRelated>().HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdRelatedArticle).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticleRelated -> Mag.MagazineArticle
        modelBuilder.Entity<AccountCart>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.IdCart); // Account.Cart -> Account.CartItem
        modelBuilder.Entity<AccountCartItemAccessory>().HasOne<StoreProductAccessory>().WithMany().HasForeignKey(x => x.IdProductAccessory).OnDelete(DeleteBehavior.NoAction); // Account.CartItemAccessory -> Store.ProductAccessory
        modelBuilder.Entity<StoreContentPage>().HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.IdContentPage); // Store.ContentPage -> Store.ContentPageSection
        modelBuilder.Entity<StoreFaqItem>().HasOne<StoreContentPage>().WithMany().HasForeignKey(x => x.IdContentPage).OnDelete(DeleteBehavior.NoAction); // Store.FaqItem -> Store.ContentPage
        modelBuilder.Entity<Shop.Data.Domain.Admin.Entities.AdminSystemUser>().HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.IdUser); // Admin.SystemUser -> Admin.SystemUserRole
        modelBuilder.Entity<AdminRole>().HasMany(x => x.Users).WithOne(x => x.Role).HasForeignKey(x => x.IdRole); // Admin.Role -> Admin.SystemUserRole
        modelBuilder.Entity<AdminRolePermission>().HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.IdRole); // Admin.RolePermission -> Admin.Role
        modelBuilder.Entity<AdminRolePermission>().HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.IdPermission); // Admin.RolePermission -> Admin.Permission
        modelBuilder.Entity<WarehouseEntity>().HasOne(x => x.Company).WithMany(x => x.Warehouses).HasForeignKey(x => x.IdCompany).OnDelete(DeleteBehavior.NoAction); // Warehouse.Warehouse -> Warehouse.Company
        modelBuilder.Entity<WarehouseUnit>().HasOne<WarehouseUnit>().WithMany().HasForeignKey(x => x.IdParent).OnDelete(DeleteBehavior.Restrict); // Warehouse.Unit -> Warehouse.Unit
        modelBuilder.Entity<WarehouseParty>().HasOne<WarehouseEntity>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.Party -> Warehouse.Warehouse
        modelBuilder.Entity<WarehouseProductUnit>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Warehouse.ProductUnit -> Warehouse.Product
        modelBuilder.Entity<WarehouseProductUnit>().HasOne<WarehouseUnit>().WithMany().HasForeignKey(x => x.IdUnit).OnDelete(DeleteBehavior.NoAction); // Warehouse.ProductUnit -> Warehouse.Unit
        modelBuilder.Entity<Voucher>().HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdVoucherStatus).OnDelete(DeleteBehavior.Restrict); // Warehouse.Voucher -> Warehouse.VoucherStatus
        modelBuilder.Entity<Voucher>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.IdVoucher).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.VoucherLine
        modelBuilder.Entity<StockBalance>().HasOne(x => x.Warehouse).WithMany(x => x.StockBalances).HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockBalance -> Warehouse.Warehouse
        modelBuilder.Entity<StockBalance>().HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.IdInventoryItem).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockBalance -> Warehouse.InventoryItem
        modelBuilder.Entity<StockBalance>().HasOne(x => x.SalesPlanReference).WithMany(x => x.StockBalances).HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockBalance -> Warehouse.SalesPlanReference
        modelBuilder.Entity<InventoryItem>().HasOne(x => x.Product).WithMany(x => x.InventoryItems).HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Warehouse.InventoryItem -> Warehouse.Product
        modelBuilder.Entity<InventoryItem>().HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => new { x.IdVariant, x.IdProduct }).HasPrincipalKey(x => new { x.Id, x.IdProduct }); // Warehouse.InventoryItem -> Warehouse.ProductVariant
        modelBuilder.Entity<WarehouseProduct>().HasOne(x => x.ProductBadge).WithMany().HasForeignKey(x => x.IdBadge).OnDelete(DeleteBehavior.Restrict); // Warehouse.Product -> Store.ProductBadge
        modelBuilder.Entity<VariantName>().HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.IdParent).OnDelete(DeleteBehavior.Restrict); // Warehouse.VariantName -> Warehouse.VariantName
        modelBuilder.Entity<ProductVariant>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Warehouse.ProductVariant -> Warehouse.Product
        modelBuilder.Entity<ProductVariant>().HasOne(x => x.VariantName).WithMany().HasForeignKey(x => x.IdVariantName).OnDelete(DeleteBehavior.Restrict); // Warehouse.ProductVariant -> Warehouse.VariantName
        modelBuilder.Entity<ProductAttribute>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct); // Warehouse.ProductAttribute -> Warehouse.Product
        modelBuilder.Entity<ProductAttributeValue>().HasOne(x => x.Attribute).WithMany(x => x.Values).HasForeignKey(x => new { x.IdProductAttribute, x.IdProduct }).HasPrincipalKey(x => new { x.Id, x.IdProduct }); // Warehouse.ProductAttributeValue -> Warehouse.ProductAttribute
        modelBuilder.Entity<StoreCouponUsage>().HasOne<AccountCoupon>().WithMany().HasForeignKey(x => x.IdCoupon).OnDelete(DeleteBehavior.Restrict); // Store.CouponUsage -> Store.Coupon
        modelBuilder.Entity<StoreCouponUsage>().HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdCustomerOrder); // Store.CouponUsage -> Store.Order
        modelBuilder.Entity<AccountProfile>().HasOne(x => x.Gender).WithMany().HasForeignKey(x => x.IdGender).OnDelete(DeleteBehavior.Restrict); // Account.Profile -> Account.Gender
        modelBuilder.Entity<AccountMessage>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.Message -> Account.Profile
        modelBuilder.Entity<StoreOrderPreviousNumber>().HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder); // Store.OrderPreviousNumber -> Store.Order
        modelBuilder.Entity<StoreCustomerOrderReference>().HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus).OnDelete(DeleteBehavior.Restrict); // Store.Order -> Store.OrderStatus
        modelBuilder.Entity<StoreCustomerOrderReference>().HasOne<AccountCoupon>().WithMany().HasForeignKey(x => x.IdCoupon).OnDelete(DeleteBehavior.Restrict); // Store.Order -> Store.Coupon
        modelBuilder.Entity<StoreCustomerOrderReference>().HasOne(x => x.Bank).WithMany().HasForeignKey(x => x.IdBank).OnDelete(DeleteBehavior.Restrict); // Store.Order -> Store.Bank
        modelBuilder.Entity<StoreCustomerOrderReference>().HasMany(x => x.Items).WithOne().HasForeignKey(x => x.IdOrder); // Store.Order -> Store.OrderItem
        modelBuilder.Entity<StoreCustomerOrderItem>().HasOne(x => x.PriceVersion).WithMany().HasForeignKey(x => x.IdPriceVersion).OnDelete(DeleteBehavior.Restrict); // Store.OrderItem -> Store.PriceVersion
        modelBuilder.Entity<StorePriceVersion>().HasOne<AdminSystemUser>().WithMany().HasForeignKey(x => x.IdChangedBy).OnDelete(DeleteBehavior.Restrict); // Store.PriceVersion -> Admin.SystemUser
        modelBuilder.Entity<StorePriceVersion>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict); // Store.PriceVersion -> Warehouse.Product
        modelBuilder.Entity<StorePriceVersion>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.Restrict); // Store.PriceVersion -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StorePriceVersion>().HasOne(x => x.PreviousVersion).WithMany().HasForeignKey(x => x.IdPreviousVersion).OnDelete(DeleteBehavior.Restrict); // Store.PriceVersion -> Store.PriceVersion
        modelBuilder.Entity<StorePriceVersion>().HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.IdVariant, x.IdProduct }).HasPrincipalKey(x => new { x.Id, x.IdProduct }).OnDelete(DeleteBehavior.Restrict); // Store.PriceVersion -> Warehouse.ProductVariant
        modelBuilder.Entity<StorePriceVersion>().HasOne<StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory).OnDelete(DeleteBehavior.Restrict); // Store.PriceVersion -> Store.Accessory
        modelBuilder.Entity<StoreOrderAccessory>().HasOne(x => x.PriceVersion).WithMany().HasForeignKey(x => x.IdPriceVersion).OnDelete(DeleteBehavior.Restrict); // Store.OrderAccessory -> Store.PriceVersion
        modelBuilder.Entity<StoreOrderAccessory>().HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder); // Store.OrderAccessory -> Store.Order
        modelBuilder.Entity<StoreOrderAccessory>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict); // Store.OrderAccessory -> Warehouse.Product
        modelBuilder.Entity<StoreOrderAccessory>().HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.Restrict); // Store.OrderAccessory -> Warehouse.SalesPlanReference
        modelBuilder.Entity<StoreOrderAccessory>().HasOne<StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory).OnDelete(DeleteBehavior.Restrict); // Store.OrderAccessory -> Store.Accessory
        modelBuilder.Entity<StorePaymentAttempt>().HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus).OnDelete(DeleteBehavior.Restrict); // Store.PaymentAttempt -> Store.PaymentAttemptStatus
        modelBuilder.Entity<AccountRecentSearch>().HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile); // Account.RecentSearch -> Account.Profile
        modelBuilder.Entity<ProductFeedback>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict); // Store.ProductFeedback -> Warehouse.Product
        modelBuilder.Entity<ProductFeedback>().HasOne(x => x.Status).WithMany().HasForeignKey(x => x.IdStatus).OnDelete(DeleteBehavior.Restrict); // Store.ProductFeedback -> Store.ProductFeedbackStatus
        modelBuilder.Entity<ProductFeedback>().HasOne(x => x.Profile).WithMany(x => x.ProductFeedback).HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.SetNull); // Store.ProductFeedback -> Account.Profile
        modelBuilder.Entity<StoreHomeTestimonial>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict); // Store.HomeTestimonial -> Warehouse.Product
        modelBuilder.Entity<StoreProductAsset>().HasOne<StoreProductMediaType>().WithMany().HasForeignKey(x => x.IdMediaType).OnDelete(DeleteBehavior.NoAction); // Store.ProductAsset -> Store.ProductMediaType
        modelBuilder.Entity<StoreCatalogFeatureValue>().HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => x.IdFeature).OnDelete(DeleteBehavior.Restrict); // Store.CatalogFeatureValue -> Store.CatalogFeature
        modelBuilder.Entity<StoreCategoryCatalogFeature>().HasOne<StoreCategory>().WithMany().HasForeignKey(x => x.IdCategory).OnDelete(DeleteBehavior.Restrict); // Store.CategoryCatalogFeature -> Store.Category
        modelBuilder.Entity<StoreCategoryCatalogFeature>().HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => x.IdFeature).OnDelete(DeleteBehavior.Restrict); // Store.CategoryCatalogFeature -> Store.CatalogFeature
        modelBuilder.Entity<StoreProductCatalogFeatureValue>().HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict); // Store.ProductCatalogFeatureValue -> Warehouse.Product
        modelBuilder.Entity<StoreProductCatalogFeatureValue>().HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => new { x.IdFeature, x.Kind }).HasPrincipalKey(x => new { x.Id, x.Kind }).OnDelete(DeleteBehavior.Restrict); // Store.ProductCatalogFeatureValue -> Store.CatalogFeature
        modelBuilder.Entity<StoreProductCatalogFeatureValue>().HasOne<StoreCatalogFeatureValue>().WithMany().HasForeignKey(x => new { x.IdFeatureValue, x.IdFeature }).HasPrincipalKey(x => new { x.Id, x.IdFeature }).OnDelete(DeleteBehavior.Restrict); // Store.ProductCatalogFeatureValue -> Store.CatalogFeatureValue
        modelBuilder.Entity<StoreVariantCatalogFeatureValue>().HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.IdProductVariant).OnDelete(DeleteBehavior.Restrict); // Store.VariantCatalogFeatureValue -> Warehouse.ProductVariant
        modelBuilder.Entity<StoreVariantCatalogFeatureValue>().HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => new { x.IdFeature, x.Kind }).HasPrincipalKey(x => new { x.Id, x.Kind }).OnDelete(DeleteBehavior.Restrict); // Store.VariantCatalogFeatureValue -> Store.CatalogFeature
        modelBuilder.Entity<StoreVariantCatalogFeatureValue>().HasOne<StoreCatalogFeatureValue>().WithMany().HasForeignKey(x => new { x.IdFeatureValue, x.IdFeature }).HasPrincipalKey(x => new { x.Id, x.IdFeature }).OnDelete(DeleteBehavior.Restrict); // Store.VariantCatalogFeatureValue -> Store.CatalogFeatureValue
        modelBuilder.Entity<StoreHomeCategoryTab>().HasOne<StoreCategory>().WithMany().HasForeignKey(x => x.IdCategory).OnDelete(DeleteBehavior.Restrict); // Store.HomeCategoryTab -> Store.Category
    }
}
