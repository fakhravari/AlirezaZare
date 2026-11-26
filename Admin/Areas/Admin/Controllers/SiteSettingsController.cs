using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Shop.Data.Application.Store;
using Shop.Data.Application.Store.Contracts;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "system.full-access")]
public sealed class SiteSettingsController(IStoreSiteSettingsAdminData data, IDataProtectionProvider protection) : Controller
{
    [HttpGet]
    public IActionResult Slider() => View();

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await data.ReadAsync(cancellationToken);
        if (settings is null) return NotFound();

        var model = new SiteSettingsInput
        {
            DisplayName = settings.DisplayName,
            StoreName = settings.StoreName,
            VisualBrandName = settings.VisualBrandName,
            SupportPhoneDisplay = settings.SupportPhoneDisplay,
            SupportPhoneUri = settings.SupportPhoneUri,
            WhatsAppNumber = settings.WhatsAppNumber,
            PublicEmail = settings.PublicEmail,
            MediaCdnBaseUrl = settings.MediaCdnBaseUrl,
            FooterDescription = settings.FooterDescription,
            InstagramUrl = settings.InstagramUrl,
            TelegramUrl = settings.TelegramUrl,
            LinkedInUrl = settings.LinkedInUrl,
            IdWarehouse = settings.IdWarehouse,
            SmtpHost = settings.SmtpHost ?? "",
            SmtpPort = settings.SmtpPort,
            SmtpEnableSsl = settings.SmtpEnableSsl,
            SmtpUsername = settings.SmtpUsername ?? "",
            SmtpFrom = settings.SmtpFrom ?? "",
            SmtpPassword = settings.SmtpPassword,
            EmailVerificationCodeLifetimeMinutes = settings.EmailVerificationCodeLifetimeMinutes,
            FtpHost = settings.FtpHost,
            FtpPort = settings.FtpPort,
            FtpUsername = settings.FtpUsername,
            FtpPassword = settings.FtpPassword,
            LegacyFtpPassword = settings.LegacyFtpPassword,
            FtpPasswordConfigured = settings.FtpPassword is not null || settings.LegacyFtpPassword is not null
        };
        bool ftpPasswordUnreadable = false;
        if (string.IsNullOrEmpty(model.FtpPassword) && !string.IsNullOrEmpty(model.LegacyFtpPassword))
        {
            try
            {
                model.FtpPassword = protection.CreateProtector("Shop.Admin.FtpPassword.v1")
                    .Unprotect(model.LegacyFtpPassword);
            }
            catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException
                                                  or FormatException)
            {
                ftpPasswordUnreadable = true;
                ViewData["FtpPasswordReadError"] = "رمز ذخیره‌شدهٔ FTP قابل بازیابی نیست؛ آن را دوباره وارد کنید.";
            }
        }

        model.LegacyFtpPassword = null;
        ViewData["FtpPasswordStatus"] = !model.FtpPasswordConfigured ? "ثبت نشده"
            : ftpPasswordUnreadable ? "ثبت شده، اما قابل بازیابی نیست"
            : string.IsNullOrEmpty(model.FtpPassword) ? "ثبت شده، اما مقدار رمز خالی است"
            : "ثبت شده و نمایش داده می‌شود";
        ViewData["SmtpPasswordStatus"] =
            string.IsNullOrEmpty(model.SmtpPassword) ? "ثبت نشده" : "ثبت شده و نمایش داده می‌شود";
        await PopulateWarehouses(cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SiteSettingsInput model, CancellationToken cancellationToken)
    {
        ValidateHttpsUrl(model.InstagramUrl, nameof(model.InstagramUrl));
        ValidateHttpsUrl(model.TelegramUrl, nameof(model.TelegramUrl));
        ValidateHttpsUrl(model.LinkedInUrl, nameof(model.LinkedInUrl));

        try
        {
            model.MediaCdnBaseUrl = MediaCdnOptions.NormalizeBaseUrl(model.MediaCdnBaseUrl);
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(nameof(model.MediaCdnBaseUrl), "نشانی CDN باید مبدأ HTTPS بدون مسیر اضافی باشد.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateWarehouses(cancellationToken);

            return View(model);
        }

        var result = await data.SaveAsync(new StoreSiteSettingsData
        {
            DisplayName = model.DisplayName, StoreName = model.StoreName, VisualBrandName = model.VisualBrandName,
            SupportPhoneDisplay = model.SupportPhoneDisplay, SupportPhoneUri = model.SupportPhoneUri,
            WhatsAppNumber = model.WhatsAppNumber, PublicEmail = model.PublicEmail,
            MediaCdnBaseUrl = model.MediaCdnBaseUrl, IdWarehouse = model.IdWarehouse,
            FooterDescription = model.FooterDescription, InstagramUrl = NormalizeOptionalUrl(model.InstagramUrl),
            TelegramUrl = NormalizeOptionalUrl(model.TelegramUrl), LinkedInUrl = NormalizeOptionalUrl(model.LinkedInUrl),
            SmtpHost = model.SmtpHost, SmtpPort = model.SmtpPort, SmtpEnableSsl = model.SmtpEnableSsl,
            SmtpUsername = model.SmtpUsername, SmtpFrom = model.SmtpFrom, SmtpPassword = model.SmtpPassword,
            EmailVerificationCodeLifetimeMinutes = model.EmailVerificationCodeLifetimeMinutes,
            FtpHost = model.FtpHost, FtpPort = model.FtpPort, FtpUsername = model.FtpUsername,
            FtpPassword = model.FtpPassword
        }, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value, cancellationToken);
        if (result == StoreSettingsSaveResult.NotFound) return NotFound();
        if (result == StoreSettingsSaveResult.InvalidWarehouse)
        {
            ModelState.AddModelError(nameof(model.IdWarehouse), "انبار باید انبار فروش فعال یک شرکت فعال باشد.");
            await PopulateWarehouses(cancellationToken);
            return View(model);
        }

        TempData["SiteSettingsSaved"] = true;

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateWarehouses(CancellationToken cancellationToken) =>
        ViewData["Warehouses"] = (await data.ReadSalesWarehousesAsync(cancellationToken))
            .Select(w => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                { Value = w.Id.ToString(), Text = w.Text }).ToList();

    private static string? NormalizeOptionalUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void ValidateHttpsUrl(string? value, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            ModelState.AddModelError(fieldName, "نشانی شبکه باید با HTTPS آغاز شود.");
    }
}

public sealed class SiteSettingsInput
{
    [Required, StringLength(160)] public string DisplayName { get; set; } = "";
    [Required, StringLength(240)] public string StoreName { get; set; } = "";
    [Required, StringLength(160)] public string VisualBrandName { get; set; } = "";
    [Required, StringLength(32)] public string SupportPhoneDisplay { get; set; } = "";

    [Required, StringLength(40),
     RegularExpression(@"\+?[0-9]+", ErrorMessage = "شمارهٔ تماس باید با ارقام انگلیسی و در صورت نیاز + آغاز شود.")]
    public string SupportPhoneUri { get; set; } = "";

    [Required, StringLength(32),
     RegularExpression(@"[0-9]+", ErrorMessage = "شمارهٔ واتس‌اپ باید فقط شامل ارقام انگلیسی باشد.")]
    public string WhatsAppNumber { get; set; } = "";

    [Required, StringLength(254), EmailAddress]
    public string PublicEmail { get; set; } = "";

    [StringLength(1000)] public string FooterDescription { get; set; } = "";
    [StringLength(500), Url] public string? InstagramUrl { get; set; }
    [StringLength(500), Url] public string? TelegramUrl { get; set; }
    [StringLength(500), Url] public string? LinkedInUrl { get; set; }
    [Required, StringLength(500)] public string MediaCdnBaseUrl { get; set; } = MediaCdnOptions.DefaultBaseUrl;
    public long? IdWarehouse { get; set; }
    [Required, StringLength(255)] public string SmtpHost { get; set; } = "";
    [Required, Range(1, 65535)] public int? SmtpPort { get; set; }
    public bool SmtpEnableSsl { get; set; }
    [Required, StringLength(320)] public string SmtpUsername { get; set; } = "";

    [Required, StringLength(320), EmailAddress]
    public string SmtpFrom { get; set; } = "";

    [StringLength(1024)] public string? SmtpPassword { get; set; }
    [Range(1, 1440)] public int EmailVerificationCodeLifetimeMinutes { get; set; } = 15;
    [StringLength(255)] public string? FtpHost { get; set; }
    [Range(1, 65535)] public int? FtpPort { get; set; }
    [StringLength(320)] public string? FtpUsername { get; set; }
    [StringLength(1024)] public string? FtpPassword { get; set; }
    public bool FtpPasswordConfigured { get; set; }
    public string? LegacyFtpPassword { get; set; }
}
