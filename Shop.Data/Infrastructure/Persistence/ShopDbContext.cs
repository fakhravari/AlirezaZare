using Microsoft.EntityFrameworkCore;
using Shop.Database.Domain.Account.Entities;
using Shop.Database.Domain.Store.Entities;
using Shop.Database.Domain.Warehouse.Entities;

namespace Shop.Database.Infrastructure.Persistence;

public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<SalesPlanReference> SalesPlanReferences => Set<SalesPlanReference>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<WarehouseProduct> Products => Set<WarehouseProduct>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<StoreProduct> StoreProducts => Set<StoreProduct>();
    public DbSet<ProductFeedback> ProductFeedback => Set<ProductFeedback>();
    public DbSet<AccountProfile> AccountProfiles => Set<AccountProfile>();
    public DbSet<AccountRecentSearch> AccountRecentSearches => Set<AccountRecentSearch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.HasOne(x => x.SalesPlanReference).WithMany(x => x.StockBalances).HasForeignKey(x => x.IdSalesPlanReference);
            entity.Property(x => x.Quantity).HasPrecision(18, 3);
            entity.Property(x => x.MinimumQuantity).HasPrecision(18, 3);
        });
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("InventoryItem", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Product).WithMany(x => x.InventoryItems).HasForeignKey(x => x.IdProduct);
            entity.HasOne(x => x.ProductVariant).WithMany().HasForeignKey(x => x.IdProductVariant);
        });
        modelBuilder.Entity<WarehouseProduct>(entity =>
        {
            entity.ToTable("Product", "Warehouse");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IdStoreProduct).HasColumnName("IdStoreProduct");
        });
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariant", "Warehouse");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<StoreProduct>(entity =>
        {
            entity.ToTable("Product", "Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StoreProductKey).HasMaxLength(128).IsRequired();
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
