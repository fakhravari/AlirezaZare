using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using WarehouseEntity = Shop.Data.Domain.Warehouse.Entities.Warehouse;

namespace Shop.Data.Infrastructure.Persistence;

public sealed partial class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<AdminSystemUser> AdminSystemUsers => Set<AdminSystemUser>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<AdminPermission> AdminPermissions => Set<AdminPermission>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();
    public DbSet<AdminRolePermission> AdminRolePermissions => Set<AdminRolePermission>();
    public DbSet<AdminUserWarehouseAccess> AdminUserWarehouseAccesses => Set<AdminUserWarehouseAccess>();
    public DbSet<VoucherType> VoucherTypes => Set<VoucherType>();
    public DbSet<WarehouseUnit> WarehouseUnits => Set<WarehouseUnit>();
    public DbSet<WarehouseParty> WarehouseParties => Set<WarehouseParty>();
    public DbSet<WarehouseProductUnit> WarehouseProductUnits => Set<WarehouseProductUnit>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<VoucherLine> VoucherLines => Set<VoucherLine>();
    public DbSet<WarehouseStockMovement> WarehouseStockMovements => Set<WarehouseStockMovement>();
    public DbSet<VoucherAudit> VoucherAudits => Set<VoucherAudit>();
    public DbSet<WarehouseEntity> Warehouses => Set<WarehouseEntity>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<SalesPlanReference> SalesPlanReferences => Set<SalesPlanReference>();
    public DbSet<WarehouseStockOwnerType> WarehouseStockOwnerTypes => Set<WarehouseStockOwnerType>();
    public DbSet<WarehouseSchemaMigration> WarehouseSchemaMigrations => Set<WarehouseSchemaMigration>();
    public DbSet<WarehouseStockOwner> WarehouseStockOwners => Set<WarehouseStockOwner>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantName> VariantNames => Set<VariantName>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues => Set<ProductVariantAttributeValue>();
    public DbSet<WarehouseProduct> Products => Set<WarehouseProduct>();
    public DbSet<StoreSlider> StoreSliders => Set<StoreSlider>();
    public DbSet<StoreSiteIdentity> StoreSiteIdentities => Set<StoreSiteIdentity>();
    public DbSet<MagazineArticle> MagazineArticles => Set<MagazineArticle>();
    public DbSet<MagazineArticleCategory> MagazineArticleCategories => Set<MagazineArticleCategory>();
    public DbSet<MagazineArticleBranch> MagazineArticleBranches => Set<MagazineArticleBranch>();
    public DbSet<MagazineArticleTopic> MagazineArticleTopics => Set<MagazineArticleTopic>();
    public DbSet<MagazineArticleArea> MagazineArticleAreas => Set<MagazineArticleArea>();
    public DbSet<MagazineArticleSection> MagazineArticleSections => Set<MagazineArticleSection>();
    public DbSet<MagazineArticleSource> MagazineArticleSources => Set<MagazineArticleSource>();
    public DbSet<MagazineArticleTag> MagazineArticleTags => Set<MagazineArticleTag>();
    public DbSet<MagazineArticleRelated> MagazineArticleRelated => Set<MagazineArticleRelated>();
    public DbSet<MagazineArticleMetric> MagazineArticleMetrics => Set<MagazineArticleMetric>();
    public DbSet<AccountArticleInteraction> AccountArticleInteractions => Set<AccountArticleInteraction>();
    public DbSet<StoreContentPage> StoreContentPages => Set<StoreContentPage>();
    public DbSet<StoreContentPageSection> StoreContentPageSections => Set<StoreContentPageSection>();
    public DbSet<StoreFaqItem> StoreFaqItems => Set<StoreFaqItem>();
    public DbSet<ProductFeedback> ProductFeedback => Set<ProductFeedback>();
    public DbSet<ProductFeedbackStatus> ProductFeedbackStatuses => Set<ProductFeedbackStatus>();
    public DbSet<StoreHomeTestimonial> StoreHomeTestimonials => Set<StoreHomeTestimonial>();
    public DbSet<StoreAttributeDefinition> StoreAttributeDefinitions => Set<StoreAttributeDefinition>();
    public DbSet<StoreProductMediaType> StoreProductMediaTypes => Set<StoreProductMediaType>();
    public DbSet<StoreProductAsset> StoreProductAssets => Set<StoreProductAsset>();
    public DbSet<StoreProductBenefit> StoreProductBenefits => Set<StoreProductBenefit>();
    public DbSet<StoreProductBadge> StoreProductBadges => Set<StoreProductBadge>();
    public DbSet<StoreBrand> StoreBrands => Set<StoreBrand>();
    public DbSet<StoreBank> StoreBanks => Set<StoreBank>();
    public DbSet<StoreBrandEditorial> StoreBrandEditorials => Set<StoreBrandEditorial>();
    public DbSet<StoreColor> StoreColors => Set<StoreColor>();
    public DbSet<StoreSpecificationGroup> StoreSpecificationGroups => Set<StoreSpecificationGroup>();
    public DbSet<StoreSpecificationCategory> StoreSpecificationCategories => Set<StoreSpecificationCategory>();
    public DbSet<StoreSpecificationFeature> StoreSpecificationFeatures => Set<StoreSpecificationFeature>();
    public DbSet<StoreCatalogFeature> StoreCatalogFeatures => Set<StoreCatalogFeature>();
    public DbSet<StoreCatalogFeatureValue> StoreCatalogFeatureValues => Set<StoreCatalogFeatureValue>();
    public DbSet<StoreCategoryCatalogFeature> StoreCategoryCatalogFeatures => Set<StoreCategoryCatalogFeature>();

    public DbSet<StoreProductCatalogFeatureValue> StoreProductCatalogFeatureValues =>
        Set<StoreProductCatalogFeatureValue>();

    public DbSet<StoreVariantCatalogFeatureValue> StoreVariantCatalogFeatureValues =>
        Set<StoreVariantCatalogFeatureValue>();

    public DbSet<StoreProductColor> StoreProductColors => Set<StoreProductColor>();

    public DbSet<StoreProductSpecificationValue> StoreProductSpecificationValues =>
        Set<StoreProductSpecificationValue>();

    public DbSet<StoreProductCollection> StoreProductCollections => Set<StoreProductCollection>();
    public DbSet<StoreProductCollectionItem> StoreProductCollectionItems => Set<StoreProductCollectionItem>();
    public DbSet<StoreHomeCategoryTab> StoreHomeCategoryTabs => Set<StoreHomeCategoryTab>();
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<StoreProductSalesPlanOffer> StoreProductSalesPlanOffers => Set<StoreProductSalesPlanOffer>();

    public DbSet<StoreProductVariantSalesPlanOffer> StoreProductVariantSalesPlanOffers =>
        Set<StoreProductVariantSalesPlanOffer>();

    public DbSet<StoreAccessory> StoreAccessories => Set<StoreAccessory>();
    public DbSet<StoreProductAccessory> StoreProductAccessories => Set<StoreProductAccessory>();

    public DbSet<StoreProductSalesPlanOfferAccessory> StoreProductSalesPlanOfferAccessories =>
        Set<StoreProductSalesPlanOfferAccessory>();

    public DbSet<StoreProductSalesPlanOfferSpecificationOverride> StoreProductSalesPlanOfferSpecificationOverrides =>
        Set<StoreProductSalesPlanOfferSpecificationOverride>();

    public DbSet<StorePriceVersion> StorePriceVersions => Set<StorePriceVersion>();
    public DbSet<StoreOrderAccessory> StoreOrderAccessories => Set<StoreOrderAccessory>();
    public DbSet<StoreOrderItemAmount> StoreOrderItemAmounts => Set<StoreOrderItemAmount>();
    public DbSet<GeoCountry> GeoCountries => Set<GeoCountry>();
    public DbSet<GeoProvince> GeoProvinces => Set<GeoProvince>();
    public DbSet<GeoCity> GeoCities => Set<GeoCity>();
    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<AccountMessage> AccountMessages => Set<AccountMessage>();
    public DbSet<AccountGender> AccountGenders => Set<AccountGender>();
    public DbSet<AccountCoupon> AccountCoupons => Set<AccountCoupon>();
    public DbSet<StoreCouponUsage> StoreCouponUsages => Set<StoreCouponUsage>();
    public DbSet<AccountRecentSearch> AccountRecentSearches => Set<AccountRecentSearch>();
    public DbSet<AccountCart> AccountCarts => Set<AccountCart>();
    public DbSet<AccountCartItem> AccountCartItems => Set<AccountCartItem>();
    public DbSet<AccountCartItemAccessory> AccountCartItemAccessories => Set<AccountCartItemAccessory>();
    public DbSet<AccountAddress> AccountAddresses => Set<AccountAddress>();
    public DbSet<AccountFavorite> AccountFavorites => Set<AccountFavorite>();
    public DbSet<AccountCompareItem> AccountCompareItems => Set<AccountCompareItem>();
    public DbSet<AccountPriceAlert> AccountPriceAlerts => Set<AccountPriceAlert>();
    public DbSet<AccountArticleFeedback> AccountArticleFeedback => Set<AccountArticleFeedback>();
    public DbSet<StoreCustomerOrderItem> StoreCustomerOrderItems => Set<StoreCustomerOrderItem>();
    public DbSet<StorePaymentAttempt> StorePaymentAttempts => Set<StorePaymentAttempt>();
    public DbSet<StoreCustomerOrderReference> StoreCustomerOrders => Set<StoreCustomerOrderReference>();
    public DbSet<OrderStatusReference> OrderStatuses => Set<OrderStatusReference>();
    public DbSet<PaymentAttemptStatusReference> PaymentAttemptStatuses => Set<PaymentAttemptStatusReference>();
    public DbSet<ContactMessageStatusReference> ContactMessageStatuses => Set<ContactMessageStatusReference>();
    public DbSet<ArticleFeedbackStatusReference> ArticleFeedbackStatuses => Set<ArticleFeedbackStatusReference>();
    public DbSet<VoucherStatus> VoucherStatuses => Set<VoucherStatus>();
    public DbSet<StoreOrderPreviousNumber> StoreOrderPreviousNumbers => Set<StoreOrderPreviousNumber>();
    public DbSet<StoreNewsletterSubscriber> StoreNewsletterSubscribers => Set<StoreNewsletterSubscriber>();
    public DbSet<StoreContactMessage> StoreContactMessages => Set<StoreContactMessage>();

    public DbSet<StoreOrderShipmentTrackingHistory> StoreOrderShipmentTrackingHistories =>
        Set<StoreOrderShipmentTrackingHistory>();

    public DbSet<StoreSchemaMigration> StoreSchemaMigrations => Set<StoreSchemaMigration>();
}
