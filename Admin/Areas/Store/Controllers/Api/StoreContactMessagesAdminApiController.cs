using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Shop.Admin.Infrastructure;
using Shop.Data.Application.Email.Contracts;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Helpers;

namespace Shop.Admin.Areas.Store.Controllers.Api;

[Area("Store")]
[ApiController]
[Route("api/admin/store/contact-messages")]
public sealed class StoreContactMessagesAdminApiController(
    IStoreContactMessageAdminData data,
    IEmailSender emailSender,
    ILogger<StoreContactMessagesAdminApiController> logger) : ControllerBase
{
    [HttpGet("statuses")]
    public async Task<IActionResult> Statuses(CancellationToken cancellationToken) =>
        Ok(await data.ReadStatusesAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AdminListQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!AdminListQuery.TryCreate(page, pageSize, q, out AdminPageRequest paging)
            || (!string.IsNullOrEmpty(status) &&
                !await data.StatusExistsAsync(status, cancellationToken)))
            return ValidationProblem(detail: "فیلتر یا صفحه‌بندی معتبر نیست.", statusCode: 400);

        var (total, items) = await data.ReadMessagesAsync(status, paging.Search, paging.Offset, paging.PageSize,
            cancellationToken);

        return Ok(AdminListQuery.CreateResult(items, total, paging));
    }

    [HttpPut("{id:long}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] ContactMessageStatusInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Status) ||
            !await data.StatusExistsAsync(input.Status, cancellationToken))
            return ValidationProblem(detail: "وضعیت پیام معتبر نیست.", statusCode: 400);
        if (!await data.SetStatusAsync(id, input.Status, cancellationToken)) return NotFound();
        return Ok(new { message = "وضعیت پیام ذخیره شد." });
    }

    [HttpPut("{id:long}/reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(long id, [FromBody] ContactMessageReplyInput input, CancellationToken ct)
    {
        string reply = (input.Reply ?? string.Empty).Trim();
        if (reply.Length is < 2 or > 4000)
            return ValidationProblem(detail: "متن پاسخ باید بین ۲ تا ۴۰۰۰ نویسه باشد.", statusCode: 400);

        var item = await data.SaveReplyAsync(id, reply, ct);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(item.Email) || !MailAddress.TryCreate(item.Email, out var recipient))
            return Ok(new
                { saved = true, emailSent = false, message = "پاسخ ثبت شد؛ برای این پیام ایمیل مشتری ثبت نشده است." });

        try
        {
            string subject = item.SubjectKey switch
            {
                "order" => "پاسخ پیگیری سفارش", "consultation" => "پاسخ مشاوره پیش از خرید",
                "after-sales" => "پاسخ خدمات پس از فروش", "business" => "پاسخ همکاری سازمانی", _ => "پاسخ پیام شما"
            };
            await emailSender.SendAsync(recipient.Address, subject,
                $"سلام {item.Name}\n\n{System.Text.RegularExpressions.Regex.Replace(item.AdminReply ?? string.Empty, "<[^>]+>", " ")}\n\nبا احترام\nپشتیبانی فروشگاه",
                ct);
            await data.MarkReplyEmailSentAsync(item.Id, ct);
            return Ok(new { saved = true, emailSent = true, message = "پاسخ ثبت و ایمیل ارسال شد." });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Contact reply email failed for {ContactMessageId}", id);
            return Ok(new { saved = true, emailSent = false, message = "پاسخ ثبت شد اما ارسال ایمیل ناموفق بود." });
        }
    }
}

public sealed record ContactMessageStatusInput(string Status);

public sealed record ContactMessageReplyInput(string? Reply);
