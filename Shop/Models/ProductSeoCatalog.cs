using System.Text;
using System.Text.RegularExpressions;

namespace Shop.Models;

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
    private static readonly string[] Images =
    [
        "Shop/images/products/tool-hammer-drill.png",
        "Shop/images/products/tool-multimeter-kit.png",
        "Shop/images/products/tool-soldering-station.png",
        "Shop/images/products/tool-led-worklight.png"
    ];

    public static IReadOnlyList<ProductSeoEntry> Load(string webRootPath)
    {
        string catalogPath = Path.Combine(webRootPath, "Shop", "js", "store", "catalog.js");
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
            if (imageIndex < 0 || imageIndex >= Images.Length) continue;

            products.Add(new ProductSeoEntry(
                id,
                fields[0].Groups[1].Value,
                fields[1].Groups[1].Value,
                fields[2].Groups[1].Value,
                fields[3].Groups[1].Value,
                Images[imageIndex]));
        }

        return products;
    }
}
