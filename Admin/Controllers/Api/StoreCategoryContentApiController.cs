using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/category-content")]
public sealed class StoreCategoryContentApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        var category = await db.StoreCategories.AsNoTracking().SingleOrDefaultAsync(x => x.CategoryKey == key, cancellationToken);
        return category is null ? NotFound() : Ok(new
        {
            intro = category.ProductIntro ?? "",
            highlights = category.ProductHighlightsJson ?? "[]",
            specs = category.ProductSpecsJson ?? "[]"
        });
    }

    [HttpPut("{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, [FromBody] CategoryContentInput input, CancellationToken cancellationToken)
    {
        if ((input.Intro?.Length ?? 0) > 10000 || !ValidArray(input.Highlights, false) || !ValidArray(input.Specs, true))
            return ValidationProblem(detail: "معرفی یا قالب نکات و مشخصات معتبر نیست.", statusCode: 400);
        var category = await db.StoreCategories.SingleOrDefaultAsync(x => x.CategoryKey == key, cancellationToken);
        if (category is null) return NotFound();
        category.ProductIntro = input.Intro ?? "";
        category.ProductHighlightsJson = input.Highlights!;
        category.ProductSpecsJson = input.Specs!;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "محتوای دسته ذخیره شد." });
    }

    private static bool ValidArray(string? json, bool pairs)
    {
        if (json is null || json.Length > 30000) return false;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() <= 100 &&
                   doc.RootElement.EnumerateArray().All(x => pairs
                       ? x.ValueKind == JsonValueKind.Array && x.GetArrayLength() == 2 && x.EnumerateArray().All(ValidText)
                       : ValidText(x));
        }
        catch (JsonException) { return false; }
    }

    private static bool ValidText(JsonElement value) => value.ValueKind == JsonValueKind.String &&
        value.GetString() is { Length: > 0 and <= 1000 } text && !string.IsNullOrWhiteSpace(text);

}

public sealed record CategoryContentInput(string? Intro, string? Highlights, string? Specs);
