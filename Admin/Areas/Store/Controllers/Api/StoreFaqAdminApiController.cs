using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Authorize(Roles = "system.full-access")]
[Route("api/admin/store/faq")]
public sealed class StoreFaqAdminApiController(IStoreContentAdminData data) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok((await data.ReadFaqItemsAsync(cancellationToken)).Select(item => new FaqRow(item.Id, item.Question,
            item.Answer, item.LinkText, item.LinkUrl, item.SortOrder, item.Published)));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Create([FromBody] FaqInput input, CancellationToken cancellationToken) =>
        Save(null, input, cancellationToken);

    [HttpPut("{id:long}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Update(long id, [FromBody] FaqInput input, CancellationToken cancellationToken) =>
        Save(id, input, cancellationToken);

    private async Task<IActionResult> Save(long? id, FaqInput input, CancellationToken cancellationToken)
    {
        string question = input.Question?.Trim() ?? "", answer = input.Answer?.Trim() ?? "";
        string linkText = input.LinkText?.Trim() ?? "", linkUrl = input.LinkUrl?.Trim() ?? "";
        if (id is <= 0 || question.Length is < 1 or > 1000 || answer.Length is < 1 or > 10000 ||
            linkText.Length > 300 || linkUrl.Length > 1000 || input.SortOrder is < 0 or > 1000000 ||
            (linkText.Length == 0) != (linkUrl.Length == 0) ||
            (linkUrl.Length > 0 &&
             (!linkUrl.StartsWith('/') || !Url.IsLocalUrl(linkUrl) || linkUrl.Any(char.IsControl))))
            return BadRequest(new { title = "پرسش، پاسخ، ترتیب یا پیوند معتبر نیست." });

        StoreFaqSaveResult result = await data.SaveFaqAsync(id, new StoreFaqAdminEdit(question, answer,
            linkText.Length == 0 ? null : linkText, linkUrl.Length == 0 ? null : linkUrl, input.SortOrder,
            input.IsPublished), cancellationToken);
        if (result == StoreFaqSaveResult.NotFound) return NotFound();
        if (result == StoreFaqSaveResult.FaqPageMissing)
            return Conflict(new { title = "صفحهٔ FAQ در دیتابیس تعریف نشده است." });

        return Ok(new { message = "پرسش متداول ذخیره شد." });
    }

    [HttpDelete("{id:long}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (!await data.DeleteFaqAsync(id, cancellationToken)) return NotFound();

        return Ok(new { message = "پرسش متداول حذف شد." });
    }

    public sealed record FaqInput(
        string? Question,
        string? Answer,
        string? LinkText,
        string? LinkUrl,
        int SortOrder,
        bool IsPublished);

    public sealed record FaqRow(
        long Id,
        string Question,
        string Answer,
        string? LinkText,
        string? LinkUrl,
        int SortOrder,
        bool IsPublished);
}
