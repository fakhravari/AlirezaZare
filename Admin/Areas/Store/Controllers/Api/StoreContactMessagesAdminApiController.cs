using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Application.Store;
using Shop.Data.Infrastructure.Persistence;

using Shop.Data.Helpers;
namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/contact-messages")]
public sealed class StoreContactMessagesAdminApiController(ShopDbContext db, IEmailSender emailSender, ILogger<StoreContactMessagesAdminApiController> logger) : ControllerBase
{
    [HttpGet("statuses")]
    public async Task<IActionResult> Statuses(CancellationToken cancellationToken) =>
        Ok(await db.ContactMessageStatuses.AsNoTracking().OrderBy(x => x.Code).Select(x => new { x.Code, x.NameFa, x.NameEn }).ToListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging)
            || (!string.IsNullOrEmpty(status) && !await db.ContactMessageStatuses.AnyAsync(x => x.Code == status, cancellationToken)))
            return ValidationProblem(detail: "فیلتر یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        var query = db.StoreContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(x => x.StatusReference.Code == status);
        if (paging.Search.Length > 0) query = query.Where(x => x.Name.Contains(paging.Search) || x.Phone.Contains(paging.Search) || (x.Email != null && x.Email.Contains(paging.Search)) || x.Message.Contains(paging.Search));
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip(paging.Offset).Take(paging.PageSize)
            .Select(x => new { x.Id, x.Name, x.Phone, x.Email, x.SubjectKey, x.Message, Status = x.StatusReference.Code, StatusName = x.StatusReference.NameFa, x.AdminReply, x.RepliedAtUtc, x.ReplyEmailSentAtUtc, x.CreatedAtUtc }).ToListAsync(cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    [HttpPut("{id:long}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] ContactMessageStatusInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Status) || !await db.ContactMessageStatuses.AnyAsync(x => x.Code == input.Status, cancellationToken))
            return ValidationProblem(detail: "وضعیت پیام معتبر نیست.", statusCode: 400);

        var item = await db.StoreContactMessages.SingleOrDefaultAsync(x => x.Id == id, cancellationToken); if (item is null) return NotFound();

        item.IdStatus = await db.ContactMessageStatuses.Where(x => x.Code == input.Status).Select(x => x.Id).SingleAsync(cancellationToken); item.UpdatedAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken); return Ok(new { message = "وضعیت پیام ذخیره شد." });
    }

    [HttpPut("{id:long}/reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(long id, [FromBody] ContactMessageReplyInput input, CancellationToken ct)
    {
        string reply = (input.Reply ?? string.Empty).Trim();
        if (reply.Length is < 2 or > 4000) return ValidationProblem(detail: "متن پاسخ باید بین ۲ تا ۴۰۰۰ نویسه باشد.", statusCode: 400);

        var item = await db.StoreContactMessages.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return NotFound();

        item.AdminReply = StoreHtmlSanitizer.Sanitize(reply); item.RepliedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow; item.IdStatus = await db.ContactMessageStatuses.Where(x => x.Code == "closed").Select(x => x.Id).SingleAsync(ct);
        await db.SaveChangesAsync(ct);

        if (string.IsNullOrWhiteSpace(item.Email) || !MailAddress.TryCreate(item.Email, out var recipient)) return Ok(new { saved = true, emailSent = false, message = "پاسخ ثبت شد؛ برای این پیام ایمیل مشتری ثبت نشده است." });

        try
        {
            string subject = item.SubjectKey switch { "order" => "پاسخ پیگیری سفارش", "consultation" => "پاسخ مشاوره پیش از خرید", "after-sales" => "پاسخ خدمات پس از فروش", "business" => "پاسخ همکاری سازمانی", _ => "پاسخ پیام شما" };
            await emailSender.SendAsync(recipient.Address, subject, $"سلام {item.Name}\n\n{System.Text.RegularExpressions.Regex.Replace(item.AdminReply, "<[^>]+>", " ")}\n\nبا احترام\nپشتیبانی فروشگاه", ct);
            item.ReplyEmailSentAtUtc = DateTime.UtcNow; await db.SaveChangesAsync(ct); return Ok(new { saved = true, emailSent = true, message = "پاسخ ثبت و ایمیل ارسال شد." });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { logger.LogWarning(ex, "Contact reply email failed for {ContactMessageId}", id); return Ok(new { saved = true, emailSent = false, message = "پاسخ ثبت شد اما ارسال ایمیل ناموفق بود." }); }
    }
}
public sealed record ContactMessageStatusInput(string Status);
public sealed record ContactMessageReplyInput(string? Reply);
