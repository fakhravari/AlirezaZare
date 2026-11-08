using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Shop.Database.Application.Store.Contracts;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/catalog")]
public sealed class StoreCatalogAdminApiController(IStoreRuntimeDatasetRepository datasets) : ControllerBase
{
    [HttpGet("{section}")]
    public async Task<IActionResult> GetSection(
        string section,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 200)
            return ValidationProblem("شماره صفحه باید مثبت و اندازهٔ صفحه بین ۱ تا ۲۰۰ باشد.");

        string? json = await datasets.ReadJsonAsync("catalog", cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "دادهٔ کاتالوگ در دیتابیس در دسترس نیست.");

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        string query = q?.Trim() ?? "";

        return section.ToLowerInvariant() switch
        {
            "products" => Ok(Page(Products(root).Where(item => Matches(query,
                item.Code, item.Name, item.EnglishName, item.BrandName, item.CategoryName)), page, pageSize)),
            "categories" => Ok(Page(Categories(root).Where(item => Matches(query,
                item.Name, item.EnglishName, item.Slug)), page, pageSize)),
            "brands" => Ok(Page(Brands(root).Where(item => Matches(query,
                item.Name, item.EnglishName)), page, pageSize)),
            "attributes" => Ok(Page(Attributes(root).Where(item => Matches(query,
                item.Key, item.Name, item.Source)), page, pageSize)),
            _ => NotFound()
        };
    }

    private static IReadOnlyList<ProductRow> Products(JsonElement root)
    {
        Dictionary<string, string> brands = root.GetProperty("brands").EnumerateObject()
            .ToDictionary(item => item.Name, item => Text(item.Value, "fa", Text(item.Value, "name", item.Name)));
        Dictionary<string, string> categories = root.GetProperty("categories").EnumerateObject()
            .ToDictionary(item => item.Name, item => Text(item.Value, "fa", item.Name));

        return root.GetProperty("products").EnumerateArray().Select(item =>
        {
            string brandId = Text(item, "brandId", "");
            string categoryId = Text(item, "categoryId", "");
            return new ProductRow(
                item.GetProperty("id").GetInt32(),
                Text(item, "code", ""),
                Text(item, "name", ""),
                Text(item, "englishName", ""),
                brands.GetValueOrDefault(brandId, ""),
                categories.GetValueOrDefault(categoryId, ""),
                item.TryGetProperty("price", out JsonElement price) ? price.GetDecimal() : 0,
                item.TryGetProperty("stockQuantity", out JsonElement stock) ? stock.GetInt32() : 0,
                item.TryGetProperty("discountPercent", out JsonElement discount) ? discount.GetDecimal() : 0);
        }).ToArray();
    }

    private static IReadOnlyList<CategoryRow> Categories(JsonElement root) =>
        root.GetProperty("categories").EnumerateObject().Select(item => new CategoryRow(
            Text(item.Value, "id", item.Name),
            Text(item.Value, "fa", ""),
            Text(item.Value, "en", ""),
            Text(item.Value, "slug", ""),
            Text(item.Value, "parentId", ""))).ToArray();

    private static IReadOnlyList<BrandRow> Brands(JsonElement root) =>
        root.GetProperty("brands").EnumerateObject().Select(item => new BrandRow(
            Text(item.Value, "id", item.Name),
            Text(item.Value, "fa", Text(item.Value, "name", "")),
            Text(item.Value, "name", ""),
            Text(item.Value, "countryCode", ""))).ToArray();

    private static IReadOnlyList<AttributeRow> Attributes(JsonElement root) =>
        root.GetProperty("attributeDefinitions").EnumerateObject().Select(item => new AttributeRow(
            item.Name,
            Text(item.Value, "label", ""),
            Text(item.Value, "source", ""),
            item.Value.TryGetProperty("is_filterable", out JsonElement filterable) && filterable.GetBoolean(),
            item.Value.TryGetProperty("is_highlight", out JsonElement highlight) && highlight.GetBoolean())).ToArray();

    private static object Page<T>(IEnumerable<T> rows, int page, int pageSize)
    {
        T[] items = rows.ToArray();
        return new
        {
            total = items.Length,
            page,
            pageSize,
            items = items.Skip((page - 1) * pageSize).Take(pageSize)
        };
    }

    private static bool Matches(string query, params string?[] values) =>
        query.Length == 0 || values.Any(value => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);

    private static string Text(JsonElement item, string property, string fallback) =>
        item.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private sealed record ProductRow(int Id, string Code, string Name, string EnglishName, string BrandName,
        string CategoryName, decimal Price, int StockQuantity, decimal DiscountPercent);
    private sealed record CategoryRow(string Id, string Name, string EnglishName, string Slug, string ParentId);
    private sealed record BrandRow(string Id, string Name, string EnglishName, string CountryCode);
    private sealed record AttributeRow(string Key, string Name, string Source, bool IsFilterable, bool IsHighlight);
}
