using FluentFTP;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Services;

public sealed class FtpMediaStorage(ShopDbContext db, IDataProtectionProvider protection)
{
    public async Task<AsyncFtpClient> ConnectAsync(CancellationToken cancellationToken)
    {
        var settings = await ReadSettings(cancellationToken);
        if (settings is null || string.IsNullOrWhiteSpace(settings.Host) || settings.Port is null or < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(settings.Username) || (string.IsNullOrEmpty(settings.Password) && string.IsNullOrEmpty(settings.LegacyPassword)))
            throw new InvalidOperationException("تنظیمات FTP در صفحهٔ تنظیمات سایت کامل نیست.");

        string? password = settings.Password;
        if (string.IsNullOrEmpty(password))
        {
            try { password = protection.CreateProtector("Shop.Admin.FtpPassword.v1").Unprotect(settings.LegacyPassword!); }
            catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or FormatException)
            { throw new InvalidOperationException("رمز پیشین FTP قابل بازیابی نیست؛ آن را دوباره ثبت کنید.", exception); }
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [Store].[SiteSetting] SET [FtpPassword]={password}, [FtpPasswordProtected]=NULL
                WHERE [Id]=1 AND [FtpPassword] IS NULL AND [FtpPasswordProtected]={settings.LegacyPassword}
                """, cancellationToken);
            settings = await ReadSettings(cancellationToken);
            if (settings is null || string.IsNullOrWhiteSpace(settings.Host) || settings.Port is null or < 1 or > 65535 ||
                string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrEmpty(settings.Password))
                throw new InvalidOperationException("تنظیمات FTP تغییر کرده است؛ دوباره تلاش کنید.");
            password = settings.Password;
        }

        var client = new AsyncFtpClient(settings.Host, settings.Username, password, settings.Port.Value);
        client.Config.EncryptionMode = FtpEncryptionMode.None;
        try { await client.Connect(cancellationToken); return client; }
        catch { client.Dispose(); throw; }
    }

    private Task<FtpSettingsRow?> ReadSettings(CancellationToken cancellationToken) =>
        db.Database.SqlQueryRaw<FtpSettingsRow>("""
            SELECT [FtpHost] AS [Host], [FtpPort] AS [Port], [FtpUsername] AS [Username],
                   [FtpPassword] AS [Password], [FtpPasswordProtected] AS [LegacyPassword]
            FROM [Store].[SiteSetting] WHERE [Id]=1
            """).SingleOrDefaultAsync(cancellationToken);

    public sealed class FtpSettingsRow
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? LegacyPassword { get; set; }
    }
}
