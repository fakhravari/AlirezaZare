namespace Shop.Data.Domain.Admin.Entities;

public sealed class AdminSystemUser
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public ICollection<AdminUserRole> Roles { get; set; } = [];
    public ICollection<AdminUserWarehouseAccess> WarehouseAccess { get; set; } = [];
}

public sealed class AdminRole
{
    public long Id { get; set; }
    public string RoleKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<AdminUserRole> Users { get; set; } = [];
    public ICollection<AdminRolePermission> Permissions { get; set; } = [];
}

public sealed class AdminPermission
{
    public long Id { get; set; }
    public string PermissionKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentKey { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? FeatureKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AdminUserRole
{
    public long IdUser { get; set; }
    public long IdRole { get; set; }
    public AdminSystemUser User { get; set; } = null!;
    public AdminRole Role { get; set; } = null!;
}

public sealed class AdminRolePermission
{
    public long IdRole { get; set; }
    public long IdPermission { get; set; }
    public AdminRole Role { get; set; } = null!;
    public AdminPermission Permission { get; set; } = null!;
}

public sealed class AdminUserWarehouseAccess
{
    public long IdUser { get; set; }
    public long IdWarehouse { get; set; }
    public AdminSystemUser User { get; set; } = null!;
}