using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Admin.Entities;
using Shop.Database.Domain.Account.Entities;
using Shop.Database.Domain.Store.Entities;
using Shop.Database.Domain.Warehouse.Entities;

namespace Shop.Database.Infrastructure.Persistence;

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
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<WarehouseProduct> Products => Set<WarehouseProduct>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<StoreProduct> StoreProducts => Set<StoreProduct>();
    public DbSet<StoreSiteIdentity> StoreSiteIdentities => Set<StoreSiteIdentity>();
    public DbSet<ProductFeedback> ProductFeedback => Set<ProductFeedback>();
    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();
    public DbSet<AccountRecentSearch> AccountRecentSearches => Set<AccountRecentSearch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.HasAlternateKey(x => new { x.Id, x.IdStoreProduct });
            entity.Property(x => x.IdStoreProduct).HasColumnName("IdStoreProduct");
        });
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariant", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.Id, x.IdProduct });
            entity.HasAlternateKey(x => new { x.Id, x.IdStoreProduct });
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(300).IsRequired();
            entity.Property(x => x.AttributesJson).HasColumnType("nvarchar(max)");
            entity.Property(x => x.CreatedAtUtc).HasPrecision(3);
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
            entity.HasOne(x => x.Product).WithMany().HasForeignKey(x => new { x.IdProduct, x.IdStoreProduct })
                .HasPrincipalKey(x => new { x.Id, x.IdStoreProduct });
        });
        modelBuilder.Entity<StoreProduct>(entity =>
        {
            entity.ToTable("Product", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StoreProductKey).HasMaxLength(128).IsRequired();
        });
        modelBuilder.Entity<StoreSiteIdentity>(entity =>
        {
            entity.ToTable("SiteIdentity", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.StoreName).HasMaxLength(240).IsRequired();
            entity.Property(x => x.VisualBrandName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SupportPhoneDisplay).HasMaxLength(32).IsRequired();
            entity.Property(x => x.SupportPhoneUri).HasMaxLength(40).IsRequired();
            entity.Property(x => x.WhatsAppNumber).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PublicEmail).HasMaxLength(254).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasPrecision(3);
        });
        modelBuilder.Entity<AccountProfile>(entity =>
        {
            entity.ToTable("Profile", "Account");
            entity.HasKey(x => x.Id);
        });
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
            entity.HasOne(x => x.Product).WithMany(x => x.Feedback).HasForeignKey(x => x.IdProduct)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Profile).WithMany(x => x.ProductFeedback).HasForeignKey(x => x.IdProfile)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
