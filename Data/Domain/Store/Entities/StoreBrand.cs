namespace Shop.Database.Domain.Store.Entities;

public sealed class StoreBrand
{
    public long Id { get; set; }
    public long? IdCountry { get; set; }
    public string BrandKey { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; } = true;
}
