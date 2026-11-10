using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Domain.Store.Entities;
using Shop.Data.Domain.Warehouse.Entities;

namespace Shop.Data.Infrastructure.Persistence;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<AdminSystemUser> AdminSystemUsers => Set<AdminSystemUser>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<AdminPermission> AdminPermissions => Set<AdminPermission>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();
    public DbSet<AdminRolePermission> AdminRolePermissions => Set<AdminRolePermission>();
    public DbSet<AdminUserWarehouseAccess> AdminUserWarehouseAccesses => Set<AdminUserWarehouseAccess>();
    public DbSet<WarehouseDocumentType> WarehouseDocumentTypes => Set<WarehouseDocumentType>();
    public DbSet<WarehouseUnit> WarehouseUnits => Set<WarehouseUnit>();
    public DbSet<WarehouseParty> WarehouseParties => Set<WarehouseParty>();
    public DbSet<WarehouseProductUnit> WarehouseProductUnits => Set<WarehouseProductUnit>();
    public DbSet<WarehouseDocumentRecord> WarehouseDocuments => Set<WarehouseDocumentRecord>();
    public DbSet<WarehouseDocumentLineRecord> WarehouseDocumentLines => Set<WarehouseDocumentLineRecord>();
    public DbSet<WarehouseStockMovement> WarehouseStockMovements => Set<WarehouseStockMovement>();
    public DbSet<WarehouseDocumentAudit> WarehouseDocumentAudits => Set<WarehouseDocumentAudit>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<SalesPlanReference> SalesPlanReferences => Set<SalesPlanReference>();
    public DbSet<WarehouseStockOwner> WarehouseStockOwners => Set<WarehouseStockOwner>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<WarehouseProduct> Products => Set<WarehouseProduct>();
    public DbSet<StoreSiteIdentity> StoreSiteIdentities => Set<StoreSiteIdentity>();
    public DbSet<MagazineArticle> MagazineArticles => Set<MagazineArticle>();
    public DbSet<MagazineArticleCategory> MagazineArticleCategories => Set<MagazineArticleCategory>();
    public DbSet<MagazineArticleBranch> MagazineArticleBranches => Set<MagazineArticleBranch>();
    public DbSet<MagazineArticleTopic> MagazineArticleTopics => Set<MagazineArticleTopic>();
    public DbSet<MagazineArticleArea> MagazineArticleAreas => Set<MagazineArticleArea>();
    public DbSet<MagazineArticleSection> MagazineArticleSections => Set<MagazineArticleSection>();
    public DbSet<AccountArticleInteraction> AccountArticleInteractions => Set<AccountArticleInteraction>();
    public DbSet<StoreContentPage> StoreContentPages => Set<StoreContentPage>();
    public DbSet<StoreContentPageSection> StoreContentPageSections => Set<StoreContentPageSection>();
    public DbSet<StoreFaqItem> StoreFaqItems => Set<StoreFaqItem>();
    public DbSet<ProductFeedback> ProductFeedback => Set<ProductFeedback>();
    public DbSet<StoreAttributeDefinition> StoreAttributeDefinitions => Set<StoreAttributeDefinition>();
    public DbSet<StoreProductMedia> StoreProductMedia => Set<StoreProductMedia>();
    public DbSet<StoreBrand> StoreBrands => Set<StoreBrand>();
    public DbSet<StoreCategory> StoreCategories => Set<StoreCategory>();
    public DbSet<GeoCountry> GeoCountries => Set<GeoCountry>();
    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();
    public DbSet<AccountRecentSearch> AccountRecentSearches => Set<AccountRecentSearch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MagazineArticle>(entity =>
        {
            entity.ToTable("MagazineArticle", "Mag");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ArticleKey).HasMaxLength(150).IsRequired();
            entity.Property(x => x.IdCategory).HasColumnName("IdCategory");
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.Author).HasMaxLength(200);
            entity.Property(x => x.HeroImagePath).HasMaxLength(1000);
            entity.Property(x => x.ContentImagePath).HasMaxLength(1000);
            entity.Property(x => x.Kicker).HasMaxLength(300);
            entity.Property(x => x.ReadTimeText).HasMaxLength(80);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasIndex(x => x.ArticleKey).IsUnique();
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.IdCategory);
            entity.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.IdBranch);
            entity.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.IdTopic);
            entity.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.IdArea);
            entity.HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.IdMagazineArticle).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MagazineArticleCategory>(entity => { entity.ToTable("ArticleCategory", "Mag"); entity.HasKey(x => x.Id); entity.Property(x => x.CategoryKey).HasMaxLength(300).IsRequired(); entity.HasIndex(x => x.CategoryKey).IsUnique(); });
        modelBuilder.Entity<MagazineArticleBranch>(entity => { entity.ToTable("ArticleBranch", "Mag"); entity.HasKey(x => x.Id); entity.Property(x => x.BranchKey).HasMaxLength(160).IsRequired(); entity.HasIndex(x => x.BranchKey).IsUnique(); });
        modelBuilder.Entity<MagazineArticleTopic>(entity => { entity.ToTable("ArticleTopic", "Mag"); entity.HasKey(x => x.Id); entity.Property(x => x.TopicKey).HasMaxLength(240).IsRequired(); entity.HasIndex(x => x.TopicKey).IsUnique(); });
        modelBuilder.Entity<MagazineArticleArea>(entity => { entity.ToTable("ArticleArea", "Mag"); entity.HasKey(x => x.Id); entity.Property(x => x.AreaKey).HasMaxLength(240).IsRequired(); entity.HasIndex(x => x.AreaKey).IsUnique(); });
        modelBuilder.Entity<MagazineArticleSection>(entity =>
        {
            entity.ToTable("MagazineArticleSection", "Mag");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Heading).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Body).IsRequired();
        });
        modelBuilder.Entity<AccountArticleInteraction>(entity =>
        {
            entity.ToTable("ArticleInteraction", "Account");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InteractionType).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => new { x.IdProfile, x.IdMagazineArticle, x.InteractionType }).IsUnique();
        });
        modelBuilder.Entity<StoreContentPage>(entity =>
        {
            entity.ToTable("ContentPage", "Store"); entity.HasKey(x => x.Id);
            entity.Property(x => x.PageKey).HasMaxLength(150).IsRequired(); entity.Property(x => x.RoutePath).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired(); entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ContentSource).HasMaxLength(30).IsRequired(); entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasIndex(x => x.PageKey).IsUnique(); entity.HasIndex(x => x.RoutePath).IsUnique();
            entity.HasMany(x => x.Sections).WithOne().HasForeignKey(x => x.IdContentPage).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StoreContentPageSection>(entity =>
        {
            entity.ToTable("ContentPageSection", "Store"); entity.HasKey(x => x.Id);
            entity.Property(x => x.SectionKey).HasMaxLength(100).IsRequired(); entity.Property(x => x.Heading).HasMaxLength(300);
            entity.HasIndex(x => new { x.IdContentPage, x.SectionKey }).IsUnique();
        });
        modelBuilder.Entity<StoreFaqItem>(entity =>
        {
            entity.ToTable("FaqItem", "Store"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Question).HasMaxLength(1000).IsRequired(); entity.Property(x => x.Answer).IsRequired();
            entity.Property(x => x.LinkText).HasMaxLength(300); entity.Property(x => x.LinkUrl).HasMaxLength(1000);
            entity.HasOne<StoreContentPage>().WithMany().HasForeignKey(x => x.IdContentPage);
        });
        modelBuilder.Entity<AdminSystemUser>(entity =>
        {
            entity.ToTable("SystemUser", "Admin");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SecurityStamp).IsRequired();
            entity.Property(x => x.NormalizedUserName).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.NormalizedUserName).IsUnique();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.LastLoginAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasMany(x => x.Roles).WithOne(x => x.User).HasForeignKey(x => x.IdUser);
            entity.HasMany(x => x.WarehouseAccess).WithOne(x => x.User).HasForeignKey(x => x.IdUser);
        });
        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.ToTable("Role", "Admin");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RoleKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.RoleKey).IsUnique();
            entity.HasMany(x => x.Users).WithOne(x => x.Role).HasForeignKey(x => x.IdRole);
            entity.HasMany(x => x.Permissions).WithOne(x => x.Role).HasForeignKey(x => x.IdRole);
        });
        modelBuilder.Entity<AdminPermission>(entity =>
        {
            entity.ToTable("Permission", "Admin");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PermissionKey).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.ParentKey).HasColumnName("ParentPermissionKey").HasMaxLength(160);
            entity.Property(x => x.Area).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Controller).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FeatureKey).HasMaxLength(100);
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
            entity.Property(x => x.IdPermission).HasColumnName("IdPermission");
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.IdRole);
            entity.HasOne(x => x.Permission)
                .WithMany()
                .HasForeignKey(x => x.IdPermission);
        });
        modelBuilder.Entity<AdminUserWarehouseAccess>(entity =>
        {
            entity.ToTable("SystemUserWarehouseAccess", "Admin");
            entity.HasKey(x => new { x.IdUser, x.IdWarehouse });
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouse", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Company).WithMany(x => x.Warehouses).HasForeignKey(x => x.IdCompany);
        });
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });
        modelBuilder.Entity<WarehouseDocumentType>(entity =>
        {
            entity.ToTable("DocumentType", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Direction).HasColumnType("char(1)").IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<WarehouseUnit>(entity =>
        {
            entity.ToTable("Unit", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<WarehouseParty>(entity =>
        {
            entity.ToTable("Party", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PartyType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ExternalPartyKey).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.IdWarehouse);
        });
        modelBuilder.Entity<WarehouseProductUnit>(entity =>
        {
            entity.ToTable("ProductUnit", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ConversionFactor).HasPrecision(18, 9);
            entity.HasIndex(x => new { x.IdProduct, x.IdUnit }).IsUnique();
            entity.HasOne<WarehouseProduct>().WithMany().HasForeignKey(x => x.IdProduct);
            entity.HasOne<WarehouseUnit>().WithMany().HasForeignKey(x => x.IdUnit);
        });
        modelBuilder.Entity<WarehouseDocumentRecord>(entity =>
        {
            entity.ToTable("WarehouseDocument", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DocumentNumber).HasDefaultValueSql("NEXT VALUE FOR [Warehouse].[DocumentNumberSequence]");
            entity.Property(x => x.Status).HasColumnType("char(1)").IsRequired();
            entity.Property(x => x.CustomerOrderKey).HasMaxLength(128);
            entity.Property(x => x.ExternalReference).HasMaxLength(128);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.CreatedByUserKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.UpdatedByUserKey).HasMaxLength(128);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasIndex(x => new { x.CustomerOrderKey, x.IdSalesPlanReference }).IsUnique()
                .HasFilter("[CustomerOrderKey] IS NOT NULL");
            entity.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.IdDocument);
        });
        modelBuilder.Entity<WarehouseDocumentLineRecord>(entity =>
        {
            entity.ToTable("WarehouseDocumentLine", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.ConversionFactor).HasPrecision(18, 9);
            entity.Property(x => x.BaseQuantity).HasPrecision(18, 3);
            entity.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired();
            entity.Property(x => x.VariantNameSnapshot).HasMaxLength(300);
            entity.Property(x => x.UnitNameSnapshot).HasMaxLength(100).IsRequired();
        });
        modelBuilder.Entity<WarehouseStockMovement>(entity =>
        {
            entity.ToTable("StockMovement", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.QuantityDelta).HasPrecision(18, 3);
            entity.Property(x => x.OccurredAtUtc).HasPrecision(3);
        });
        modelBuilder.Entity<WarehouseDocumentAudit>(entity =>
        {
            entity.ToTable("DocumentAudit", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(20).IsRequired();
            entity.Property(x => x.ActorUserKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.OccurredAtUtc).HasPrecision(3);
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.BeforeJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.AfterJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.DocumentId, x.OccurredAtUtc, x.Id });
            entity.HasIndex(x => new { x.WarehouseId, x.OccurredAtUtc, x.Id });
        });
        modelBuilder.Entity<SalesPlanReference>(entity =>
        {
            entity.ToTable("SalesPlanReference", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<WarehouseStockOwner>(entity =>
        {
            entity.ToTable("WarehouseStockOwner", "Warehouse");
            entity.HasKey(x => new { x.IdWarehouse, x.IdStockOwner });
        });
        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("StockBalance", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.IdWarehouse, x.IdInventoryItem, x.IdSalesPlanReference }).IsUnique();
            entity.HasOne(x => x.Warehouse).WithMany(x => x.StockBalances).HasForeignKey(x => x.IdWarehouse);
            entity.HasOne(x => x.InventoryItem).WithMany().HasForeignKey(x => x.IdInventoryItem);
            entity.HasOne(x => x.SalesPlanReference).WithMany(x => x.StockBalances)
                .HasForeignKey(x => x.IdSalesPlanReference);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.MinimumQuantity).HasPrecision(18, 3);
        });
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItem", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Product).WithMany(x => x.InventoryItems).HasForeignKey(x => x.IdProduct);
            entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => new { x.IdProductVariant, x.IdProduct })
                .HasPrincipalKey(x => new { x.Id, x.IdProduct });
        });
        modelBuilder.Entity<WarehouseProduct>(entity =>
        {
            entity.ToTable("Product", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.ProductKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.EnglishName).HasMaxLength(300);
            entity.Property(x => x.PowerSource).HasMaxLength(200);
            entity.Property(x => x.UseCase).HasMaxLength(500);
            entity.Property(x => x.Slug).HasMaxLength(200);
            entity.Property(x => x.Badge).HasMaxLength(100);
            entity.Property(x => x.PriceAmount).HasPrecision(19, 0);
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
        });
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariant", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.IdProduct });
            entity.HasAlternateKey(x => new { x.IdProduct, x.Id });
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.AttributesJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct);
        });
        modelBuilder.Entity<StoreSiteIdentity>(entity =>
        {
            entity.ToTable("SiteSetting", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.StoreName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.VisualBrandName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SupportPhoneDisplay).HasMaxLength(32).IsRequired();
            entity.Property(x => x.SupportPhoneUri).HasMaxLength(40).IsRequired();
            entity.Property(x => x.WhatsAppNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PublicEmail).HasMaxLength(254).IsRequired();
            entity.Property(x => x.MediaCdnBaseUrl).HasMaxLength(500).HasDefaultValue("https://cdn.bs-shop.ir/").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("IdentityUpdatedAtUtc").HasPrecision(3);
        });
        modelBuilder.Entity<AccountProfile>(entity =>
        {
            entity.ToTable("Profile", "Account");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
        });
        modelBuilder.Entity<AccountCartReference>(entity => { entity.HasNoKey(); entity.ToTable("Cart", "Account"); });
        modelBuilder.Entity<StoreCustomerOrderReference>(entity => { entity.HasNoKey(); entity.ToTable("Order", "Store"); });
        modelBuilder.Entity<AccountRecentSearch>(entity =>
        {
            entity.ToTable("RecentSearch", "Account");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SearchKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SearchText).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SearchedAtUtc).HasPrecision(3);
            entity.HasIndex(x => new { x.IdProfile, x.SearchKey }).IsUnique();
            entity.HasIndex(x => new { x.IdProfile, x.SearchedAtUtc });
            entity.HasOne<AccountProfile>().WithMany().HasForeignKey(x => x.IdProfile)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProductFeedback>(entity =>
        {
            entity.ToTable("ProductFeedback", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FeedbackType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.FeedbackText).HasMaxLength(800).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AdminReply).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.RepliedAtUtc).HasPrecision(3);
            entity.HasIndex(x => new { x.IdProduct, x.FeedbackType, x.Status, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.IdProfile, x.IdProduct, x.FeedbackType });
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profile).WithMany(x => x.ProductFeedback).HasForeignKey(x => x.IdProfile)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<StoreAttributeDefinition>(entity =>
        {
            entity.ToTable("AttributeDefinition", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AttributeKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(100);
            entity.Property(x => x.SourceKey).HasMaxLength(100);
            entity.Property(x => x.FilterKey).HasMaxLength(100);
            entity.HasIndex(x => x.AttributeKey).IsUnique();
        });
        modelBuilder.Entity<StoreProductMedia>(entity =>
        {
            entity.ToTable("ProductMedia", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MediaKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MainPath).HasMaxLength(1000);
            entity.Property(x => x.CardPath).HasMaxLength(1000);
            entity.Property(x => x.ThumbnailPath).HasMaxLength(1000);
            entity.Property(x => x.ImagesJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.VideosJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.IdProduct, x.MediaKey }).IsUnique();
        });
        modelBuilder.Entity<StoreBrand>(entity =>
        {
            entity.ToTable("Brand", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.BrandKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameFa).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LogoPath).HasMaxLength(500);
            entity.HasIndex(x => x.BrandKey).IsUnique();
        });
        modelBuilder.Entity<StoreCategory>(entity =>
        {
            entity.ToTable("Category", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CategoryKey).HasMaxLength(150).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameFa).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PathJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(x => x.CategoryKey).IsUnique();
        });
        modelBuilder.Entity<GeoCountry>(entity =>
        {
            entity.ToTable("Country", "Geo");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CountryCode).HasMaxLength(8).IsRequired();
            entity.Property(x => x.NameFa).HasMaxLength(150).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.CountryCode).IsUnique();
        });
    }
}
