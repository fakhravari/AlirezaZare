namespace Shop.Data.Domain.Warehouse.Entities;

public sealed class Company
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}