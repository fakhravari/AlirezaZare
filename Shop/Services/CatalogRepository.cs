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
        if (root.GetProperty("schemaVersion").GetInt32() != 2)
            throw new InvalidDataException("Unsupported catalog schemaVersion.");

        JsonElement brands = root.GetProperty("brands"), categories = root.GetProperty("categories"),
            media = root.GetProperty("media"), colors = root.GetProperty("colors"),
            countries = root.GetProperty("countries"), details = root.GetProperty("details"),
            attributeDefinitions = root.GetProperty("attributeDefinitions");
        ValidateAttributeDefinitions(attributeDefinitions);
        HashSet<string> nonLeafCategories = ValidateCategories(categories);
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
        foreach (JsonElement detail in details.EnumerateObject().Select(item => item.Value))
        {
            if (detail.TryGetProperty("specs", out JsonElement sharedSpecs)) ValidateSpecPairs(sharedSpecs, "Shared detail specs");
            ValidateSpecGroups(detail, "specGroups", "Shared detail");
        }
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
            string code = Text(product, "code");
            if (!codes.Add(code)) throw new InvalidDataException($"Duplicate product code: {id}");
            if (slug.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
                throw new InvalidDataException($"Invalid product slug: {id}");
            JsonElement brand = Reference(brands, product, "brandId"),
                category = Reference(categories, product, "categoryId"),
                images = Reference(media, product, "mediaId");
            if (nonLeafCategories.Contains(Text(category, "id")))
                throw new InvalidDataException($"Product category must be a leaf category: {id}");
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
            ValidateProductSpecs(product);
            ValidateSpecGroups(product, "specGroups", "Product");
            products.Add(new(id, name, englishName, Text(brand, "name"), Text(category, "fa"), imagePath, slug,
                code, GetCategoryPath(category, categories)));
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

    private static IReadOnlyList<ProductCategorySeoEntry> GetCategoryPath(JsonElement category, JsonElement categories)
    {
        List<ProductCategorySeoEntry> path = [];
        HashSet<string> visited = new(StringComparer.Ordinal);
        JsonElement current = category;

        while (true)
        {
            string id = Text(current, "id");
            if (!visited.Add(id)) throw new InvalidDataException($"Category parent cycle: {id}");
            path.Add(new(Text(current, "fa"), Text(current, "slug")));

            JsonElement parent = current.GetProperty("parentId");
            if (parent.ValueKind == JsonValueKind.Null) break;
            current = categories.GetProperty(parent.GetString()!);
        }

        path.Reverse();
        return path.AsReadOnly();
    }

    private static HashSet<string> ValidateCategories(JsonElement categories)
    {
        if (categories.ValueKind != JsonValueKind.Object || !categories.EnumerateObject().Any())
            throw new InvalidDataException("Catalog categories must be a non-empty object.");

        HashSet<string> slugs = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> paths = new(StringComparer.Ordinal);
        List<(string Id, string? ParentId, string[] Path)> categoryNodes = [];
        foreach (JsonProperty property in categories.EnumerateObject())
        {
            JsonElement category = property.Value;
            string id = Text(category, "id").Trim();
            string englishName = Text(category, "en").Trim();
            string slug = Text(category, "slug").Trim();
            string persianName = Text(category, "fa").Trim();
            if (!category.TryGetProperty("parentId", out JsonElement parentElement) ||
                parentElement.ValueKind is not (JsonValueKind.Null or JsonValueKind.String))
                throw new InvalidDataException($"Category parentId is missing or invalid: {id}");
            string? parentId = parentElement.ValueKind == JsonValueKind.String
                ? parentElement.GetString()?.Trim() : null;
            if (!category.TryGetProperty("path", out JsonElement pathElement) ||
                pathElement.ValueKind != JsonValueKind.Array || pathElement.GetArrayLength() == 0)
                throw new InvalidDataException($"Category path is missing: {id}");
            string[] path = pathElement.EnumerateArray()
                .Select(segment => segment.ValueKind == JsonValueKind.String ? segment.GetString()?.Trim() ?? "" : "")
                .ToArray();

            if (!StringComparer.Ordinal.Equals(id, property.Name))
                throw new InvalidDataException($"Category key/id mismatch: {property.Name}");
            if (!englishName.Any(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z'))
                throw new InvalidDataException($"Category English name is invalid: {id}");
            if (!IsLowercaseSlug(slug))
                throw new InvalidDataException($"Invalid category slug: {id}");
            if (!slugs.Add(slug))
                throw new InvalidDataException($"Duplicate category slug: {slug}");
            if (path.Any(string.IsNullOrWhiteSpace) || !StringComparer.Ordinal.Equals(path[^1], persianName))
                throw new InvalidDataException($"Invalid category path: {id}");
            if (!paths.Add(string.Join("\u001f", path)))
                throw new InvalidDataException($"Duplicate category path: {id}");
            categoryNodes.Add((id, parentId, path));
        }

        Dictionary<string, string> categoryIdByPath = categoryNodes.ToDictionary(
            node => string.Join("\u001f", node.Path), node => node.Id, StringComparer.Ordinal);
        HashSet<string> nonLeafCategories = [];
        foreach ((string id, string? parentId, string[] path) in categoryNodes)
        {
            for (int length = 1; length < path.Length; length++)
                if (!paths.Contains(string.Join("\u001f", path.Take(length))))
                    throw new InvalidDataException($"Missing category ancestor for: {id}");

            if (path.Length == 1)
            {
                if (parentId is not null)
                    throw new InvalidDataException($"Root category cannot have a parent: {id}");
                continue;
            }

            string expectedParentPath = string.Join("\u001f", path.Take(path.Length - 1));
            if (string.IsNullOrWhiteSpace(parentId) ||
                !categoryIdByPath.TryGetValue(expectedParentPath, out string? expectedParentId) ||
                !StringComparer.Ordinal.Equals(parentId, expectedParentId))
                throw new InvalidDataException($"Invalid category parent: {id}");
            nonLeafCategories.Add(parentId);
        }

        return nonLeafCategories;
    }

    private static bool IsLowercaseSlug(string value) =>
        value.Length > 0 && value[0] != '-' && value[^1] != '-' && !value.Contains("--", StringComparison.Ordinal) &&
        value.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

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

    private static void ValidateProductSpecs(JsonElement product)
    {
        if (!product.TryGetProperty("specs", out JsonElement specs)) return;
        ValidateSpecPairs(specs, "Product specs");
    }

    private static void ValidateAttributeDefinitions(JsonElement definitions)
    {
        if (definitions.ValueKind != JsonValueKind.Object || definitions.EnumerateObject().Count() == 0)
            throw new InvalidDataException("Catalog attributeDefinitions must be a non-empty object.");
        foreach (JsonProperty attribute in definitions.EnumerateObject())
        {
            JsonElement value = attribute.Value;
            if (attribute.Name.Length == 0 || attribute.Name[0] is < 'a' or > 'z' || attribute.Name.Any(character =>
                !(character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-')))
                throw new InvalidDataException("Catalog attribute ids must be lower-camel identifiers or lowercase slugs.");
            _ = Text(value, "label");
            _ = Text(value, "icon");
            _ = Text(value, "source");
            _ = value.GetProperty("is_filterable").GetBoolean();
            _ = value.GetProperty("is_highlight").GetBoolean();
            if (value.TryGetProperty("filterKey", out JsonElement filterKey) &&
                filterKey.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Catalog attribute filterKey must be a string.");
        }
    }

    private static void ValidateSpecGroups(JsonElement item, string propertyName, string context)
    {
        if (!item.TryGetProperty(propertyName, out JsonElement groups)) return;
        if (groups.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"{context} specGroups must be an array.");
        HashSet<string> titles = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> labels = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement group in groups.EnumerateArray())
        {
            string title = Text(group, "title").Trim();
            if (title.Length == 0) throw new InvalidDataException($"{context} spec group titles cannot be empty.");
            if (!titles.Add(title)) throw new InvalidDataException("Product spec group titles must be unique.");
            if (!group.TryGetProperty("specs", out JsonElement specs) || specs.ValueKind != JsonValueKind.Array || specs.GetArrayLength() == 0)
                throw new InvalidDataException("Each product spec group needs at least one spec.");
            ValidateSpecPairs(specs, $"{context} spec group");
            foreach (JsonElement pair in specs.EnumerateArray())
                if (!labels.Add(pair[0].GetString()!.Trim()))
                    throw new InvalidDataException("A product spec label cannot be repeated across groups.");
        }
    }

    private static void ValidateSpecPairs(JsonElement specs, string context)
    {
        if (specs.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"{context} must be an array of label/value pairs.");
        HashSet<string> labels = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement pair in specs.EnumerateArray())
        {
            if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
                throw new InvalidDataException($"{context} entries must contain a label and value.");
            string label = pair[0].GetString()?.Trim() ?? "";
            string value = pair[1].GetString()?.Trim() ?? "";
            if (label.Length == 0 || value.Length == 0 || !labels.Add(label))
                throw new InvalidDataException($"{context} labels and values must be non-empty and labels unique.");
        }
    }

    private static string Text(JsonElement item, string property) =>
        item.GetProperty(property).GetString() is { Length: > 0 } text
            ? text : throw new InvalidDataException($"Missing catalog field: {property}");

    private static JsonElement Reference(JsonElement collection, JsonElement item, string property) =>
        collection.TryGetProperty(Text(item, property), out JsonElement result)
            ? result : throw new InvalidDataException($"Broken catalog relation: {property}");
}
