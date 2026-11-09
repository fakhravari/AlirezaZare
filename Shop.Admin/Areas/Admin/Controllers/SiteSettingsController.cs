using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "system.full-access")]
public sealed class SiteSettingsController(ShopDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await db.Database.SqlQueryRaw<SiteSettingsInput>("""
            SELECT [DisplayName], [StoreName], [VisualBrandName], [SupportPhoneDisplay], [SupportPhoneUri],
                   [WhatsAppNumber], [PublicEmail], [IdWarehouse], [SmtpHost], [SmtpPort], [SmtpEnableSsl],
                   [SmtpUsername], [SmtpFrom], [FtpHost], [FtpPort], [FtpUsername],
                   CAST(NULL AS nvarchar(1)) AS [FtpPassword],
                   CAST(CASE WHEN [FtpPassword] IS NULL AND [FtpPasswordProtected] IS NULL THEN 0 ELSE 1 END AS bit) AS [FtpPasswordConfigured]
            FROM [Store].[SiteSetting] WHERE [Id] = 1
            """).SingleOrDefaultAsync(cancellationToken);
        if (model is null) return NotFound();
        await PopulateWarehouses(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsInput model, CancellationToken cancellationToken)
    {
        if (model.IdWarehouse.HasValue && !await db.Warehouses.AnyAsync(w => w.Id == model.IdWarehouse && w.IsActive && w.IsSalesWarehouse && w.Company.IsActive, cancellationToken))
            ModelState.AddModelError(nameof(model.IdWarehouse), "انبار باید انبار فروش فعال یک شرکت فعال باشد.");
        if (!ModelState.IsValid)
        {
            await PopulateWarehouses(cancellationToken);
            return View(model);
        }

        int updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [Store].[SiteSetting] SET [DisplayName]={model.DisplayName}, [StoreName]={model.StoreName},
                [VisualBrandName]={model.VisualBrandName}, [SupportPhoneDisplay]={model.SupportPhoneDisplay},
                [SupportPhoneUri]={model.SupportPhoneUri}, [WhatsAppNumber]={model.WhatsAppNumber},
                [PublicEmail]={model.PublicEmail}, [IdWarehouse]={model.IdWarehouse}, [SmtpHost]={model.SmtpHost},
                [SmtpPort]={model.SmtpPort}, [SmtpEnableSsl]={model.SmtpEnableSsl}, [SmtpUsername]={model.SmtpUsername},
                [SmtpFrom]={model.SmtpFrom}, [FtpHost]={model.FtpHost}, [FtpPort]={model.FtpPort},
                [FtpUsername]={model.FtpUsername}, [FtpPassword]=COALESCE({model.FtpPassword}, [FtpPassword]),
                [FtpPasswordProtected]=CASE WHEN {model.FtpPassword} IS NULL THEN [FtpPasswordProtected] ELSE NULL END,
                [IdentityUpdatedAtUtc]=SYSUTCDATETIME(), [UpdatedAtUtc]=SYSUTCDATETIME(),
                [UpdatedByUserKey]={User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value}
            WHERE [Id]=1
            """, cancellationToken);
        if (updated == 0) return NotFound();
        TempData["SiteSettingsSaved"] = true;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateWarehouses(CancellationToken cancellationToken) =>
        ViewData["Warehouses"] = await db.Warehouses.AsNoTracking()
            .Where(w => w.IsActive && w.IsSalesWarehouse && w.Company.IsActive)
            .OrderBy(w => w.Name).Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            { Value = w.Id.ToString(), Text = w.Name + " (" + w.Company.Name + ")" })
            .ToListAsync(cancellationToken);
}

public sealed class SiteSettingsInput
{
    [Required, StringLength(160)] public string DisplayName { get; set; } = "";
    [Required, StringLength(240)] public string StoreName { get; set; } = "";
    [Required, StringLength(160)] public string VisualBrandName { get; set; } = "";
    [Required, StringLength(32)] public string SupportPhoneDisplay { get; set; } = "";
    [Required, StringLength(40), RegularExpression(@"\+?[0-9]+", ErrorMessage = "شمارهٔ تماس باید با ارقام انگلیسی و در صورت نیاز + آغاز شود.")] public string SupportPhoneUri { get; set; } = "";
    [Required, StringLength(32), RegularExpression(@"[0-9]+", ErrorMessage = "شمارهٔ واتس‌اپ باید فقط شامل ارقام انگلیسی باشد.")] public string WhatsAppNumber { get; set; } = "";
    [Required, StringLength(254), EmailAddress] public string PublicEmail { get; set; } = "";
    public long? IdWarehouse { get; set; }
    [Required, StringLength(255)] public string SmtpHost { get; set; } = "";
    [Required, Range(1, 65535)] public int? SmtpPort { get; set; }
    public bool SmtpEnableSsl { get; set; }
    [Required, StringLength(320)] public string SmtpUsername { get; set; } = "";
    [Required, StringLength(320), EmailAddress] public string SmtpFrom { get; set; } = "";
    [StringLength(255)] public string? FtpHost { get; set; }
    [Range(1, 65535)] public int? FtpPort { get; set; }
    [StringLength(320)] public string? FtpUsername { get; set; }
    [StringLength(1024)] public string? FtpPassword { get; set; }
    public bool FtpPasswordConfigured { get; set; }
}
