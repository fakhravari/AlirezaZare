using System.Text.RegularExpressions;

namespace Shop.Data.Application.Store;

public static partial class StoreHtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase) { "p", "br", "strong", "b", "em", "i", "u", "s", "h3", "h4", "blockquote", "ul", "ol", "li", "a" };
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var html = value.Trim().Replace("\r\n", "\n").Replace("\n", "<br>");
        html = ScriptOrStyle().Replace(html, string.Empty);
        html = Comments().Replace(html, string.Empty);
        return Tags().Replace(html, match => SanitizeTag(match.Value));
    }
    private static string SanitizeTag(string tag)
    {
        var nameMatch = TagName().Match(tag);
        if (!nameMatch.Success || !AllowedTags.Contains(nameMatch.Groups[1].Value)) return string.Empty;
        var name = nameMatch.Groups[1].Value.ToLowerInvariant();
        if (name == "br") return tag.StartsWith("</", StringComparison.Ordinal) ? string.Empty : "<br>";
        if (tag.StartsWith("</", StringComparison.Ordinal)) return $"</{name}>";
        if (name != "a") return $"<{name}>";
        var href = Href().Match(tag).Groups[1].Value.Trim();
        var safeHref = href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) || href.StartsWith("/", StringComparison.Ordinal) || href.StartsWith("#", StringComparison.Ordinal);
        return safeHref ? $"<a href=\"{System.Net.WebUtility.HtmlEncode(href)}\" target=\"_blank\" rel=\"noopener noreferrer\">" : "<a>";
    }
    [GeneratedRegex("<\\s*(script|style)\\b[^>]*>.*?<\\s*/\\s*\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)] private static partial Regex ScriptOrStyle();
    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)] private static partial Regex Comments();
    [GeneratedRegex("<\\/?[A-Za-z][^>]*>", RegexOptions.IgnoreCase)] private static partial Regex Tags();
    [GeneratedRegex("^<\\/?\\s*([A-Za-z][A-Za-z0-9]*)", RegexOptions.IgnoreCase)] private static partial Regex TagName();
    [GeneratedRegex("\\bhref\\s*=\\s*[\\\"']([^\\\"']*)", RegexOptions.IgnoreCase)] private static partial Regex Href();
}
