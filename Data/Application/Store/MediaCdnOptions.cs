namespace Shop.Database.Application.Store;

public sealed class MediaCdnOptions
{
    public const string SectionName = "MediaCdn";
    public const string DefaultBaseUrl = "https://cdn.bs-shop.ir/";
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    public static string NormalizeBaseUrl(string? value, string? fallback = null)
    {
        string candidate = string.IsNullOrWhiteSpace(value) ? fallback ?? DefaultBaseUrl : value.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo) ||
            uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("MediaCdn:BaseUrl must be an HTTPS origin without credentials, path, query, or fragment.");
        return uri.GetLeftPart(UriPartial.Authority) + "/";
    }
}
