using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/faq")]
public sealed class StoreFaqAdminApiController(ShopDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await db.Database.SqlQueryRaw<FaqRow>("""
        SELECT f.[Id], f.[Question], f.[Answer], f.[LinkText], f.[LinkUrl], f.[SortOrder], f.[IsPublished]
        FROM [Store].[FaqItem] f JOIN [Store].[ContentPage] p ON p.[Id] = f.[IdContentPage]
        WHERE p.[PageKey] = N'faq' ORDER BY f.[SortOrder], f.[Id]
        """).ToListAsync(cancellationToken));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] FaqInput input, CancellationToken cancellationToken) => Save(null, input, cancellationToken);

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Update(long id, [FromBody] FaqInput input, CancellationToken cancellationToken) => Save(id, input, cancellationToken);

    private async Task<IActionResult> Save(long? id, FaqInput input, CancellationToken cancellationToken)
    {
        string question = input.Question?.Trim() ?? "", answer = input.Answer?.Trim() ?? "";
        string linkText = input.LinkText?.Trim() ?? "", linkUrl = input.LinkUrl?.Trim() ?? "";
        if (id is <= 0 || question.Length is < 1 or > 1000 || answer.Length is < 1 or > 10000 ||
            linkText.Length > 300 || linkUrl.Length > 1000 || input.SortOrder is < 0 or > 1000000 ||
            (linkText.Length == 0) != (linkUrl.Length == 0) ||
            (linkUrl.Length > 0 && (!linkUrl.StartsWith('/') || !Url.IsLocalUrl(linkUrl) || linkUrl.Any(char.IsControl))))
            return BadRequest(new { title = "پرسش، پاسخ، ترتیب یا پیوند معتبر نیست." });
        int changed = id is null
            ? await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT [Store].[FaqItem] ([IdContentPage], [Question], [Answer], [LinkText], [LinkUrl], [SortOrder], [IsPublished])
                SELECT [Id], {question}, {answer}, NULLIF({linkText}, N''), NULLIF({linkUrl}, N''), {input.SortOrder}, {input.IsPublished}
                FROM [Store].[ContentPage] WHERE [PageKey] = N'faq'
                """, cancellationToken)
            : await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE f SET [Question] = {question}, [Answer] = {answer}, [LinkText] = NULLIF({linkText}, N''),
                    [LinkUrl] = NULLIF({linkUrl}, N''), [SortOrder] = {input.SortOrder}, [IsPublished] = {input.IsPublished}
                FROM [Store].[FaqItem] f JOIN [Store].[ContentPage] p ON p.[Id] = f.[IdContentPage]
                WHERE f.[Id] = {id} AND p.[PageKey] = N'faq'
                """, cancellationToken);
        return changed == 0 ? NotFound() : Ok(new { message = "پرسش متداول ذخیره شد." });
    }
    public sealed record FaqInput(string? Question, string? Answer, string? LinkText, string? LinkUrl, int SortOrder, bool IsPublished);
    public sealed class FaqRow
    {
        public long Id { get; set; }
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public string? LinkText { get; set; }
        public string? LinkUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; }
    }
}
