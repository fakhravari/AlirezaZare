using System.Text.Json;
using FluentFTP;
using Microsoft.Data.SqlClient;

string connectionString;
using (var config = JsonDocument.Parse(File.ReadAllText("Admin/appsettings.json")))
    connectionString = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()
        ?? throw new InvalidOperationException("Default connection is missing.");

string migration = File.ReadAllText("Shop/Features/Store/Database/Migrations/053-plain-smtp-password-setting.sql");
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
await using (var command = new SqlCommand(migration, connection))
    await command.ExecuteNonQueryAsync();

await using var verify = new SqlCommand("""
    SELECT
        CASE WHEN COL_LENGTH(N'Store.SiteSetting', N'SmtpPassword') IS NOT NULL THEN 1 ELSE 0 END,
        CASE WHEN EXISTS (SELECT 1 FROM [Store].[SchemaMigration] WHERE [MigrationKey] = N'053-plain-smtp-password-setting') THEN 1 ELSE 0 END
    """, connection);
await using (var reader = await verify.ExecuteReaderAsync())
{
    if (!await reader.ReadAsync() || reader.GetInt32(0) != 1 || reader.GetInt32(1) != 1)
        throw new InvalidOperationException("Migration verification failed.");
}

Console.WriteLine("migration=053-plain-smtp-password-setting verified on configured online database");

string RequiredEnvironment(string key) => Environment.GetEnvironmentVariable(key)
    ?? throw new InvalidOperationException($"Missing environment variable: {key}");

string ftpHost = RequiredEnvironment("SHOP_FTP_HOST");
string ftpUsername = RequiredEnvironment("SHOP_FTP_USERNAME");
string ftpPassword = RequiredEnvironment("SHOP_FTP_PASSWORD");
string smtpHost = RequiredEnvironment("SHOP_SMTP_HOST");
string smtpUsername = RequiredEnvironment("SHOP_SMTP_USERNAME");
string smtpFrom = RequiredEnvironment("SHOP_SMTP_FROM");
string smtpPassword = RequiredEnvironment("SHOP_SMTP_PASSWORD");

await using var check = new SqlCommand("""
    SELECT CASE WHEN [FtpHost]=@FtpHost AND [FtpUsername]=@FtpUsername
        AND CONVERT(varbinary(2048), [FtpPassword])=CONVERT(varbinary(2048), @FtpPassword)
        AND [FtpPasswordProtected] IS NULL AND [SmtpHost]=@SmtpHost AND [SmtpUsername]=@SmtpUsername
        AND [SmtpFrom]=@SmtpFrom
        AND CONVERT(varbinary(2048), [SmtpPassword])=CONVERT(varbinary(2048), @SmtpPassword) THEN 1 ELSE 0 END
    FROM [Store].[SiteSetting] WHERE [Id]=1
    """, connection);
void AddValue(SqlCommand command, string name, string value, int size) =>
    command.Parameters.Add(name, System.Data.SqlDbType.NVarChar, size).Value = value;
void AddExpected(SqlCommand command)
{
    AddValue(command, "@FtpHost", ftpHost, 255);
    AddValue(command, "@FtpUsername", ftpUsername, 320);
    AddValue(command, "@FtpPassword", ftpPassword, 1024);
    AddValue(command, "@SmtpHost", smtpHost, 255);
    AddValue(command, "@SmtpUsername", smtpUsername, 320);
    AddValue(command, "@SmtpFrom", smtpFrom, 320);
    AddValue(command, "@SmtpPassword", smtpPassword, 1024);
}
AddExpected(check);
bool alreadyMatches = Convert.ToInt32(await check.ExecuteScalarAsync()) == 1;
if (!alreadyMatches)
{
    await using var update = new SqlCommand("""
        SET NOCOUNT OFF;
        UPDATE [Store].[SiteSetting] SET
            [FtpHost]=@FtpHost, [FtpUsername]=@FtpUsername, [FtpPassword]=@FtpPassword,
            [FtpPasswordProtected]=NULL, [SmtpHost]=@SmtpHost, [SmtpUsername]=@SmtpUsername,
            [SmtpFrom]=@SmtpFrom, [SmtpPassword]=@SmtpPassword,
            [UpdatedAtUtc]=SYSUTCDATETIME(), [IdentityUpdatedAtUtc]=SYSUTCDATETIME()
        WHERE [Id]=1
        """, connection);
    AddExpected(update);
    if (await update.ExecuteNonQueryAsync() != 1)
        throw new InvalidOperationException("The singleton site settings row was not updated.");
}

check.Parameters.Clear();
AddExpected(check);
if (Convert.ToInt32(await check.ExecuteScalarAsync()) != 1)
    throw new InvalidOperationException("The site credential values did not verify.");
Console.WriteLine(alreadyMatches ? "credentials=already matched" : "credentials=updated and verified");

string ftpTestHost;
string ftpTestUsername;
string ftpTestPassword;
int ftpTestPort;
await using (var ftpCommand = new SqlCommand("""
    SELECT [FtpHost], [FtpPort], [FtpUsername], [FtpPassword]
    FROM [Store].[SiteSetting] WHERE [Id]=1
    """, connection))
await using (var ftpReader = await ftpCommand.ExecuteReaderAsync())
{
    if (!await ftpReader.ReadAsync()) throw new InvalidOperationException("FTP settings row is missing.");
    ftpTestHost = ftpReader.GetString(0);
    ftpTestPort = ftpReader.GetInt32(1);
    ftpTestUsername = ftpReader.GetString(2);
    ftpTestPassword = ftpReader.GetString(3);
}

using (var ftp = new AsyncFtpClient(ftpTestHost, ftpTestUsername, ftpTestPassword, ftpTestPort))
{
    ftp.Config.EncryptionMode = FtpEncryptionMode.None;
    await ftp.Connect();
    await ftp.GetWorkingDirectory();
    await ftp.Disconnect();
}
Console.WriteLine("ftp-authentication=success");
