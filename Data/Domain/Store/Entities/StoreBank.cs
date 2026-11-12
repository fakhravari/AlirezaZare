namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreBank
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; }
}
