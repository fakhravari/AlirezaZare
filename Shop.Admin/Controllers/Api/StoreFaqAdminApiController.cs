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
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await db.StoreFaqItems.AsNoTracking()
        .Where(x => db.StoreContentPages.Any(p => p.Id == x.IdContentPage && p.PageKey == "faq"))
        .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => new FaqRow(x.Id, x.Question, x.Answer, x.LinkText, x.LinkUrl, x.SortOrder, x.IsPublished)).ToListAsync(cancellationToken));

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
        var item = id is null ? null : await db.StoreFaqItems.SingleOrDefaultAsync(x => x.Id == id && db.StoreContentPages.Any(p => p.Id == x.IdContentPage && p.PageKey == "faq"), cancellationToken);
        if (id is not null && item is null) return NotFound();
        if (id is null)
        {
            var page = await db.StoreContentPages.SingleOrDefaultAsync(p => p.PageKey == "faq", cancellationToken);
            if (page is null) return Conflict(new { title = "صفحهٔ FAQ در دیتابیس تعریف نشده است." });
            item = new Shop.Database.Domain.Store.Entities.StoreFaqItem { IdContentPage = page.Id };
            db.StoreFaqItems.Add(item);
        }
        item!.Question = question; item.Answer = answer; item.LinkText = linkText.Length == 0 ? null : linkText;
        item.LinkUrl = linkUrl.Length == 0 ? null : linkUrl; item.SortOrder = input.SortOrder; item.IsPublished = input.IsPublished;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "پرسش متداول ذخیره شد." });
    }
    [HttpDelete("{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var item = await db.StoreFaqItems.SingleOrDefaultAsync(x => x.Id == id && db.StoreContentPages.Any(p => p.Id == x.IdContentPage && p.PageKey == "faq"), cancellationToken);
        if (item is null) return NotFound();
        db.StoreFaqItems.Remove(item); await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "پرسش متداول حذف شد." });
    }
    public sealed record FaqInput(string? Question, string? Answer, string? LinkText, string? LinkUrl, int SortOrder, bool IsPublished);
    public sealed record FaqRow(long Id, string Question, string Answer, string? LinkText, string? LinkUrl, int SortOrder, bool IsPublished);
}
