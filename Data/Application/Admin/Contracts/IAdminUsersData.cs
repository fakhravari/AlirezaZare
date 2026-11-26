namespace Shop.Data.Application.Admin.Contracts;

public interface IAdminUsersData
{
    Task<AdminOperationResult> GetUsers(string section, CancellationToken cancellationToken);
    Task<AdminOperationResult> SaveUser(SaveSystemUserInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> SaveRole(string section, long id, SaveRoleInput input,
        CancellationToken cancellationToken);
    Task<AdminOperationResult> CreateRole(CreateRoleInput input, CancellationToken cancellationToken);
    Task<AdminOperationResult> ResetPassword(long id, ResetPasswordInput input,
        CancellationToken cancellationToken);
}

public sealed record SaveSystemUserInput(
    long? Id,
    string UserName,
    string DisplayName,
    string? Password,
    bool IsActive,
    long[]? RoleIds,
    long[]? WarehouseIds);

public sealed record SaveRoleInput(string? Name, bool IsActive, long[]? PermissionIds);

public sealed record CreateRoleInput(string RoleKey, string Name);

public sealed record ResetPasswordInput(string Password);
