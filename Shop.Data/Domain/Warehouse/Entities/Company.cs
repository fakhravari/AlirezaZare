namespace Shop.Database.Domain.Warehouse.Entities;

public sealed class Company
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
