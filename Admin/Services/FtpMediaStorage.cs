using FluentFTP;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
            await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var identity = await db.StoreSiteIdentities.SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
                if (identity is not null && identity.FtpPassword is null && identity.FtpPasswordProtected == settings.LegacyPassword)
                {
                    identity.FtpPassword = password;
                    identity.FtpPasswordProtected = null;
                    await db.SaveChangesAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
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
        db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1)
            .Select(x => new FtpSettingsRow
            {
                Host = x.FtpHost,
                Port = x.FtpPort,
                Username = x.FtpUsername,
                Password = x.FtpPassword,
                LegacyPassword = x.FtpPasswordProtected
            }).SingleOrDefaultAsync(cancellationToken);

    public sealed class FtpSettingsRow
    {
        public string? Host { get; set; }
        public int? Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? LegacyPassword { get; set; }
    }
}
