using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Admin.Contracts;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Admin;

public sealed class AdminIdentityData(ShopDbContext db, PasswordHasher<AdminSystemUser> passwordHasher)
    : IAdminIdentityData
{
    public async Task<AdminIdentityState?> AuthenticateAsync(string normalizedUserName, string password,
        CancellationToken cancellationToken)
    {
        var user = await db.AdminSystemUsers.Include(x => x.Warehouses).Include(x => x.Roles)
            .ThenInclude(x => x.Role).ThenInclude(x => x.Permissions).ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.NormalizedUserName == normalizedUserName && x.IsActive, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) ==
            PasswordVerificationResult.Failed) return null;
        user.LastLoginAtUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(user, cancellationToken);
    }

    public async Task<AdminIdentityState?> ReadActiveAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await db.AdminSystemUsers.AsNoTracking().Include(x => x.Warehouses).Include(x => x.Roles)
            .ThenInclude(x => x.Role).ThenInclude(x => x.Permissions).ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        return user is null ? null : await ProjectAsync(user, cancellationToken);
    }

    private async Task<AdminIdentityState> ProjectAsync(AdminSystemUser user, CancellationToken cancellationToken)
    {
        var permissions = (await db.AdminPermissions.AsNoTracking().Where(x => x.IsActive)
            .ToListAsync(cancellationToken)).Select(Project).ToArray();
        var roles = user.Roles.Select(link => new AdminRoleData(link.Role.RoleKey, link.Role.IsActive,
            link.Role.Permissions.Select(p => new AdminRolePermissionData(Project(p.Permission))).ToArray())).ToArray();
        return new AdminIdentityState(user.Id, user.UserName, user.DisplayName, user.SecurityStamp,
            user.Warehouses.Select(x => x.IdWarehouse).ToArray(), roles, permissions);
    }

    private static AdminPermissionData Project(AdminPermission x) => new(x.Id, x.PermissionKey,
        x.ParentPermissionKey, x.Area, x.Controller, x.Action, x.FeatureKey, x.Name, x.SortOrder, x.IsActive);

    public async Task<AdminBootstrapResult> BootstrapAsync(string userName, string password, string? displayName,
        CancellationToken cancellationToken)
    {
        if (await db.AdminSystemUsers.AnyAsync(cancellationToken)) return AdminBootstrapResult.AlreadyInitialized;
        var role = await db.AdminRoles.SingleOrDefaultAsync(x => x.RoleKey == "system.full-access", cancellationToken);
        if (role is null) return AdminBootstrapResult.SchemaMissing;
        string trimmedName = userName.Trim();
        var user = new AdminSystemUser
        {
            UserName = trimmedName,
            NormalizedUserName = trimmedName.ToUpperInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? trimmedName : displayName.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        db.AdminSystemUsers.Add(user);
        db.AdminUserRoles.Add(new AdminUserRole { User = user, Role = role });
        await db.SaveChangesAsync(cancellationToken);
        return AdminBootstrapResult.Created;
    }
}
