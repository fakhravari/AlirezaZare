using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Infrastructure.Persistence;
using Shop.Data.Application.Admin.Contracts;
using static Shop.Data.Application.Admin.Contracts.AdminOperationResult;

namespace Shop.Data.Infrastructure.Admin;

public sealed class AdminUsersDataService(ShopDbContext db,
    PasswordHasher<AdminSystemUser> passwordHasher, System.Security.Claims.ClaimsPrincipal User) : IAdminUsersData
{
    public async Task<AdminOperationResult> GetUsers(string section, CancellationToken cancellationToken)
    {
        section = section.ToLowerInvariant();
        if (section is not ("users" or "roles" or "permissions"))
            return Invalid(new { message = "بخش درخواست معتبر نیست." });

        return Success(await ReadSectionAsync(section, cancellationToken));
    }

    public async Task<AdminOperationResult> SaveUser(SaveSystemUserInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.DisplayName))
            return Validation("نام کاربری و نام نمایشی الزامی است.");

        if (input.UserName.Trim().Length > 100 || input.DisplayName.Trim().Length > 160)
            return Validation("طول نام کاربری یا نام نمایشی بیش از حد مجاز است.");

        if (input.Id is null && string.IsNullOrEmpty(input.Password))
            return Validation("برای کاربر جدید، گذرواژه الزامی است.");

        if (input.Password is { Length: > 200 })
            return Validation("گذرواژه حداکثر ۲۰۰ نویسه می‌تواند داشته باشد.");

        string normalized = input.UserName.Trim().ToUpperInvariant();
        AdminSystemUser? user = input.Id is null
            ? null
            : await db.AdminSystemUsers.Include(x => x.Roles).ThenInclude(x => x.Role).Include(x => x.Warehouses)
                .SingleOrDefaultAsync(x => x.Id == input.Id, cancellationToken);
        if (input.Id is not null && user is null)
            return Missing();

        if (await db.AdminSystemUsers.AnyAsync(x => x.NormalizedUserName == normalized && x.Id != (input.Id ?? 0),
                cancellationToken))
            return Validation("این نام کاربری قبلاً ثبت شده است.");

        if (input.RoleIds is null || input.WarehouseIds is null)
            return Invalid(new { message = "فهرست نقش‌ها و انبارها الزامی است." });

        long[] roleIds = input.RoleIds.Distinct().ToArray();
        long[] warehouseIds = input.WarehouseIds.Distinct().ToArray();
        long[] existingRoleIds = user?.Roles.Select(x => x.IdRole).ToArray() ?? [];
        long[] existingWarehouseIds = user?.Warehouses.Select(x => x.IdWarehouse).ToArray() ?? [];
        long[] inactiveRoleIds = (await db.AdminRoles.AsNoTracking().Where(role => existingRoleIds.Contains(role.Id) && !role.IsActive).Select(role => role.Id).ToListAsync(cancellationToken)).ToArray();
        long[] inactiveWarehouseIds = (await db.Warehouses.AsNoTracking().Where(warehouse => existingWarehouseIds.Contains(warehouse.Id) && !warehouse.IsActive).Select(warehouse => warehouse.Id).ToListAsync(cancellationToken)).ToArray();
        roleIds = roleIds.Union(inactiveRoleIds).ToArray();
        warehouseIds = warehouseIds.Union(inactiveWarehouseIds).ToArray();
        if (await db.AdminRoles.CountAsync(x => roleIds.Contains(x.Id) && (x.IsActive || existingRoleIds.Contains(x.Id)), cancellationToken) != roleIds.Length)
            return Validation("یکی از نقش‌های انتخاب‌شده معتبر یا فعال نیست.");

        if (await db.Warehouses.CountAsync(x => warehouseIds.Contains(x.Id) && (x.IsActive || existingWarehouseIds.Contains(x.Id)), cancellationToken) != warehouseIds.Length)
            return Validation("یکی از انبارهای انتخاب‌شده معتبر یا فعال نیست.");

        bool targetHasFullAccess = user?.Roles.Any(x => x.Role.RoleKey == "system.full-access") == true;
        bool willHaveFullAccess = await db.AdminRoles.AnyAsync(x => roleIds.Contains(x.Id) && x.RoleKey == "system.full-access", cancellationToken);
        if (!User.IsInRole("system.full-access") && (targetHasFullAccess || willHaveFullAccess))
            return Denied();
        bool remainsActive = input.IsActive;
        if (targetHasFullAccess && (!willHaveFullAccess || !remainsActive)
                                && !await db.AdminSystemUsers.AnyAsync(x => x.IsActive && x.Id != user!.Id
                                && x.Roles.Any(r => r.Role.RoleKey == "system.full-access" && r.Role.IsActive), cancellationToken))
            return Validation("سامانه باید دست‌کم یک کاربر فعال با دسترسی کامل داشته باشد.");

        user ??= new AdminSystemUser { CreatedAtUtc = DateTime.UtcNow };
        user.UserName = input.UserName.Trim();
        user.NormalizedUserName = normalized;
        user.DisplayName = input.DisplayName.Trim();
        user.IsActive = remainsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (input.Id is null || !string.IsNullOrEmpty(input.Password))
            user.SecurityStamp = Guid.NewGuid();

        if (!string.IsNullOrEmpty(input.Password))
            user.PasswordHash = passwordHasher.HashPassword(user, input.Password);

        if (input.Id is null)
            db.AdminSystemUsers.Add(user);
        else
        {
            db.AdminUserRoles.RemoveRange(user.Roles.Where(x => !roleIds.Contains(x.IdRole)));
            db.AdminSystemUserWarehouses.RemoveRange(user.Warehouses.Where(x =>
                !warehouseIds.Contains(x.IdWarehouse)));
        }

        foreach (long roleId in roleIds.Except(existingRoleIds))
            db.AdminUserRoles.Add(new AdminUserRole { User = user, IdRole = roleId });
        foreach (long warehouseId in warehouseIds.Except(existingWarehouseIds))
            db.AdminSystemUserWarehouses.Add(new AdminSystemUserWarehouse { User = user, IdWarehouse = warehouseId });

        await db.SaveChangesAsync(cancellationToken);

        return Success(new { user.Id, message = "اطلاعات کاربر ذخیره شد." });
    }

    public async Task<AdminOperationResult> SaveRole(string section, long id, SaveRoleInput input, CancellationToken cancellationToken)
    {
        AdminRole? role = await db.AdminRoles.Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
            return Missing();

        if (!string.Equals(section, "roles", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(section, "permissions", StringComparison.OrdinalIgnoreCase))
            return Invalid(new { message = "بخش درخواست معتبر نیست." });

        bool editRoleDetails = string.Equals(section, "roles", StringComparison.OrdinalIgnoreCase);
        if (role.RoleKey == "system.full-access")
            return Validation("نقش دسترسی کامل قابل ویرایش نیست.");

        if (editRoleDetails && (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160))
            return Validation("نام نقش الزامی است و حداکثر ۱۶۰ نویسه دارد.");

        long[] permissionIds = [];
        HashSet<long> visiblePermissionIds = [];
        if (!editRoleDetails)
        {
            if (input.PermissionIds is null)
                return Invalid(new { message = "فهرست مجوزها الزامی است." });

            permissionIds = input.PermissionIds.Distinct().ToArray();
            visiblePermissionIds = GetVisiblePermissions(await db.AdminPermissions.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken)).Select(x => x.Id).ToHashSet();

            if (permissionIds.Any(id => !visiblePermissionIds.Contains(id)))
                return Validation("یکی از مجوزهای انتخاب‌شده معتبر یا فعال نیست.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        if (editRoleDetails)
        {
            role.Name = input.Name!.Trim();
            role.IsActive = input.IsActive;
        }

        if (!editRoleDetails)
        {
            db.AdminRolePermissions.RemoveRange(role.Permissions.Where(x => visiblePermissionIds.Contains(x.IdPermission) && !permissionIds.Contains(x.IdPermission)));
            foreach (long permissionId in permissionIds.Except(role.Permissions.Select(x => x.IdPermission)))
                db.AdminRolePermissions.Add(new AdminRolePermission { IdRole = role.Id, IdPermission = permissionId });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Success(new { message = "تغییرهای نقش ذخیره شد و دسترسی کاربران در درخواست بعدی به‌روز می‌شود." });
    }

    private async Task<object> ReadSectionAsync(string section, CancellationToken cancellationToken)
    {
        if (section == "roles")
        {
            var roles = await db.AdminRoles.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.RoleKey, x.Name, x.IsActive }).ToListAsync(cancellationToken);
            var userCounts = await db.AdminUserRoles.AsNoTracking().GroupBy(userRole => userRole.IdRole)
                .Select(group => new { RoleId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);
            return new
            {
                users = Array.Empty<object>(),
                roles = roles.Select(role => new
                { role.Id, role.RoleKey, role.Name, role.IsActive, userCount = userCounts.GetValueOrDefault(role.Id) }),
                permissions = Array.Empty<object>(),
                warehouses = Array.Empty<object>()
            };
        }

        if (section == "permissions")
        {
            var visible = GetVisiblePermissions(await db.AdminPermissions.AsNoTracking().Where(x => x.IsActive).ToListAsync(cancellationToken));
            var canonicalKeys = visible.ToDictionary(x => x.PermissionKey, x => x.PermissionKey, StringComparer.OrdinalIgnoreCase);
            var permissions = visible.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => new
            {
                x.Id,
                x.PermissionKey,
                x.Name,
                ParentKey = string.IsNullOrWhiteSpace(x.ParentPermissionKey) ? null : canonicalKeys.GetValueOrDefault(x.ParentPermissionKey),
                x.Area,
                x.Controller,
                x.Action
            }).ToList();

            long[] allPermissionIds = permissions.Select(x => x.Id).ToArray();
            var storedRoles = await db.AdminRoles.AsNoTracking().OrderBy(x => x.Name).Select(x => new
            { x.Id, x.RoleKey, x.Name, x.IsActive, permissionIds = x.Permissions.Select(p => p.IdPermission) }).ToListAsync(cancellationToken);
            var roleRows = storedRoles.Select(role => new
            {
                role.Id,
                role.RoleKey,
                role.Name,
                role.IsActive,
                permissionIds = role.RoleKey == "system.full-access" ? allPermissionIds : role.permissionIds
            });
            return new
            {
                users = Array.Empty<object>(),
                roles = roleRows,
                permissions,
                warehouses = Array.Empty<object>()
            };
        }

        var userRoles = await db.AdminRoles.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.RoleKey, x.Name, x.IsActive }).ToListAsync(cancellationToken);
        var users = await db.AdminSystemUsers.AsNoTracking().OrderBy(x => x.UserName).Select(x => new
        {
            x.Id,
            x.UserName,
            x.DisplayName,
            x.IsActive,
            x.CreatedAtUtc,
            x.LastLoginAtUtc,
            roleIds = x.Roles.Select(r => r.IdRole),
            warehouseIds = x.Warehouses.Select(w => w.IdWarehouse)
        }).ToListAsync(cancellationToken);

        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Company.Name)
            .ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code, x.IdCompany, CompanyName = x.Company.Name })
            .ToListAsync(cancellationToken);
        return new { users, roles = userRoles, permissions = Array.Empty<object>(), warehouses };
    }

    private static List<AdminPermission> GetVisiblePermissions(IReadOnlyCollection<AdminPermission> permissions)
    {
        var children = permissions.GroupBy(x => string.IsNullOrWhiteSpace(x.ParentPermissionKey) ? "" : x.ParentPermissionKey, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var visible = new List<AdminPermission>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<string>();
        pending.Enqueue("");
        while (pending.TryDequeue(out string? parent))
        {
            if (!children.TryGetValue(parent, out var entries)) continue;
            foreach (var entry in entries)
                if (visited.Add(entry.PermissionKey))
                {
                    visible.Add(entry);
                    pending.Enqueue(entry.PermissionKey);
                }
        }

        return visible;
    }

    public async Task<AdminOperationResult> CreateRole(CreateRoleInput input, CancellationToken cancellationToken)
    {
        string key = input.RoleKey.Trim().ToLowerInvariant();
        if (key.Length is < 2 or > 100 || key.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')))
            return Validation("کلید نقش فقط می‌تواند شامل حروف انگلیسی، عدد، نقطه، خط تیره و زیرخط باشد.");

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160)
            return Validation("نام نقش الزامی است و حداکثر ۱۶۰ نویسه دارد.");

        if (await db.AdminRoles.AnyAsync(x => x.RoleKey == key, cancellationToken))
            return Validation("این کلید نقش قبلاً ثبت شده است.");

        db.AdminRoles.Add(new AdminRole { RoleKey = key, Name = input.Name.Trim(), IsActive = true });
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "نقش جدید ساخته شد." });
    }

    public async Task<AdminOperationResult> ResetPassword(long id, ResetPasswordInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.Password) || input.Password.Length > 200)
            return Validation("گذرواژه الزامی است و حداکثر ۲۰۰ نویسه می‌تواند داشته باشد.");

        AdminSystemUser? user = await db.AdminSystemUsers.Include(x => x.Roles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return Missing();
        if (!User.IsInRole("system.full-access") && user.Roles.Any(x => x.Role.RoleKey == "system.full-access"))
            return Denied();

        user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid();
        await db.SaveChangesAsync(cancellationToken);

        return Success(new { message = "گذرواژهٔ کاربر تغییر کرد." });
    }
}
