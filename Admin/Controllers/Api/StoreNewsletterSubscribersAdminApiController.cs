using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;
namespace Shop.Admin.Controllers.Api;
[Area("Store")][ApiController][Route("api/admin/store/newsletter-subscribers")]
public sealed class StoreNewsletterSubscribersAdminApiController(ShopDbContext db) : ControllerBase
{
 [HttpGet] public async Task<IActionResult> List([FromQuery] string? q,[FromQuery] bool includeUnsubscribed=false,[FromQuery] int page=1,CancellationToken ct=default){if(page<1)return BadRequest();var query=db.StoreNewsletterSubscribers.AsNoTracking().AsQueryable();if(!includeUnsubscribed)query=query.Where(x=>x.UnsubscribedAtUtc==null);if(!string.IsNullOrWhiteSpace(q)){var term=q.Trim();query=query.Where(x=>x.Email.Contains(term));}var total=await query.CountAsync(ct);var items=await query.OrderByDescending(x=>x.SubscribedAtUtc).Skip((page-1)*50).Take(50).Select(x=>new{x.Id,x.Email,x.SubscribedAtUtc,x.UnsubscribedAtUtc}).ToListAsync(ct);return Ok(new{total,page,pageSize=50,items});}
 [HttpPut("{id:long}/status")][ValidateAntiForgeryToken] public async Task<IActionResult> Update(long id,[FromBody] NewsletterStatusInput input,CancellationToken ct){var item=await db.StoreNewsletterSubscribers.SingleOrDefaultAsync(x=>x.Id==id,ct);if(item is null)return NotFound();item.UnsubscribedAtUtc=input.Subscribed?null:DateTime.UtcNow;await db.SaveChangesAsync(ct);return Ok(new{message=input.Subscribed?"عضویت فعال شد.":"عضویت لغو شد."});}
 [HttpGet("export")] public async Task<IActionResult> Export([FromQuery] bool includeUnsubscribed=false,CancellationToken ct=default){var q=db.StoreNewsletterSubscribers.AsNoTracking();if(!includeUnsubscribed)q=q.Where(x=>x.UnsubscribedAtUtc==null);var rows=await q.OrderBy(x=>x.Email).Select(x=>new{x.Email,x.SubscribedAtUtc,x.UnsubscribedAtUtc}).ToListAsync(ct);var csv="ایمیل,تاریخ عضویت,تاریخ لغو\n"+string.Join("\n",rows.Select(x=>$"{x.Email},{x.SubscribedAtUtc:O},{x.UnsubscribedAtUtc:O}"));return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(),"text/csv","newsletter-subscribers.csv");}
}
public sealed record NewsletterStatusInput(bool Subscribed);
