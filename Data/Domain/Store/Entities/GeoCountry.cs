namespace Shop.Data.Domain.Store.Entities;

public sealed class GeoCountry
{
    public long Id { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string NameFa { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
}
