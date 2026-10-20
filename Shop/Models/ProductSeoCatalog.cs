using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Shop.Models;

public static class ProductMediaCatalog
{
    public const int ImageGroupCount = 4;

    public static string ForProduct(string englishName, int row)
    {
        string fileName = ProductFileName(englishName, row);
        return $"{ProductDirectory(englishName, row)}/images/main/{fileName}.png";
    }

    public static string ForCard(string englishName, int row)
    {
        string fileName = ProductFileName(englishName, row);
        return $"{ProductDirectory(englishName, row)}/images/card/{fileName}-800.jpg";
    }

    public static string ForThumbnail(string englishName, int row)
    {
        string fileName = ProductFileName(englishName, row);
        return $"{ProductDirectory(englishName, row)}/images/thumbnail/{fileName}-256.jpg";
    }

    private static string ProductDirectory(string englishName, int row)
    {
        string slug = Regex.Replace(englishName.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        string rowCode = row.ToString("D3", CultureInfo.InvariantCulture);
        return $"content/products/{slug}-hn-{rowCode}-row-{rowCode}";
    }

    private static string ProductFileName(string englishName, int row)
    {
        string slug = Regex.Replace(englishName.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        string rowCode = row.ToString("D3", CultureInfo.InvariantCulture);
        return $"{slug}-hn-{rowCode}-row-{rowCode}";
    }

    public static string GetContentPath(string fileName) => fileName switch
    {
        "tool-hammer-drill.png" => ForProduct("Cordless Hammer Drill", 1),
        "tool-multimeter-kit.png" => ForProduct("Digital Multimeter", 6),
        "tool-soldering-station.png" => ForProduct("Digital Soldering Station", 11),
        "tool-led-worklight.png" => ForProduct("Portable LED Work Light", 16),
        _ => throw new ArgumentOutOfRangeException(nameof(fileName), fileName, "Unknown product media file.")
    };
}

public sealed record ProductSeoEntry(
    int Id,
    string Name,
    string EnglishName,
    string Brand,
    string Type,
    string ImagePath)
{
    public string Slug => Regex.Replace(
        EnglishName.Normalize(NormalizationForm.FormKD).ToLowerInvariant(),
        "[^a-z0-9]+",
        "-").Trim('-');

    public string Description =>
        $"{Name} از برند {Brand}، در گروه {Type}. مشخصات فنی، تصاویر، ویژگی‌های قابل انتخاب و اقلام همراه این کالا را بررسی کنید.";
}

public static class ProductSeoCatalog
{
    public static IReadOnlyList<ProductSeoEntry> Load(string webRootPath)
    {
        string catalogPath = Path.Combine(webRootPath, "shop", "js", "store", "catalog.js");
        if (!File.Exists(catalogPath)) return [];

        string source = File.ReadAllText(catalogPath);
        int catalogEnd = source.IndexOf("window.HNProducts =", StringComparison.Ordinal);
        if (catalogEnd < 0) return [];

        MatchCollection rows = Regex.Matches(source[..catalogEnd], @"(?ms)^  \[\r?\n(.*?)^  \],");
        Regex stringField = new("\"([^\"]*)\"");
        Regex imageField = new(@"(?m)^\s*(\d+),\s*$");
        List<ProductSeoEntry> products = [];
        int id = 0;

        foreach (Match row in rows)
        {
            id++;
            MatchCollection fields = stringField.Matches(row.Groups[1].Value);
            Match imageIndexMatch = imageField.Matches(row.Groups[1].Value).Cast<Match>().LastOrDefault()!;
            if (fields.Count < 4 || imageIndexMatch is null) continue;

            int imageIndex = int.Parse(imageIndexMatch.Groups[1].Value);
            if (imageIndex < 0 || imageIndex >= ProductMediaCatalog.ImageGroupCount) continue;

            products.Add(new ProductSeoEntry(
                id,
                fields[0].Groups[1].Value,
                fields[1].Groups[1].Value,
                fields[2].Groups[1].Value,
                fields[3].Groups[1].Value,
                ProductMediaCatalog.ForProduct(fields[1].Groups[1].Value, id)));
        }

        return products;
    }
}
