using System.Text.Json;
using Shop.Models;

namespace Shop.Services;

/// <summary>The JSON catalog is shared by Razor, SEO and the browser. No JavaScript parsing.</summary>
public sealed class CatalogRepository(IWebHostEnvironment environment)
{
    private readonly object _gate = new();
    private readonly string _path = Path.Combine(environment.WebRootPath, "content", "data", "catalog.json");
    private CatalogSnapshot? _snapshot;
    private DateTime _lastWrite;
    private long _length;

    public CatalogSnapshot Read()
    {
        lock (_gate)
        {
            FileInfo file = new(_path);
            if (_snapshot is not null && file.LastWriteTimeUtc == _lastWrite && file.Length == _length)
                return _snapshot;

            CatalogSnapshot next = CatalogSnapshot.Parse(File.ReadAllText(_path));
            _snapshot = next;
            _lastWrite = file.LastWriteTimeUtc;
            _length = file.Length;
            return next;
        }
    }
}

public sealed record CatalogSnapshot(string BrowserJson, IReadOnlyList<ProductSeoEntry> Products,
    IReadOnlyList<BrandProfileModel> Brands)
{
    public static CatalogSnapshot Parse(string source)
    {
        using JsonDocument document = JsonDocument.Parse(source);
        JsonElement root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetInt32() != 1)
            throw new InvalidDataException("Unsupported catalog schemaVersion.");

        JsonElement brands = root.GetProperty("brands"), categories = root.GetProperty("categories"),
            media = root.GetProperty("media"), colors = root.GetProperty("colors"),
            countries = root.GetProperty("countries"), details = root.GetProperty("details");
        HashSet<int> ids = [];
        HashSet<string> codes = new(StringComparer.Ordinal);
        List<ProductSeoEntry> products = [];
        List<BrandProfileModel> brandProfiles = [];
        foreach (JsonElement brand in brands.EnumerateObject().Select(item => item.Value))
        {
            string? country = brand.GetProperty("countryCode").GetString();
            if (country is not null && !countries.TryGetProperty(country, out _))
                throw new InvalidDataException($"Unknown brand country: {country}");
            string id = Text(brand, "id");
            string? logo = brand.GetProperty("logo").GetString();
            if (logo is not null) ValidatePath(logo, "content/brands/");
            BrandProfileModel editorial = BrandCatalog.Find(id) ?? new(id, Text(brand, "name"),
                Text(brand, "fa"), null, "", "", []);
            brandProfiles.Add(editorial with { Name = Text(brand, "name"), PersianName = Text(brand, "fa"),
                LogoFile = logo is null ? null : Path.GetFileName(logo) });
        }
        foreach (JsonElement country in countries.EnumerateObject().Select(item => item.Value))
            ValidatePath(Text(country, "flag"), "content/countries/flags/");
        foreach (JsonElement images in media.EnumerateObject().Select(item => item.Value))
        {
            ValidatePath(Text(images, "main"), "content/products/images/main/");
            ValidatePath(Text(images, "card"), "content/products/images/card/");
            ValidatePath(Text(images, "thumbnail"), "content/products/images/thumbnail/");
            foreach (JsonElement image in images.GetProperty("images").EnumerateArray())
            {
                ValidatePath(Text(image, "src"), "content/products/images/main/");
                ValidatePath(Text(image, "thumbnail"), "content/products/images/thumbnail/");
            }
            foreach (JsonElement video in images.GetProperty("videos").EnumerateArray())
                ValidatePath(video.GetString() ?? "", "content/products/videos/");
        }
        foreach (JsonElement product in root.GetProperty("products").EnumerateArray())
        {
            int id = product.GetProperty("id").GetInt32();
            if (id <= 0 || !ids.Add(id)) throw new InvalidDataException($"Invalid/duplicate product id: {id}");
            string name = Text(product, "name"), englishName = Text(product, "englishName"),
                slug = Text(product, "slug");
            if (!codes.Add(Text(product, "code"))) throw new InvalidDataException($"Duplicate product code: {id}");
            if (slug.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
                throw new InvalidDataException($"Invalid product slug: {id}");
            JsonElement brand = Reference(brands, product, "brandId"),
                category = Reference(categories, product, "categoryId"),
                images = Reference(media, product, "mediaId");
            _ = Reference(details, product, "detailId");
            foreach (JsonElement color in product.GetProperty("colorIds").EnumerateArray())
                if (!colors.TryGetProperty(color.GetString()!, out _))
                    throw new InvalidDataException($"Unknown product color: {id}");
            string imagePath = Text(images, "main");
            if (product.GetProperty("price").GetDecimal() < 0 || product.GetProperty("stockQuantity").GetInt32() < 0)
                throw new InvalidDataException($"Invalid price/stock: {id}");
            int cartLimit = product.GetProperty("maxCartQuantity").GetInt32();
            decimal discount = product.GetProperty("discountPercent").GetDecimal();
            if (cartLimit is < 1 or > 99 || discount is < 0 or >= 100)
                throw new InvalidDataException($"Invalid cart limit/discount: {id}");
            ValidateOptions(product);
            products.Add(new(id, name, englishName, Text(brand, "name"), Text(category, "fa"), imagePath, slug));
        }
        if (products.Count == 0) throw new InvalidDataException("The catalog has no products.");
        foreach (JsonElement collection in root.GetProperty("collections").EnumerateObject().Select(item => item.Value))
            foreach (JsonElement id in collection.EnumerateArray())
                if (!ids.Contains(id.GetInt32())) throw new InvalidDataException("Unknown product in collection.");

        // Default System.Text.Json encoding escapes '<', '>' and '&': safe inside a JSON script block.
        return new(JsonSerializer.Serialize(root), products.AsReadOnly(), brandProfiles.AsReadOnly());
    }

    private static void ValidatePath(string path, string prefix)
    {
        if (!path.StartsWith(prefix, StringComparison.Ordinal) || path.Contains("..") || path.Contains('\\') ||
            path.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '/' or '-' or '_' or '.')))
            throw new InvalidDataException($"Invalid catalog media path: {path}");
    }

    private static void ValidateOptions(JsonElement product)
    {
        JsonElement variants = product.GetProperty("variants");
        if (variants.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
            throw new InvalidDataException("Variants must be an object or null.");
        foreach (JsonProperty variant in variants.ValueKind == JsonValueKind.Null
            ? Enumerable.Empty<JsonProperty>() : variants.EnumerateObject())
        {
            _ = Text(variant.Value, "label");
            HashSet<string> values = new(StringComparer.Ordinal);
            foreach (JsonElement option in variant.Value.GetProperty("options").EnumerateArray())
            {
                if (!values.Add(Text(option, "value"))) throw new InvalidDataException("Duplicate variant value.");
                _ = Text(option, "label");
                if (option.TryGetProperty("priceDelta", out JsonElement delta)) _ = delta.GetDecimal();
            }
            if (values.Count == 0) throw new InvalidDataException("A variant needs an option.");
        }
        JsonElement accessoryList = product.GetProperty("accessories");
        if (accessoryList.ValueKind == JsonValueKind.Null) return;
        if (accessoryList.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Accessories must be an array or null.");
        HashSet<string> accessories = new(StringComparer.Ordinal);
        foreach (JsonElement accessory in accessoryList.EnumerateArray())
        {
            if (!accessories.Add(Text(accessory, "id"))) throw new InvalidDataException("Duplicate accessory id.");
            _ = Text(accessory, "name");
            _ = accessory.GetProperty("required").GetBoolean();
            if (accessory.GetProperty("price").GetDecimal() < 0) throw new InvalidDataException("Invalid accessory price.");
        }
    }

    private static string Text(JsonElement item, string property) =>
        item.GetProperty(property).GetString() is { Length: > 0 } text
            ? text : throw new InvalidDataException($"Missing catalog field: {property}");

    private static JsonElement Reference(JsonElement collection, JsonElement item, string property) =>
        collection.TryGetProperty(Text(item, property), out JsonElement result)
            ? result : throw new InvalidDataException($"Broken catalog relation: {property}");
}
