using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Shop.Data.Helpers;

/// <summary>Shared paging and search bounds for operational lists.</summary>
public static class AdminListQuery
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 100;
    public const int MaximumPage = 100_000;
    public const int MaximumSearchLength = 100;
    public const int ExportBatchSize = 1_000;

    public static bool TryCreate(int page, int? pageSize, string? search, out AdminPageRequest request)
    {
        int size = pageSize ?? DefaultPageSize;
        string normalizedSearch = NormalizeSearch(search);
        long offset = ((long)page - 1) * size;

        if (page is < 1 or > MaximumPage
            || size is < 1 or > MaximumPageSize
            || offset > int.MaxValue
            || normalizedSearch.Length > MaximumSearchLength
            || normalizedSearch.Any(char.IsControl))
        {
            request = default;
            return false;
        }

        request = new AdminPageRequest(page, size, (int)offset, normalizedSearch);
        return true;
    }

    public static string NormalizeSearch(string? search) => search?.Trim() ?? string.Empty;

    public static bool HasNext(int page, int pageSize, long total) => (long)page * pageSize < total;

    public static AdminPageResult<T> CreateResult<T>(IReadOnlyCollection<T> items, int total, AdminPageRequest request) =>
        new(items, total, request.Page, request.PageSize, HasNext(request.Page, request.PageSize, total));

    public static AdminPageResult<T> CreateLookaheadResult<T>(IReadOnlyCollection<T> items, bool hasNext,
        AdminPageRequest request) => new(items, null, request.Page, request.PageSize, hasNext);

}

public readonly record struct AdminPageRequest(int Page, int PageSize, int Offset, string Search);

public sealed record AdminPageResult<T>(IReadOnlyCollection<T> Items, int? Total, int Page, int PageSize, bool HasNext);
