using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<Shop.Services.CatalogRepository>();
var persianHtmlEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Arabic);
persianHtmlEncoderSettings.AllowCharacter('\u200C');
builder.Services.AddWebEncoders(options => options.TextEncoderSettings = persianHtmlEncoderSettings);
builder.Services.Configure<Shop.Models.SeoMetadataOptions>(builder.Configuration.GetSection("Seo"));
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options => options.LowercaseUrls = true);
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
