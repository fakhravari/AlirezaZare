using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;
using WarehouseEntity = Shop.Data.Domain.Warehouse.Entities.Warehouse;

namespace Shop.Data.Infrastructure.Persistence;

public sealed partial class ShopDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoreSlider>(entity =>
        {
            entity.ToTable("Slider", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<MagazineArticle>(entity =>
        {
            entity.ToTable("MagazineArticle", "Mag");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ArticleKey).IsUnique();
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.IdCategory)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.IdBranch)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.IdTopic)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.IdArea)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.IdMagazineArticle)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<MagazineArticleSource>(entity =>
        {
            entity.ToTable("MagazineArticleSource", "Mag");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<MagazineArticleTag>(entity =>
        {
            entity.ToTable("MagazineArticleTag", "Mag");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<MagazineArticleRelated>(entity =>
        {
            entity.ToTable("MagazineArticleRelated", "Mag");
            entity.HasKey(x => x.Id);
            entity.HasOne<MagazineArticle>().WithMany().HasForeignKey(x => x.IdRelatedArticle)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<MagazineArticleMetric>(entity =>
        {
            entity.ToTable("MagazineArticleMetric", "Mag");
            entity.HasKey(x => x.IdMagazineArticle);
        });
        modelBuilder.Entity<MagazineArticleCategory>(entity =>
        {
            entity.ToTable("ArticleCategory", "Mag");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CategoryKey).IsUnique();
        });
        modelBuilder.Entity<MagazineArticleBranch>(entity =>
        {
            entity.ToTable("ArticleBranch", "Mag");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.BranchKey).IsUnique();
        });
        modelBuilder.Entity<MagazineArticleTopic>(entity =>
        {
            entity.ToTable("ArticleTopic", "Mag");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TopicKey).IsUnique();
        });
        modelBuilder.Entity<MagazineArticleArea>(entity =>
        {
            entity.ToTable("ArticleArea", "Mag");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AreaKey).IsUnique();
        });
        modelBuilder.Entity<MagazineArticleSection>(entity =>
        {
            entity.ToTable("MagazineArticleSection", "Mag");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<AccountArticleInteraction>(entity =>
        {
            entity.ToTable("ArticleInteraction", "Account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProfile, x.IdMagazineArticle, x.InteractionType }).IsUnique();
        });
        modelBuilder.Entity<AccountArticleFeedback>(entity =>
        {
            entity.ToTable("ArticleFeedback", "Account");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AccountCart>(entity =>
        {
            entity.ToTable("Cart", "Account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IdProfile).IsUnique();
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.IdCart).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AccountCartItem>(entity =>
        {
            entity.ToTable("CartItem", "Account");
            entity.HasKey(x => x.Id);
            entity.HasMany(x => x.Accessories).WithOne().HasForeignKey(x => x.IdCartItem)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AccountCartItemAccessory>(entity =>
        {
            entity.ToTable("CartItemAccessory", "Account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdCartItem, x.IdProductAccessory }).IsUnique();
            entity.HasOne<StoreProductAccessory>().WithMany().HasForeignKey(x => x.IdProductAccessory)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<AccountAddress>(entity =>
        {
            entity.ToTable("Address", "Account");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<AccountFavorite>(entity =>
        {
            entity.ToTable("Favorite", "Account");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<AccountCompareItem>(entity =>
        {
            entity.ToTable("CompareItem", "Account");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<AccountPriceAlert>(entity =>
        {
            entity.ToTable("PriceAlert", "Account");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.ToTable("EmailVerificationCode", "Account");
            entity.HasKey(x => x.Email);
        });
        modelBuilder.Entity<StoreContentPage>(entity =>
        {
            entity.ToTable("ContentPage", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PageKey).IsUnique();
            entity.HasIndex(x => x.RoutePath).IsUnique();
            entity.HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.IdContentPage)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StoreContentPageSection>(entity =>
        {
            entity.ToTable("ContentPageSection", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdContentPage, x.SectionKey }).IsUnique();
        });
        modelBuilder.Entity<StoreFaqItem>(entity =>
        {
            entity.ToTable("FaqItem", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne<StoreContentPage>().WithMany().HasForeignKey(x => x.IdContentPage)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<AdminSystemUser>(entity =>
        {
            entity.ToTable("SystemUser", "Admin");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NormalizedUserName).IsUnique();
            entity.HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.IdUser);
            entity.HasMany(x => x.WarehouseAccess).WithOne(x => x.User).HasForeignKey(x => x.IdUser);
        });
        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.ToTable("Role", "Admin");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.RoleKey).IsUnique();
            entity.HasMany(x => x.Users).WithOne(x => x.Role).HasForeignKey(x => x.IdRole);
            entity.HasMany(x => x.Permissions).WithOne(x => x.Role).HasForeignKey(x => x.IdRole);
        });
        modelBuilder.Entity<AdminPermission>(entity =>
        {
            entity.ToTable("Permission", "Admin");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.PermissionKey).IsUnique();
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
            entity.HasOne(x => x.Role).WithMany(x => x.Permissions).HasForeignKey(x => x.IdRole);
            entity.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.IdPermission);
        });
        modelBuilder.Entity<AdminUserWarehouseAccess>(entity =>
        {
            entity.ToTable("SystemUserWarehouseAccess", "Admin");
            entity.HasKey(x => new { x.IdUser, x.IdWarehouse });
        });
        modelBuilder.Entity<WarehouseEntity>(entity =>
        {
            entity.ToTable("Warehouse", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Company).WithMany(x => x.Warehouses).HasForeignKey(x => x.IdCompany)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.IdCompany, x.Name }).IsUnique().HasDatabaseName("UX_Warehouse_Company_Name");
        });
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<VoucherType>(entity =>
        {
            entity.ToTable("VoucherType", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<WarehouseUnit>(entity =>
        {
            entity.ToTable("Unit", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne<WarehouseUnit>().WithMany().HasForeignKey(x => x.IdParent).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<WarehouseParty>(entity =>
        {
            entity.ToTable("Party", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne<WarehouseEntity>().WithMany().HasForeignKey(x => x.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<WarehouseProductUnit>(entity =>
        {
            entity.ToTable("ProductUnit", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProduct, x.IdUnit }).IsUnique();
            entity.HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WarehouseUnit>().WithMany().HasForeignKey(x => x.IdUnit)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("Voucher", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdVoucherStatus)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.CustomerOrderKey, x.IdSalesPlanReference }).IsUnique()
                .HasFilter("[CustomerOrderKey] IS NOT NULL");
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.IdVoucher)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<VoucherLine>(entity =>
        {
            entity.ToTable("VoucherLine", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<WarehouseStockMovement>(entity =>
        {
            entity.ToTable("StockMovement", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<VoucherAudit>(entity =>
        {
            entity.ToTable("VoucherAudit", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<SalesPlanReference>(entity =>
        {
            entity.ToTable("SalesPlanReference", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<WarehouseStockOwnerType>(entity =>
        {
            entity.ToTable("StockOwnerType", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TypeKey).IsUnique();
        });
        modelBuilder.Entity<WarehouseSchemaMigration>(entity =>
        {
            entity.ToTable("SchemaMigration", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MigrationKey).IsUnique();
        });
        modelBuilder.Entity<StoreSchemaMigration>(entity =>
        {
            entity.ToTable("SchemaMigration", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MigrationKey).IsUnique();
        });
        modelBuilder.Entity<WarehouseStockOwner>(entity =>
        {
            entity.ToTable("WarehouseStockOwner", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.IdWarehouse, x.IdStockOwner })
                .HasName("UQ_WarehouseStockOwner_Warehouse_StockOwner");
            entity.HasIndex(x => x.IdWarehouse).IsUnique().HasFilter("[IsDefault] = 1")
                .HasDatabaseName("UX_WarehouseStockOwner_OneDefaultPerWarehouse");
        });
        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("StockBalance", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Version).IsRowVersion();
            entity.HasIndex(x => new { x.IdWarehouse, x.IdInventoryItem, x.IdSalesPlanReference }).IsUnique();
            entity.HasOne(x => x.Warehouse).WithMany(x => x.StockBalances).HasForeignKey(x => x.IdWarehouse)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.IdInventoryItem)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.SalesPlanReference).WithMany(x => x.StockBalances)
                .HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItem", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Product).WithMany(x => x.InventoryItems).HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => new { x.IdVariant, x.IdProduct })
                .HasPrincipalKey(x => new { x.Id, x.IdProduct });
        });
        modelBuilder.Entity<WarehouseProduct>(entity =>
        {
            entity.ToTable("Product", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.ProductBadge).WithMany().HasForeignKey(x => x.IdBadge)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreProductBadge>(entity =>
        {
            entity.ToTable("ProductBadge", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Title).IsUnique();
        });
        modelBuilder.Entity<VariantName>(entity =>
        {
            entity.ToTable("VariantName", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.IdParent).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariant", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.IdProduct });
            entity.HasAlternateKey(x => new { x.IdProduct, x.Id });
            entity.HasIndex(x => x.IdProduct).IsUnique().HasFilter("[IsDefault] = 1");
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.VariantName).WithMany().HasForeignKey(x => x.IdVariantName)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.ToTable("ProductAttribute", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProduct, x.Key }).IsUnique();
            entity.HasAlternateKey(x => new { x.Id, x.IdProduct });
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.ToTable("ProductAttributeValue", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProductAttribute, x.ValueKey }).IsUnique();
            entity.HasAlternateKey(x => new { x.Id, x.IdProduct, x.IdProductAttribute });
            entity.HasOne(x => x.Attribute).WithMany(x => x.Values)
                .HasForeignKey(x => new { x.IdProductAttribute, x.IdProduct })
                .HasPrincipalKey(x => new { x.Id, x.IdProduct }).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
        {
            entity.ToTable("ProductVariantAttributeValue", "Warehouse");
            entity.HasKey(x => new { x.IdProductVariant, x.IdProductAttribute });
            entity.HasOne(x => x.ProductVariant).WithMany(x => x.AttributeValues)
                .HasForeignKey(x => new { x.IdProductVariant, x.IdProduct })
                .HasPrincipalKey(x => new { x.Id, x.IdProduct }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AttributeValue).WithMany(x => x.Variants)
                .HasForeignKey(x => new { x.IdProductAttributeValue, x.IdProduct, x.IdProductAttribute })
                .HasPrincipalKey(x => new { x.Id, x.IdProduct, x.IdProductAttribute })
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreSiteIdentity>(entity =>
        {
            entity.ToTable("SiteSetting", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<AccountCoupon>(entity =>
        {
            entity.ToTable("Coupon", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CouponKey).IsUnique();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreCouponUsage>(entity =>
        {
            entity.ToTable("CouponUsage", "Store");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.IdCoupon, x.IdCustomerOrder }).HasName("UQ_CouponUsage_Coupon_Order");
            entity.HasOne<AccountCoupon>().WithMany().HasForeignKey(x => x.IdCoupon).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdCustomerOrder)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AccountProfile>(entity =>
        {
            entity.ToTable("Profile", "Account");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Gender).WithMany().HasForeignKey(x => x.IdGender).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AccountMessage>(entity =>
        {
            entity.ToTable("Message", "Account");
            entity.HasKey(x => x.Id);
            entity.HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AccountGender>(entity =>
        {
            entity.ToTable("Gender", "Account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<StoreOrderPreviousNumber>(entity =>
        {
            entity.ToTable("OrderPreviousNumber", "Store");
            entity.HasKey(x => x.IdOrder);
            entity.HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StoreCustomerOrderReference>(entity =>
        {
            entity.ToTable("Order", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AccountCoupon>().WithMany().HasForeignKey(x => x.IdCoupon).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Bank).WithMany().HasForeignKey(x => x.IdBank).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.IdOrder).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StoreNewsletterSubscriber>(entity =>
        {
            entity.ToTable("NewsletterSubscriber", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Email).IsUnique();
        });
        modelBuilder.Entity<StoreContactMessage>(entity =>
        {
            entity.ToTable("ContactFormMessage", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreCustomerOrderItem>(entity =>
        {
            entity.ToTable("OrderItem", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.PriceVersion).WithMany().HasForeignKey(x => x.IdPriceVersion)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StorePriceVersion>(entity =>
        {
            entity.ToTable("PriceVersion", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne<AdminSystemUser>().WithMany().HasForeignKey(x => x.IdChangedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PreviousVersion).WithMany().HasForeignKey(x => x.IdPreviousVersion)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.IdVariant, x.IdProduct })
                .HasPrincipalKey(x => new { x.Id, x.IdProduct }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory)
                .OnDelete(DeleteBehavior.Restrict);
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
            entity.HasOne(x => x.PriceVersion).WithMany().HasForeignKey(x => x.IdPriceVersion)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreOrderShipmentTrackingHistory>(entity =>
        {
            entity.ToTable("OrderShipmentTrackingHistory", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<StorePaymentAttempt>(entity =>
        {
            entity.ToTable("PaymentAttempt", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.StatusReference).WithMany().HasForeignKey(x => x.IdStatus)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AccountRecentSearch>(entity =>
        {
            entity.ToTable("RecentSearch", "Account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProfile, x.SearchKey }).IsUnique();
            entity.HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProductFeedback>(entity =>
        {
            entity.ToTable("ProductFeedback", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.IdStatus).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profile).WithMany(x => x.ProductFeedback).HasForeignKey(x => x.IdProfile)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ProductFeedbackStatus>(entity =>
        {
            entity.ToTable("ProductFeedbackStatus", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<OrderStatusReference>(entity =>
        {
            entity.ToTable("OrderStatus", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<PaymentAttemptStatusReference>(entity =>
        {
            entity.ToTable("PaymentAttemptStatus", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<ContactMessageStatusReference>(entity =>
        {
            entity.ToTable("ContactMessageStatus", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<ArticleFeedbackStatusReference>(entity =>
        {
            entity.ToTable("ArticleFeedbackStatus", "Account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<VoucherStatus>(entity =>
        {
            entity.ToTable("VoucherStatus", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<StoreHomeTestimonial>(entity =>
        {
            entity.ToTable("HomeTestimonial", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreAttributeDefinition>(entity =>
        {
            entity.ToTable("AttributeDefinition", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.AttributeKey).IsUnique();
        });
        modelBuilder.Entity<StoreProductSalesPlanOffer>(entity =>
        {
            entity.ToTable("ProductSalesPlanOffer", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProduct, x.IdSalesPlan }).IsUnique();
        });
        modelBuilder.Entity<StoreProductVariantSalesPlanOffer>(entity =>
        {
            entity.ToTable("ProductVariantSalesPlanOffer", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdVariant, x.IdSalesPlan }).IsUnique();
        });
        modelBuilder.Entity<StoreAccessory>(entity =>
        {
            entity.ToTable("Accessory", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<StoreProductAccessory>(entity =>
        {
            entity.ToTable("ProductAccessory", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProduct, x.IdAccessory }).IsUnique();
        });
        modelBuilder.Entity<StoreProductSalesPlanOfferAccessory>(entity =>
        {
            entity.ToTable("ProductSalesPlanOfferAccessory", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdOffer, x.IdAccessory }).IsUnique();
        });
        modelBuilder.Entity<StoreProductSalesPlanOfferSpecificationOverride>(entity =>
        {
            entity.ToTable("ProductSalesPlanOfferSpecificationOverride", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdOffer, x.SpecificationKey }).IsUnique();
        });
        modelBuilder.Entity<StoreProductMediaType>(entity =>
        {
            entity.ToTable("ProductMediaType", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Title).IsUnique();
        });
        modelBuilder.Entity<StoreProductAsset>(entity =>
        {
            entity.ToTable("ProductAsset", "Store");
            entity.HasKey(x => x.Id);
            entity.HasOne<StoreProductMediaType>().WithMany().HasForeignKey(x => x.IdMediaType)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<StoreProductBenefit>(entity =>
        {
            entity.ToTable("ProductBenefit", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<StoreColor>(entity =>
        {
            entity.ToTable("Color", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ColorKey).IsUnique();
        });
        modelBuilder.Entity<StoreSpecificationGroup>(entity =>
        {
            entity.ToTable("SpecificationGroup", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.GroupKey).IsUnique();
        });
        modelBuilder.Entity<StoreSpecificationCategory>(entity =>
        {
            entity.ToTable("SpecificationCategory", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CategoryKey).IsUnique();
        });
        modelBuilder.Entity<StoreSpecificationFeature>(entity =>
        {
            entity.ToTable("SpecificationFeature", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.FeatureKey).IsUnique();
        });
        modelBuilder.Entity<StoreCatalogFeature>(entity =>
        {
            entity.ToTable("CatalogFeature", "Store");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.Kind });
            entity.HasIndex(x => new { x.Kind, x.Name }).IsUnique();
        });
        modelBuilder.Entity<StoreCatalogFeatureValue>(entity =>
        {
            entity.ToTable("CatalogFeatureValue", "Store");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.IdFeature });
            entity.HasIndex(x => new { x.IdFeature, x.Name }).IsUnique();
            entity.HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => x.IdFeature)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreCategoryCatalogFeature>(entity =>
        {
            entity.ToTable("CategoryCatalogFeature", "Store");
            entity.HasKey(x => new { x.IdCategory, x.IdFeature });
            entity.HasOne<StoreCategory>().WithMany().HasForeignKey(x => x.IdCategory)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => x.IdFeature)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreProductCatalogFeatureValue>(entity =>
        {
            entity.ToTable("ProductCatalogFeatureValue", "Store");
            entity.HasKey(x => new { x.IdProduct, x.IdFeature, x.IdFeatureValue });
            entity.HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => new { x.IdFeature, x.Kind })
                .HasPrincipalKey(x => new { x.Id, x.Kind }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCatalogFeatureValue>().WithMany()
                .HasForeignKey(x => new { x.IdFeatureValue, x.IdFeature })
                .HasPrincipalKey(x => new { x.Id, x.IdFeature }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreVariantCatalogFeatureValue>(entity =>
        {
            entity.ToTable("VariantCatalogFeatureValue", "Store");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.IdProductVariant, x.IdFeature })
                .HasName("UQ_VariantCatalogFeatureValue_Variant_Feature");
            entity.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.IdProductVariant)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCatalogFeature>().WithMany().HasForeignKey(x => new { x.IdFeature, x.Kind })
                .HasPrincipalKey(x => new { x.Id, x.Kind }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<StoreCatalogFeatureValue>().WithMany()
                .HasForeignKey(x => new { x.IdFeatureValue, x.IdFeature })
                .HasPrincipalKey(x => new { x.Id, x.IdFeature }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreProductColor>(entity =>
        {
            entity.ToTable("ProductColor", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProduct, x.IdColor }).IsUnique();
        });
        modelBuilder.Entity<StoreProductSpecificationValue>(entity =>
        {
            entity.ToTable("ProductSpecificationValue", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdProduct, x.IdSpecificationFeature }).IsUnique();
        });
        modelBuilder.Entity<StoreProductCollection>(entity =>
        {
            entity.ToTable("ProductCollection", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CollectionKey).IsUnique();
        });
        modelBuilder.Entity<StoreProductCollectionItem>(entity =>
        {
            entity.ToTable("ProductCollectionItem", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdCollection, x.IdProduct }).IsUnique();
        });
        modelBuilder.Entity<StoreHomeCategoryTab>(entity =>
        {
            entity.ToTable("HomeCategoryTab", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IdCategory).IsUnique();
            entity.HasOne<StoreCategory>().WithMany().HasForeignKey(x => x.IdCategory)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StoreBrandEditorial>(entity =>
        {
            entity.ToTable("BrandEditorial", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.IdBrand).IsUnique();
        });
        modelBuilder.Entity<StoreBrand>(entity =>
        {
            entity.ToTable("Brand", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.BrandKey).IsUnique();
        });
        modelBuilder.Entity<StoreBank>(entity =>
        {
            entity.ToTable("Bank", "Store");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<StoreCategory>(entity =>
        {
            entity.ToTable("Category", "Store");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CategoryKey).IsUnique();
        });
        modelBuilder.Entity<GeoCountry>(entity =>
        {
            entity.ToTable("Country", "Geo");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CountryCode).IsUnique();
        });
        modelBuilder.Entity<GeoProvince>(entity =>
        {
            entity.ToTable("Province", "Geo");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<GeoCity>(entity =>
        {
            entity.ToTable("City", "Geo");
            entity.HasKey(x => x.Id);
        });
        ConfigureDatabaseForeignKeys(modelBuilder);
        ConfigureDatabaseUniqueIndexes(modelBuilder);
    }

    private static void ConfigureDatabaseUniqueIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountCartItem>().HasIndex(x => new { x.IdCart, x.IdProduct, x.IdSalesPlan, x.IdVariant })
            .IsUnique().HasFilter(null).HasDatabaseName("UQ_Account_CartItem_Configuration");
        modelBuilder.Entity<AccountCompareItem>().HasIndex(x => new { x.IdProfile, x.IdProduct })
            .IsUnique().HasDatabaseName("UQ_Account_CompareItem_Profile_Product");
        modelBuilder.Entity<AccountFavorite>().HasIndex(x => new { x.IdProfile, x.IdProduct })
            .IsUnique().HasDatabaseName("UQ_Account_Favorite_Profile_Product");
        modelBuilder.Entity<AccountProfile>().HasIndex(x => x.Email)
            .IsUnique().HasDatabaseName("UQ_Account_Profile_Email");
        modelBuilder.Entity<GeoProvince>().HasIndex(x => new { x.IdCountry, x.NameFa })
            .IsUnique().HasDatabaseName("UQ_Geo_Province_Country_Name");
        modelBuilder.Entity<StoreBank>().HasIndex(x => x.Code)
            .IsUnique().HasDatabaseName("UQ_Store_Bank_Code");
        modelBuilder.Entity<StoreBank>().HasIndex(x => x.NameFa)
            .IsUnique().HasDatabaseName("UQ_Store_Bank_NameFa");
        modelBuilder.Entity<StoreCustomerOrderReference>().HasIndex(x => x.OrderNumber)
            .IsUnique().HasDatabaseName("UQ_Account_CustomerOrder_Number");
        modelBuilder.Entity<StorePaymentAttempt>().HasIndex(x => x.GatewayReference)
            .IsUnique().HasFilter("([GatewayReference] IS NOT NULL)")
            .HasDatabaseName("UX_Store_PaymentAttempt_GatewayReference");
        modelBuilder.Entity<StorePaymentAttempt>().HasIndex(x => x.ReceiptReference)
            .IsUnique().HasFilter("([ReceiptReference] IS NOT NULL)")
            .HasDatabaseName("UX_Store_PaymentAttempt_ReceiptReference");
        modelBuilder.Entity<StorePaymentAttempt>().HasIndex(x => x.IdOrder)
            .IsUnique().HasFilter("([IdStatus]=(5))")
            .HasDatabaseName("UX_Store_PaymentAttempt_VerifiedOrder");
        modelBuilder.Entity<StorePriceVersion>().HasIndex(x => new { x.IdProduct, x.IdSalesPlan, x.IdVariant, x.IdAccessory })
            .IsUnique().HasFilter("([IsActive]=(1))")
            .HasDatabaseName("UX_Store_PriceVersion_ActiveTypedScope");
        modelBuilder.Entity<StoreProductAsset>().HasIndex(x => x.IdProduct)
            .IsUnique().HasFilter("([IsDefault]=(1))")
            .HasDatabaseName("UX_ProductAsset_Default");
        modelBuilder.Entity<Company>().HasIndex(x => x.Code)
            .IsUnique().HasDatabaseName("UQ_Warehouse_Company_Code");
        modelBuilder.Entity<InventoryItem>().HasIndex(x => new { x.IdProduct, x.IdVariant })
            .IsUnique().HasFilter("([IdVariant] IS NOT NULL)")
            .HasDatabaseName("UX_Warehouse_InventoryItem_ProductVariant");
        modelBuilder.Entity<InventoryItem>().HasIndex(x => x.IdProduct)
            .IsUnique().HasFilter("([IdVariant] IS NULL)")
            .HasDatabaseName("UX_Warehouse_InventoryItem_ProductWithoutVariant");
        modelBuilder.Entity<WarehouseParty>().HasIndex(x => x.ExternalPartyKey)
            .IsUnique().HasFilter("([ExternalPartyKey] IS NOT NULL)")
            .HasDatabaseName("UX_Warehouse_Party_ExternalKey");
        modelBuilder.Entity<WarehouseParty>().HasIndex(x => x.IdWarehouse)
            .IsUnique().HasFilter("([IdWarehouse] IS NOT NULL)")
            .HasDatabaseName("UX_Warehouse_Party_Warehouse");
        modelBuilder.Entity<WarehouseProduct>().HasIndex(x => x.ProductKey)
            .IsUnique().HasDatabaseName("UX_Warehouse_Product_ProductKey");
        modelBuilder.Entity<WarehouseProduct>().HasIndex(x => x.Slug)
            .IsUnique().HasFilter("([Slug] IS NOT NULL)")
            .HasDatabaseName("UX_Warehouse_Product_Slug");
        modelBuilder.Entity<WarehouseProductUnit>().HasIndex(x => x.IdProduct, "UX_ProductUnit_OneActiveBasePerProduct")
            .IsUnique().HasFilter("([IsBaseUnit]=(1) AND [IsActive]=(1))")
            .HasDatabaseName("UX_ProductUnit_OneActiveBasePerProduct");
        modelBuilder.Entity<WarehouseProductUnit>().HasIndex(x => x.IdProduct, "UX_Warehouse_ProductUnit_OneBaseUnitPerProduct")
            .IsUnique().HasFilter("([IsBaseUnit]=(1))")
            .HasDatabaseName("UX_Warehouse_ProductUnit_OneBaseUnitPerProduct");
        modelBuilder.Entity<SalesPlanReference>().HasIndex(x => x.StoreSalesPlanKey)
            .IsUnique().HasDatabaseName("UQ_Warehouse_SalesPlanReference_StoreSalesPlanKey");
        modelBuilder.Entity<SalesPlanReference>().HasIndex(x => x.IsRegular)
            .IsUnique().HasFilter("([IsRegular]=(1))")
            .HasDatabaseName("UX_Warehouse_SalesPlanReference_OneRegular");
        modelBuilder.Entity<WarehouseStockMovement>().HasIndex(x => x.IdVoucherLine)
            .IsUnique().HasDatabaseName("UQ_Warehouse_StockMovement_DocumentLine");
        modelBuilder.Entity<Voucher>().HasIndex(x => x.VoucherNumber)
            .IsUnique().HasDatabaseName("UQ_Warehouse_WarehouseDocument_DocumentNumber");
        modelBuilder.Entity<VoucherType>().HasIndex(x => x.IsSystemSalesDispatch)
            .IsUnique().HasFilter("([IsSystemSalesDispatch]=(1))")
            .HasDatabaseName("UX_Warehouse_DocumentType_SystemSalesDispatch");
        modelBuilder.Entity<WarehouseEntity>().HasIndex(x => new { x.IdCompany, x.Code })
            .IsUnique().HasDatabaseName("UQ_Warehouse_Warehouse_Company_Code");
        modelBuilder.Entity<WarehouseEntity>().HasIndex(x => x.IdCompany)
            .IsUnique().HasFilter("([IsSalesWarehouse]=(1))")
            .HasDatabaseName("UX_Warehouse_Warehouse_OneSalesWarehousePerCompany");
    }

    private static void ConfigureDatabaseForeignKeys(ModelBuilder modelBuilder)
    {
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreCategory>().HasOne<Shop.Data.Domain.Store.Entities.StoreCategory>().WithMany().HasForeignKey(x => x.IdParentCategory).OnDelete(DeleteBehavior.NoAction); // Store.Category -> Store.Category
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.Voucher>().HasOne<Shop.Data.Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.Warehouse
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.Voucher>().HasOne<Shop.Data.Domain.Warehouse.Entities.VoucherType>().WithMany().HasForeignKey(x => x.IdVoucherType).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.VoucherType
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.Voucher>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseParty>().WithMany().HasForeignKey(x => x.IdParty).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.Party
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.Voucher>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.Voucher -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOffer>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOffer -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreBrand>().HasOne<Shop.Data.Domain.Store.Entities.GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Store.Brand -> Geo.Country
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferSpecificationOverride>().HasOne<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOffer>().WithMany().HasForeignKey(x => x.IdOffer).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOfferSpecificationOverride -> Store.ProductSalesPlanOffer
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreSpecificationCategory>().HasOne<Shop.Data.Domain.Store.Entities.StoreSpecificationGroup>().WithMany().HasForeignKey(x => x.IdSpecificationGroup).OnDelete(DeleteBehavior.NoAction); // Store.SpecificationCategory -> Store.SpecificationGroup
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferAccessory>().HasOne<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOffer>().WithMany().HasForeignKey(x => x.IdOffer).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOfferAccessory -> Store.ProductSalesPlanOffer
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.GeoProvince>().HasOne<Shop.Data.Domain.Store.Entities.GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Geo.Province -> Geo.Country
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreSpecificationFeature>().HasOne<Shop.Data.Domain.Store.Entities.StoreSpecificationCategory>().WithMany().HasForeignKey(x => x.IdSpecificationCategory).OnDelete(DeleteBehavior.NoAction); // Store.SpecificationFeature -> Store.SpecificationCategory
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOfferAccessory>().HasOne<Shop.Data.Domain.Store.Entities.StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOfferAccessory -> Store.Accessory
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountAddress>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade); // Account.Address -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.GeoCity>().HasOne<Shop.Data.Domain.Store.Entities.GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Geo.City -> Geo.Country
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountAddress>().HasOne<Shop.Data.Domain.Store.Entities.GeoCountry>().WithMany().HasForeignKey(x => x.IdCountry).OnDelete(DeleteBehavior.NoAction); // Account.Address -> Geo.Country
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.GeoCity>().HasOne<Shop.Data.Domain.Store.Entities.GeoProvince>().WithMany().HasForeignKey(x => x.IdProvince).OnDelete(DeleteBehavior.NoAction); // Geo.City -> Geo.Province
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountAddress>().HasOne<Shop.Data.Domain.Store.Entities.GeoProvince>().WithMany().HasForeignKey(x => new { x.IdProvince, x.IdCountry }).HasPrincipalKey(x => new { x.Id, x.IdCountry }).OnDelete(DeleteBehavior.NoAction); // Account.Address -> Geo.Province
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountAddress>().HasOne<Shop.Data.Domain.Store.Entities.GeoCity>().WithMany().HasForeignKey(x => new { x.IdCity, x.IdProvince, x.IdCountry }).HasPrincipalKey(x => new { x.Id, x.IdProvince, x.IdCountry }).OnDelete(DeleteBehavior.NoAction); // Account.Address -> Geo.City
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountPriceAlert>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade); // Account.PriceAlert -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductColor>().HasOne<Shop.Data.Domain.Store.Entities.StoreColor>().WithMany().HasForeignKey(x => x.IdColor).OnDelete(DeleteBehavior.NoAction); // Store.ProductColor -> Store.Color
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreCustomerOrderReference>().HasOne<Shop.Data.Domain.Warehouse.Entities.Voucher>().WithMany().HasForeignKey(x => x.IdVoucher).OnDelete(DeleteBehavior.NoAction); // Store.Order -> Warehouse.Voucher
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductCollectionItem>().HasOne<Shop.Data.Domain.Store.Entities.StoreProductCollection>().WithMany().HasForeignKey(x => x.IdCollection).OnDelete(DeleteBehavior.NoAction); // Store.ProductCollectionItem -> Store.ProductCollection
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreCustomerOrderReference>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.NoAction); // Store.Order -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreCustomerOrderReference>().HasOne<Shop.Data.Domain.Account.Entities.AccountAddress>().WithMany().HasForeignKey(x => x.IdAddress).OnDelete(DeleteBehavior.NoAction); // Store.Order -> Account.Address
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StorePaymentAttempt>().HasOne<Shop.Data.Domain.Account.Entities.StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder).OnDelete(DeleteBehavior.NoAction); // Store.PaymentAttempt -> Store.Order
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountCartItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Account.CartItem -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreOrderShipmentTrackingHistory>().HasOne<Shop.Data.Domain.Account.Entities.StoreCustomerOrderReference>().WithMany().HasForeignKey(x => x.IdOrder).OnDelete(DeleteBehavior.NoAction); // Store.OrderShipmentTrackingHistory -> Store.Order
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountCartItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.ProductVariant>().WithMany().HasForeignKey(x => new { x.IdProduct, x.IdVariant }).HasPrincipalKey(x => new { x.IdProduct, x.Id }).OnDelete(DeleteBehavior.NoAction); // Account.CartItem -> Warehouse.ProductVariant
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockOwner>().HasOne<Shop.Data.Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.WarehouseStockOwner -> Warehouse.Warehouse
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockOwner>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdStockOwner).OnDelete(DeleteBehavior.NoAction); // Warehouse.WarehouseStockOwner -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountArticleInteraction>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade); // Account.ArticleInteraction -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountArticleInteraction>().HasOne<Shop.Data.Domain.Store.Entities.MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Account.ArticleInteraction -> Mag.MagazineArticle
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseStockOwnerType>().WithMany().HasForeignKey(x => x.IdStockOwnerType).OnDelete(DeleteBehavior.NoAction); // Warehouse.SalesPlanReference -> Warehouse.StockOwnerType
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().HasOne<Shop.Data.Domain.Store.Entities.StoreBrand>().WithMany().HasForeignKey(x => x.IdBrand).OnDelete(DeleteBehavior.NoAction); // Warehouse.Product -> Store.Brand
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().HasOne<Shop.Data.Domain.Store.Entities.StoreCategory>().WithMany().HasForeignKey(x => x.IdCategory).OnDelete(DeleteBehavior.NoAction); // Warehouse.Product -> Store.Category
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.MagazineArticleMetric>().HasOne<Shop.Data.Domain.Store.Entities.MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.Cascade); // Mag.MagazineArticleMetric -> Mag.MagazineArticle
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSpecificationValue>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductSpecificationValue -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountFavorite>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.Favorite -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreCustomerOrderItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.OrderItem -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountCompareItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.CompareItem -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountArticleFeedback>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade); // Account.ArticleFeedback -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountArticleFeedback>().HasOne<Shop.Data.Domain.Store.Entities.MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Account.ArticleFeedback -> Mag.MagazineArticle
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountCartItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.CartItem -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductBenefit>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductBenefit -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductCollectionItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductCollectionItem -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductColor>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductColor -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.MagazineArticleRelated>().HasOne<Shop.Data.Domain.Store.Entities.MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.Cascade); // Mag.MagazineArticleRelated -> Mag.MagazineArticle
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountPriceAlert>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Account.PriceAlert -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Admin.Entities.AdminPermission>().HasOne<Shop.Data.Domain.Admin.Entities.AdminPermission>().WithMany().HasForeignKey(x => x.ParentPermissionKey).HasPrincipalKey(x => x.PermissionKey).OnDelete(DeleteBehavior.NoAction); // Admin.Permission -> Admin.Permission
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSalesPlanOffer>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductSalesPlanOffer -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.MagazineArticleSource>().HasOne<Shop.Data.Domain.Store.Entities.MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.NoAction); // Mag.MagazineArticleSource -> Mag.MagazineArticle
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.MagazineArticleTag>().HasOne<Shop.Data.Domain.Store.Entities.MagazineArticle>().WithMany().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.Cascade); // Mag.MagazineArticleTag -> Mag.MagazineArticle
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductAsset>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductAsset -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Admin.Entities.AdminUserWarehouseAccess>().HasOne<Shop.Data.Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Admin.SystemUserWarehouseAccess -> Warehouse.Warehouse
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductAccessory>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct).OnDelete(DeleteBehavior.NoAction); // Store.ProductAccessory -> Warehouse.Product
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.VoucherLine>().HasOne<Shop.Data.Domain.Warehouse.Entities.WarehouseUnit>().WithMany().HasForeignKey(x => x.IdUnit).OnDelete(DeleteBehavior.NoAction); // Warehouse.VoucherLine -> Warehouse.Unit
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountCompareItem>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade); // Account.CompareItem -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductAccessory>().HasOne<Shop.Data.Domain.Store.Entities.StoreAccessory>().WithMany().HasForeignKey(x => x.IdAccessory).OnDelete(DeleteBehavior.NoAction); // Store.ProductAccessory -> Store.Accessory
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.VoucherLine>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.VoucherLine -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.VoucherLine>().HasOne<Shop.Data.Domain.Warehouse.Entities.InventoryItem>().WithMany().HasForeignKey(x => x.IdInventoryItem).OnDelete(DeleteBehavior.NoAction); // Warehouse.VoucherLine -> Warehouse.InventoryItem
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreCustomerOrderItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Store.OrderItem -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.StoreCustomerOrderItem>().HasOne<Shop.Data.Domain.Warehouse.Entities.ProductVariant>().WithMany().HasForeignKey(x => new { x.IdProduct, x.IdVariant }).HasPrincipalKey(x => new { x.IdProduct, x.Id }).OnDelete(DeleteBehavior.NoAction); // Store.OrderItem -> Warehouse.ProductVariant
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockMovement>().HasOne<Shop.Data.Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.Warehouse
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockMovement>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlanReference).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockMovement>().HasOne<Shop.Data.Domain.Warehouse.Entities.Voucher>().WithMany().HasForeignKey(x => x.IdVoucher).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.Voucher
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockMovement>().HasOne<Shop.Data.Domain.Warehouse.Entities.VoucherLine>().WithMany().HasForeignKey(x => x.IdVoucherLine).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.VoucherLine
modelBuilder.Entity<Shop.Data.Domain.Warehouse.Entities.WarehouseStockMovement>().HasOne<Shop.Data.Domain.Warehouse.Entities.InventoryItem>().WithMany().HasForeignKey(x => x.IdInventoryItem).OnDelete(DeleteBehavior.NoAction); // Warehouse.StockMovement -> Warehouse.InventoryItem
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountFavorite>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.Cascade); // Account.Favorite -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Account.Entities.AccountCart>().HasOne<Shop.Data.Domain.Account.Entities.AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile).OnDelete(DeleteBehavior.NoAction); // Account.Cart -> Account.Profile
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreSiteIdentity>().HasOne<Shop.Data.Domain.Warehouse.Entities.Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse).OnDelete(DeleteBehavior.NoAction); // Store.SiteSetting -> Warehouse.Warehouse
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductVariantSalesPlanOffer>().HasOne<Shop.Data.Domain.Warehouse.Entities.SalesPlanReference>().WithMany().HasForeignKey(x => x.IdSalesPlan).OnDelete(DeleteBehavior.NoAction); // Store.ProductVariantSalesPlanOffer -> Warehouse.SalesPlanReference
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductVariantSalesPlanOffer>().HasOne<Shop.Data.Domain.Warehouse.Entities.ProductVariant>().WithMany().HasForeignKey(x => x.IdVariant).OnDelete(DeleteBehavior.NoAction); // Store.ProductVariantSalesPlanOffer -> Warehouse.ProductVariant
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreProductSpecificationValue>().HasOne<Shop.Data.Domain.Store.Entities.StoreSpecificationFeature>().WithMany().HasForeignKey(x => x.IdSpecificationFeature).OnDelete(DeleteBehavior.NoAction); // Store.ProductSpecificationValue -> Store.SpecificationFeature
modelBuilder.Entity<Shop.Data.Domain.Store.Entities.StoreBrandEditorial>().HasOne<Shop.Data.Domain.Store.Entities.StoreBrand>().WithMany().HasForeignKey(x => x.IdBrand).OnDelete(DeleteBehavior.NoAction); // Store.BrandEditorial -> Store.Brand

    }
}
