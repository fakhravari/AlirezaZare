namespace Shop.Data.Application.Admin.Contracts;

public interface IAdminIdentityData
{
    Task<AdminIdentityState?> AuthenticateAsync(string normalizedUserName, string password,
        CancellationToken cancellationToken);
    Task<AdminIdentityState?> ReadActiveAsync(long userId, CancellationToken cancellationToken);
    Task<AdminBootstrapResult> BootstrapAsync(string userName, string password, string? displayName,
        CancellationToken cancellationToken);
}

public sealed record AdminPermissionData(long Id, string PermissionKey, string? ParentPermissionKey,
    string? Area, string? Controller, string? Action, string? FeatureKey, string Name, int SortOrder, bool IsActive);
public sealed record AdminRolePermissionData(AdminPermissionData Permission);
public sealed record AdminRoleData(string RoleKey, bool IsActive, IReadOnlyList<AdminRolePermissionData> Permissions);
public sealed record AdminIdentityState(long Id, string UserName, string DisplayName, Guid SecurityStamp,
    IReadOnlyList<long> WarehouseIds, IReadOnlyList<AdminRoleData> Roles,
    IReadOnlyList<AdminPermissionData> AllPermissions);
public enum AdminBootstrapResult { Created, AlreadyInitialized, SchemaMissing }
