using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shop.Admin.Security;

namespace Shop.Admin.Controllers;

[Route("auth")]
public sealed class AuthController(IServiceProvider services) : Controller
{
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginInput { ReturnUrl = returnUrl });

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("admin-login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(input);

        AdminAuthentication? authentication = services.GetService<AdminAuthentication>();
        if (authentication is null)
        {
            ModelState.AddModelError(string.Empty, "اتصال پایگاه دادهٔ پنل پیکربندی نشده است.");
            return View(input);
        }

        try
        {
            ClaimsPrincipal? principal =
                await authentication.SignInAsync(input.UserName, input.Password, cancellationToken);
            if (principal is null)
            {
                ModelState.AddModelError(string.Empty, "نام کاربری یا گذرواژه معتبر نیست.");
                return View(input);
            }

            await HttpContext.SignInAsync("AdminCookie", principal,
                new AuthenticationProperties { IsPersistent = input.RememberMe });
            return Url.IsLocalUrl(input.ReturnUrl)
                ? LocalRedirect(input.ReturnUrl!)
                : RedirectToAction("Index", "Home", new { area = "Admin" });
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            ModelState.AddModelError(string.Empty,
                "ساختار کاربران سامانه آماده نیست. migration بخش مدیریت را اجرا کنید.");
            return View(input);
        }
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("denied")]
    [AllowAnonymous]
    public IActionResult Denied() => View();
}

public sealed class LoginInput
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}