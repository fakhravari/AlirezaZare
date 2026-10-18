namespace Shop.Models;

public sealed class SeoMetadataOptions
{
    public string SiteName { get; set; } = "فروشگاه برق و ابزار ندا اولیائی";
    public string Locale { get; set; } = "fa_IR";
    public string DefaultImagePath { get; set; } = "~/Shop/images/heroes/hero-electrical-editorial.png";
    public string DefaultImageAlt { get; set; } = "ابزار و تجهیزات فنی فروشگاه ندا اولیائی";
    public string? GoogleSiteVerification { get; set; }
    public string? BingSiteVerification { get; set; }
    public string? FacebookAppId { get; set; }
    public string? FacebookDomainVerification { get; set; }
    public string? XSite { get; set; }
    public string? XCreator { get; set; }
}
