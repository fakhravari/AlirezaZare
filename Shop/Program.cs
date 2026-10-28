using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddRateLimiter(options => options.AddPolicy("email-code", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0,
            AutoReplenishment = true
        })));
builder.Services.Configure<Shop.Models.EmailSmtpOptions>(builder.Configuration.GetSection("Email:Smtp"));
builder.Services.AddSingleton<Shop.Services.EmailVerificationCodeStore>();
builder.Services.AddSingleton<Shop.Services.IEmailSender, Shop.Services.SmtpEmailSender>();
builder.Services.AddSingleton<Shop.Services.CatalogRepository>();
builder.Services.AddSingleton<Shop.Services.ICatalogSource>(services => services.GetRequiredService<Shop.Services.CatalogRepository>());
builder.Services.AddScoped<Shop.Services.DemoAccountDataSource>();
builder.Services.AddScoped<Shop.Services.IAccountDataSource>(services => services.GetRequiredService<Shop.Services.DemoAccountDataSource>());
builder.Services.AddScoped<Shop.Services.ICommerceOfferSource>(services => services.GetRequiredService<Shop.Services.DemoAccountDataSource>());
var persianHtmlEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Arabic);
persianHtmlEncoderSettings.AllowCharacter('\u200C');
builder.Services.AddWebEncoders(options => options.TextEncoderSettings = persianHtmlEncoderSettings);
builder.Services.Configure<Shop.Models.SeoMetadataOptions>(builder.Configuration.GetSection("Seo"));
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options => options.LowercaseUrls = true);
var app = builder.Build();
if (Uri.TryCreate(builder.Configuration["Seo:SiteUrl"], UriKind.Absolute, out var canonicalSite)
    && (canonicalSite.Scheme == Uri.UriSchemeHttp || canonicalSite.Scheme == Uri.UriSchemeHttps)
    && string.IsNullOrEmpty(canonicalSite.UserInfo)
    && canonicalSite.AbsolutePath == "/"
    && string.IsNullOrEmpty(canonicalSite.Query)
    && string.IsNullOrEmpty(canonicalSite.Fragment))
{
    app.Use(async (context, next) =>
    {
        string requestHost = context.Request.Host.Host;
        bool localDevelopmentHost = app.Environment.IsDevelopment()
            && (string.Equals(requestHost, "localhost", StringComparison.OrdinalIgnoreCase)
                || System.Net.IPAddress.TryParse(requestHost, out var address) && System.Net.IPAddress.IsLoopback(address));
        if (!localDevelopmentHost && !string.Equals(requestHost, canonicalSite.Host, StringComparison.OrdinalIgnoreCase))
        {
            var target = new UriBuilder(canonicalSite)
            {
                Path = context.Request.PathBase.Add(context.Request.Path).Value ?? "/",
                Query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value![1..] : string.Empty
            };
            context.Response.Redirect(target.Uri.ToString(), permanent: true, preserveMethod: true);
            return;
        }

        await next();
    });
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseRateLimiter();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
