using System.Text;

namespace Shop.Data.Helpers.Extensions;

public static class StringExtensions
{
    public static string Fa2En(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var result = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            char normalized = character switch
            {
                >= '۰' and <= '۹' => (char)('0' + character - '۰'),
                >= '٠' and <= '٩' => (char)('0' + character - '٠'),
                _ => character
            };

            result.Append(normalized);
        }

        return result.ToString();
    }
}
