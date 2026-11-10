using System.Data;
using System.Data.Common;
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
        DbConnection connection = db.Database.GetDbConnection();
        bool close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT [ProductIntro], [ProductHighlightsJson], [ProductSpecsJson] FROM [Store].[Category] WHERE [CategoryKey] = @key";
            Add(command, "@key", key);
            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return NotFound();
            return Ok(new { intro = reader.IsDBNull(0) ? "" : reader.GetString(0), highlights = reader.IsDBNull(1) ? "[]" : reader.GetString(1), specs = reader.IsDBNull(2) ? "[]" : reader.GetString(2) });
        }
        finally { if (close) await connection.CloseAsync(); }
    }

    [HttpPut("{key}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string key, [FromBody] CategoryContentInput input, CancellationToken cancellationToken)
    {
        if ((input.Intro?.Length ?? 0) > 10000 || !ValidArray(input.Highlights, false) || !ValidArray(input.Specs, true))
            return ValidationProblem(detail: "معرفی یا قالب نکات و مشخصات معتبر نیست.", statusCode: 400);
        DbConnection connection = db.Database.GetDbConnection();
        bool close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(cancellationToken);
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE [Store].[Category] SET [ProductIntro] = @intro, [ProductHighlightsJson] = @highlights, [ProductSpecsJson] = @specs WHERE [CategoryKey] = @key";
            Add(command, "@key", key); Add(command, "@intro", input.Intro ?? "");
            Add(command, "@highlights", input.Highlights!); Add(command, "@specs", input.Specs!);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 0 ? NotFound() : Ok(new { message = "محتوای دسته ذخیره شد." });
        }
        finally { if (close) await connection.CloseAsync(); }
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

    private static void Add(DbCommand command, string name, string value)
    {
        DbParameter parameter = command.CreateParameter(); parameter.ParameterName = name; parameter.Value = value; command.Parameters.Add(parameter);
    }
}

public sealed record CategoryContentInput(string? Intro, string? Highlights, string? Specs);
