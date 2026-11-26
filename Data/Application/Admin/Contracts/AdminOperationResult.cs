namespace Shop.Data.Application.Admin.Contracts;

public enum AdminOperationStatus { Success, Invalid, Missing, Denied, Conflict, Validation }

public sealed record AdminOperationResult(AdminOperationStatus Status, object? Body = null, string? Detail = null)
{
    public static AdminOperationResult Success(object? value = null) => new(AdminOperationStatus.Success, value);
    public static AdminOperationResult Invalid(object? value = null) => new(AdminOperationStatus.Invalid, value);
    public static AdminOperationResult Missing(object? value = null) => new(AdminOperationStatus.Missing, value);
    public static AdminOperationResult Denied() => new(AdminOperationStatus.Denied);
    public static AdminOperationResult Conflict(object? value = null) => new(AdminOperationStatus.Conflict, value);
    public static AdminOperationResult Validation(string detail) => new(AdminOperationStatus.Validation, Detail: detail);
}
