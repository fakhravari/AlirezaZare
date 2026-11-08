using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Shop.Database.Application.Store.Contracts;

namespace Shop.Database.Infrastructure.Persistence.Repositories;

public sealed class StoreRuntimeDatasetRepository(ShopDbContext dbContext) : IStoreRuntimeDatasetRepository
{
    public async Task<string?> ReadJsonAsync(string datasetKey, CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT [PayloadJson] FROM [Store].[RuntimeDataset] WHERE [DatasetKey] = @DatasetKey;";
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@DatasetKey";
            parameter.DbType = DbType.String;
            parameter.Size = 100;
            parameter.Value = datasetKey;
            command.Parameters.Add(parameter);
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null or DBNull ? null : Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    public async Task<IReadOnlyList<StoreFaqRow>> ReadFaqAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            List<StoreFaqRow> rows = [];
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT f.[Question], f.[Answer], f.[LinkText], f.[LinkUrl], f.[SortOrder]
                FROM [Store].[FaqItem] f
                INNER JOIN [Store].[ContentPage] p ON p.[Id] = f.[IdContentPage]
                WHERE p.[PageKey] = N'faq' AND p.[IsPublished] = 1 AND f.[IsPublished] = 1
                ORDER BY f.[SortOrder], f.[Id];
                """;
            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetInt32(4)));
            return rows;
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    public async Task<string?> ReadAccountJsonAsync(CancellationToken cancellationToken) =>
        await ReadScalarJsonAsync("SELECT TOP (1) [PayloadJson] FROM [Account].[ProfileSnapshot] ORDER BY [Id];",
            cancellationToken);

    public async Task<string?> ReadMagazineJsonAsync(CancellationToken cancellationToken) =>
        await ReadScalarJsonAsync("SELECT [PayloadJson] FROM [Store].[RuntimeDataset] WHERE [DatasetKey] = N'magazine';",
            cancellationToken);

    public async Task<string> ReadGeographyJsonAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT
                    JSON_QUERY((SELECT [Id] AS [id], [NameFa] AS [name], [NameEn] AS [nameEn]
                                FROM [Geo].[Country] ORDER BY [Id] FOR JSON PATH)) AS [countries],
                    JSON_QUERY((SELECT [Id] AS [id], [IdCountry] AS [countryId], [NameFa] AS [name],
                                       [NameEn] AS [nameEn], [Latitude] AS [latitude], [Longitude] AS [longitude]
                                FROM [Geo].[Province] ORDER BY [NameFa] FOR JSON PATH)) AS [provinces],
                    JSON_QUERY((SELECT [Id] AS [id], [IdCountry] AS [countryId], [IdProvince] AS [provinceId],
                                       [SourceCountryName] AS [sourceCountryName], [NameFa] AS [name],
                                       [NameEn] AS [nameEn], [Latitude] AS [latitude], [Longitude] AS [longitude],
                                       [IsProvinceCenter] AS [isProvinceCenter]
                                FROM [Geo].[City] ORDER BY [IdProvince], [NameFa] FOR JSON PATH)) AS [cities]
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
                """;
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result as string ?? "{\"countries\":[],\"provinces\":[],\"cities\":[]}";
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }

    private async Task<string?> ReadScalarJsonAsync(string sql, CancellationToken cancellationToken)
    {
        DbConnection connection = dbContext.Database.GetDbConnection();
        bool closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone) await connection.OpenAsync(cancellationToken);
        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            return result is null or DBNull ? null : Convert.ToString(result, System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (closeWhenDone) await connection.CloseAsync();
        }
    }
}
