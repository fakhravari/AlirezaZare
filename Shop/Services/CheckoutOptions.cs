using System.Text.Json;

namespace Shop.Services;

public sealed record CheckoutOptions(HashSet<string> AccessoryIds)
{
    public static bool TryRead(JsonElement? input, out CheckoutOptions result)
    {
        var accessories = new HashSet<string>(StringComparer.Ordinal);
        result = new(accessories);
        if (!input.HasValue || input.Value.ValueKind == JsonValueKind.Null) return true;

        var options = input.Value;
        if (options.ValueKind != JsonValueKind.Object) return false;

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
