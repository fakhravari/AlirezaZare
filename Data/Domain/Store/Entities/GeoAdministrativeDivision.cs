namespace Shop.Data.Domain.Store.Entities;

public sealed class GeoProvince
{
    public long Id { get; set; }
    public long IdCountry { get; set; }
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public sealed class GeoCity
{
    public long Id { get; set; }
    public long IdCountry { get; set; }
    public long IdProvince { get; set; }
    public string SourceCountryName { get; set; } = "";
    public string NameFa { get; set; } = "";
    public string NameEn { get; set; } = "";
    public bool IsProvinceCenter { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
