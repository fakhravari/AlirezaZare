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
        return $"content/products/images/main/{fileName}-main.png";
    }

    public static string ForCard(string englishName, int row)
    {
        string fileName = ProductFileName(englishName, row);
        return $"content/products/images/card/{fileName}-card-800.jpg";
    }

    public static string ForThumbnail(string englishName, int row)
    {
        string fileName = ProductFileName(englishName, row);
        return $"content/products/images/thumbnail/{fileName}-thumbnail-256.jpg";
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

