using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "system.full-access")]
public sealed class SiteSettingsController(ShopDbContext db, IDataProtectionProvider protection) : Controller
{
    [HttpGet]
    public IActionResult Slider() => View();

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await db.StoreSiteIdentities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (settings is null) return NotFound();
        var model = new SiteSettingsInput
        {
            DisplayName = settings.DisplayName, StoreName = settings.StoreName, VisualBrandName = settings.VisualBrandName,
            SupportPhoneDisplay = settings.SupportPhoneDisplay, SupportPhoneUri = settings.SupportPhoneUri,
            WhatsAppNumber = settings.WhatsAppNumber, PublicEmail = settings.PublicEmail, MediaCdnBaseUrl = settings.MediaCdnBaseUrl,
            IdWarehouse = settings.IdWarehouse, SmtpHost = settings.SmtpHost ?? "", SmtpPort = settings.SmtpPort,
            SmtpEnableSsl = settings.SmtpEnableSsl ?? false, SmtpUsername = settings.SmtpUsername ?? "",
            SmtpFrom = settings.SmtpFrom ?? "", SmtpPassword = settings.SmtpPassword,
            EmailVerificationCodeLifetimeMinutes = settings.EmailVerificationCodeLifetimeMinutes,
            FtpHost = settings.FtpHost, FtpPort = settings.FtpPort, FtpUsername = settings.FtpUsername,
            FtpPassword = settings.FtpPassword, LegacyFtpPassword = settings.FtpPasswordProtected,
            FtpPasswordConfigured = settings.FtpPassword is not null || settings.FtpPasswordProtected is not null
        };
        bool ftpPasswordUnreadable = false;
        if (string.IsNullOrEmpty(model.FtpPassword) && !string.IsNullOrEmpty(model.LegacyFtpPassword))
        {
            try { model.FtpPassword = protection.CreateProtector("Shop.Admin.FtpPassword.v1").Unprotect(model.LegacyFtpPassword); }
            catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or FormatException)
            { ftpPasswordUnreadable = true; ViewData["FtpPasswordReadError"] = "رمز ذخیره‌شدهٔ FTP قابل بازیابی نیست؛ آن را دوباره وارد کنید."; }
        }
        model.LegacyFtpPassword = null;
        ViewData["FtpPasswordStatus"] = !model.FtpPasswordConfigured ? "ثبت نشده"
            : ftpPasswordUnreadable ? "ثبت شده، اما قابل بازیابی نیست"
            : string.IsNullOrEmpty(model.FtpPassword) ? "ثبت شده، اما مقدار رمز خالی است"
            : "ثبت شده و نمایش داده می‌شود";
        ViewData["SmtpPasswordStatus"] = string.IsNullOrEmpty(model.SmtpPassword)
            ? "ثبت نشده" : "ثبت شده و نمایش داده می‌شود";
        await PopulateWarehouses(cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsInput model, CancellationToken cancellationToken)
    {
        try { model.MediaCdnBaseUrl = MediaCdnOptions.NormalizeBaseUrl(model.MediaCdnBaseUrl); }
        catch (InvalidOperationException) { ModelState.AddModelError(nameof(model.MediaCdnBaseUrl), "نشانی CDN باید مبدأ HTTPS بدون مسیر اضافی باشد."); }
        if (model.IdWarehouse.HasValue && !await db.Warehouses.AnyAsync(w => w.Id == model.IdWarehouse && w.IsActive && w.IsSalesWarehouse && w.Company.IsActive, cancellationToken))
            ModelState.AddModelError(nameof(model.IdWarehouse), "انبار باید انبار فروش فعال یک شرکت فعال باشد.");
        if (!ModelState.IsValid)
        {
            await PopulateWarehouses(cancellationToken);
            return View(model);
        }

        var settings = await db.StoreSiteIdentities.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (settings is null) return NotFound();
        settings.DisplayName = model.DisplayName; settings.StoreName = model.StoreName;
        settings.VisualBrandName = model.VisualBrandName; settings.SupportPhoneDisplay = model.SupportPhoneDisplay;
        settings.SupportPhoneUri = model.SupportPhoneUri; settings.WhatsAppNumber = model.WhatsAppNumber;
        settings.PublicEmail = model.PublicEmail; settings.MediaCdnBaseUrl = model.MediaCdnBaseUrl; settings.IdWarehouse = model.IdWarehouse;
        settings.SmtpHost = model.SmtpHost; settings.SmtpPort = model.SmtpPort; settings.SmtpEnableSsl = model.SmtpEnableSsl;
        settings.SmtpUsername = model.SmtpUsername; settings.SmtpFrom = model.SmtpFrom;
        if (model.SmtpPassword is not null) settings.SmtpPassword = model.SmtpPassword;
        settings.EmailVerificationCodeLifetimeMinutes = model.EmailVerificationCodeLifetimeMinutes;
        settings.FtpHost = model.FtpHost; settings.FtpPort = model.FtpPort; settings.FtpUsername = model.FtpUsername;
        if (model.FtpPassword is not null) { settings.FtpPassword = model.FtpPassword; settings.FtpPasswordProtected = null; }
        settings.UpdatedAtUtc = DateTime.UtcNow;
        settings.UpdatedByUserKey = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        await db.SaveChangesAsync(cancellationToken);
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
    [Required, StringLength(500)] public string MediaCdnBaseUrl { get; set; } = MediaCdnOptions.DefaultBaseUrl;
    public long? IdWarehouse { get; set; }
    [Required, StringLength(255)] public string SmtpHost { get; set; } = "";
    [Required, Range(1, 65535)] public int? SmtpPort { get; set; }
    public bool SmtpEnableSsl { get; set; }
    [Required, StringLength(320)] public string SmtpUsername { get; set; } = "";
    [Required, StringLength(320), EmailAddress] public string SmtpFrom { get; set; } = "";
    [StringLength(1024)] public string? SmtpPassword { get; set; }
    [Range(1, 1440)] public int EmailVerificationCodeLifetimeMinutes { get; set; } = 15;
    [StringLength(255)] public string? FtpHost { get; set; }
    [Range(1, 65535)] public int? FtpPort { get; set; }
    [StringLength(320)] public string? FtpUsername { get; set; }
    [StringLength(1024)] public string? FtpPassword { get; set; }
    public bool FtpPasswordConfigured { get; set; }
    public string? LegacyFtpPassword { get; set; }
}
