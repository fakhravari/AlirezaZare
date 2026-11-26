namespace Shop.Data.Application.Admin.Contracts;

public interface ISystemProblemsData
{
    Task<SystemProblemsData> ReadAsync(string? check, int page, CancellationToken cancellationToken);
}

public sealed record SystemProblemsData(IReadOnlyList<SystemProblemCheckData> Checks,
    SystemProblemCheckData Selected, IReadOnlyList<SystemProblemRowData> Rows, int Page, int LastPage,
    DateTimeOffset CheckedAtUtc);
public sealed record SystemProblemCheckData(string Key, string Title, string Description, int Count);
public sealed class SystemProblemRowData
{
    public decimal Id { get; set; }
    public string Subject { get; set; } = "";
    public string Context { get; set; } = "";
    public string Plan { get; set; } = "";
    public string Detail { get; set; } = "";
}
