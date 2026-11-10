using System.Text.Json.Nodes;

namespace Shop.Data.Application.Store;

public static class ContentCdn
{
    public static string? ToUrl(string? value, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        string cdnBaseUrl = MediaCdnOptions.NormalizeBaseUrl(baseUrl);
        string path = value.Trim();
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute)) return value;
            if (absolute.Host.Equals(new Uri(cdnBaseUrl).Host, StringComparison.OrdinalIgnoreCase))
                return cdnBaseUrl + absolute.AbsolutePath.TrimStart('/') + absolute.Query + absolute.Fragment;
            return value;
        }
        path = path.TrimStart('~', '/');
        return path.StartsWith("content/", StringComparison.OrdinalIgnoreCase) ? cdnBaseUrl + path : value;
    }

    public static string? RewriteJson(string? json, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        JsonNode? node = JsonNode.Parse(json);
        return Rewrite(node, baseUrl)?.ToJsonString();
    }

    private static JsonNode? Rewrite(JsonNode? node, string baseUrl)
    {
        if (node is JsonObject sourceObject)
        {
            var result = new JsonObject();
            foreach ((string key, JsonNode? value) in sourceObject) result[key] = Rewrite(value, baseUrl);
            return result;
        }
        if (node is JsonArray sourceArray)
        {
            var result = new JsonArray();
            foreach (JsonNode? value in sourceArray) result.Add(Rewrite(value, baseUrl));
            return result;
        }
        if (node is JsonValue valueNode && valueNode.TryGetValue<string>(out string? text))
            return JsonValue.Create(ToUrl(text, baseUrl));
        return node?.DeepClone();
    }
}
