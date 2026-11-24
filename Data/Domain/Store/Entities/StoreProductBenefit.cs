namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreProductBenefit
{
    public decimal Id { get; set; }
    public decimal IdProduct { get; set; }
    public string Title { get; set; } = "";
    public string? Detail { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}