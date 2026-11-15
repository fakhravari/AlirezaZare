using FluentFTP;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Ftp;

public sealed class FtpMediaUploadService(ShopDbContext db, IDataProtectionProvider protection)
{
    public async Task<AsyncFtpClient> ConnectAsync(CancellationToken cancellationToken)
    {
        FtpSettingsRow settings = RequireValidSettings(
            await ReadSettings(cancellationToken), allowLegacyPassword: true,
            "تنظیمات FTP در صفحهٔ تنظیمات سایت کامل نیست.");

        if (string.IsNullOrEmpty(settings.Password))
        {
            string legacyPassword = settings.LegacyPassword!;
            string password;
            try { password = protection.CreateProtector("Shop.Admin.FtpPassword.v1").Unprotect(legacyPassword); }
            catch (Exception exception) when (exception is System.Security.Cryptography.CryptographicException or FormatException)
            { throw new InvalidOperationException("رمز پیشین FTP قابل بازیابی نیست؛ آن را دوباره ثبت کنید.", exception); }

            await db.StoreSiteIdentities
                .Where(identity => identity.Id == 1 && identity.FtpPassword == null && identity.FtpPasswordProtected == legacyPassword)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(identity => identity.FtpPassword, password)
                    .SetProperty(identity => identity.FtpPasswordProtected, (string?)null), cancellationToken);

            settings = RequireValidSettings(await ReadSettings(cancellationToken), allowLegacyPassword: false,
                "تنظیمات FTP تغییر کرده است؛ دوباره تلاش کنید.");
        }

        var client = new AsyncFtpClient(settings.Host!, settings.Username!, settings.Password!, settings.Port!.Value);
        client.Config.EncryptionMode = FtpEncryptionMode.None;
        try { await client.Connect(cancellationToken); return client; }
        catch { client.Dispose(); throw; }
    }

    private static FtpSettingsRow RequireValidSettings(FtpSettingsRow? settings, bool allowLegacyPassword, string errorMessage)
    {
        if (settings is null || string.IsNullOrWhiteSpace(settings.Host) || settings.Port is null or < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(settings.Username) ||
            (string.IsNullOrEmpty(settings.Password) && (!allowLegacyPassword || string.IsNullOrEmpty(settings.LegacyPassword))))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return settings;
    }

    private Task<FtpSettingsRow?> ReadSettings(CancellationToken cancellationToken) =>
        db.StoreSiteIdentities.AsNoTracking().Where(x => x.Id == 1).Select(x => new FtpSettingsRow
            (x.FtpHost, x.FtpPort, x.FtpUsername, x.FtpPassword, x.FtpPasswordProtected))
        .SingleOrDefaultAsync(cancellationToken);

    private sealed record FtpSettingsRow(string? Host, int? Port, string? Username, string? Password, string? LegacyPassword);
}
