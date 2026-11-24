namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreHomeTestimonial
{
    public long Id { get; set; }
    public decimal IdProduct { get; set; }
    public string DisplayName { get; set; } = "";
    public string FeedbackText { get; set; } = "";
    public byte Rating { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public Shop.Data.Domain.Warehouse.Entities.WarehouseProduct Product { get; set; } = null!;
}