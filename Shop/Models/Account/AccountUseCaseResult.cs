namespace Shop.Models.Account;

public enum AccountUseCaseStatus
{
    Ok,
    BadRequest,
    NotFound,
    Conflict
}

public sealed record AccountUseCaseResult(AccountUseCaseStatus Status, object? Body)
{
    public static AccountUseCaseResult Ok(object? body) => new(AccountUseCaseStatus.Ok, body);
    public static AccountUseCaseResult BadRequest(object? body) => new(AccountUseCaseStatus.BadRequest, body);
    public static AccountUseCaseResult NotFound(object? body) => new(AccountUseCaseStatus.NotFound, body);
    public static AccountUseCaseResult Conflict(object? body) => new(AccountUseCaseStatus.Conflict, body);
}