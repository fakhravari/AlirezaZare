using System.Text.Json;

namespace Shop.Services;

public sealed record CheckoutOptions(Dictionary<string, string> PriceOptions, HashSet<string> AccessoryIds)
{
    public static bool TryRead(JsonElement? input, out CheckoutOptions result)
    {
        var prices = new Dictionary<string, string>(StringComparer.Ordinal);
        var accessories = new HashSet<string>(StringComparer.Ordinal);
        result = new(prices, accessories);
        if (!input.HasValue || input.Value.ValueKind == JsonValueKind.Null) return true;
        var options = input.Value;
        if (options.ValueKind != JsonValueKind.Object) return false;

        if (options.TryGetProperty("priceOptions", out var priceOptions) || options.TryGetProperty("variants", out priceOptions))
        {
            if (priceOptions.ValueKind != JsonValueKind.Array) return false;
            foreach (var option in priceOptions.EnumerateArray())
            {
                if (option.ValueKind != JsonValueKind.Object ||
                    !option.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String ||
                    !option.TryGetProperty("productValue", out var value) || value.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(key.GetString()) || string.IsNullOrWhiteSpace(value.GetString()) ||
                    !prices.TryAdd(key.GetString()!, value.GetString()!)) return false;
            }
        }

        if (options.TryGetProperty("accessories", out var accessoryList))
        {
            if (accessoryList.ValueKind != JsonValueKind.Array) return false;
            foreach (var accessory in accessoryList.EnumerateArray())
            {
                if (accessory.ValueKind != JsonValueKind.Object ||
                    !accessory.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(id.GetString())) return false;
                if (accessory.TryGetProperty("selected", out var selected))
                {
                    if (selected.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
                    if (selected.ValueKind == JsonValueKind.False) continue;
                }
                if (!accessories.Add(id.GetString()!)) return false;
            }
        }
        return true;
    }
}
