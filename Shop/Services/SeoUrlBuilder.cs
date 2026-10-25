using Microsoft.AspNetCore.Http;

namespace Shop.Services;

public static class SeoUrlBuilder
{
    public static string SiteOrigin(string? configuredSiteUrl, HttpRequest request)
    {
        if (Uri.TryCreate(configuredSiteUrl, UriKind.Absolute, out Uri? configured)
            && (configured.Scheme == Uri.UriSchemeHttp || configured.Scheme == Uri.UriSchemeHttps)
            && string.IsNullOrEmpty(configured.UserInfo)
            && configured.AbsolutePath == "/"
            && string.IsNullOrEmpty(configured.Query)
            && string.IsNullOrEmpty(configured.Fragment))
        {
            return configured.GetLeftPart(UriPartial.Authority);
        }

        return $"{request.Scheme}://{request.Host}";
    }

    public static string SiteRoot(string? configuredSiteUrl, HttpRequest request) =>
        $"{SiteOrigin(configuredSiteUrl, request)}{request.PathBase}/";

    public static string AbsolutePageUrl(string siteRoot, string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute)
            ? absolute.AbsoluteUri
            : new Uri(new Uri(siteRoot), path.TrimStart('/')).AbsoluteUri;

    public static string AbsoluteAssetUrl(string siteOrigin, string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute)
            ? absolute.AbsoluteUri
            : new Uri(new Uri(siteOrigin.TrimEnd('/') + "/"), path.TrimStart('/')).AbsoluteUri;
}
