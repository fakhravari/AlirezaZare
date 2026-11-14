using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Domain.Admin.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Admin.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Route("api/admin/users")]
public sealed class UsersApiController(ShopDbContext db, PasswordHasher<AdminSystemUser> passwordHasher)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        string section = Request.Query["section"].ToString().ToLowerInvariant();
        if (section is not ("users" or "roles" or "permissions"))
            return BadRequest(new { message = "بخش درخواست معتبر نیست." });

        if (section == "roles")
        {
            var roles = await db.AdminRoles.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.RoleKey, x.Name, x.IsActive })
                .ToListAsync(cancellationToken);
            var userCounts = await db.AdminUserRoles.AsNoTracking().GroupBy(userRole => userRole.IdRole)
                .Select(group => new { RoleId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);
            var roleRows = roles.Select(role => new
            {
                role.Id,
                role.RoleKey,
                role.Name,
                role.IsActive,
                userCount = userCounts.GetValueOrDefault(role.Id)
            });
            return Ok(new
            {
                users = Array.Empty<object>(),
                roles = roleRows,
                permissions = Array.Empty<object>(),
                warehouses = Array.Empty<object>()
            });
        }

        if (section == "permissions")
        {
            var permissions = await db.AdminPermissions.AsNoTracking().Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new { x.Id, x.PermissionKey, x.Name, x.ParentKey, x.Area, x.Controller, x.Action })
                .ToListAsync(cancellationToken);
            long[] allPermissionIds = permissions.Select(x => x.Id).ToArray();
            var storedRoles = await db.AdminRoles.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new
                { x.Id, x.RoleKey, x.Name, x.IsActive, permissionIds = x.Permissions.Select(p => p.IdPermission) })
                .ToListAsync(cancellationToken);
            var roles = storedRoles.Select(role => new
            {
                role.Id,
                role.RoleKey,
                role.Name,
                role.IsActive,
                permissionIds = role.RoleKey == "system.full-access" ? allPermissionIds : role.permissionIds
            });
            return Ok(new
            {
                users = Array.Empty<object>(),
                roles,
                permissions,
                warehouses = Array.Empty<object>()
            });
        }

        var userRoles = await db.AdminRoles.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.RoleKey, x.Name, x.IsActive })
            .ToListAsync(cancellationToken);

        var users = await db.AdminSystemUsers.AsNoTracking()
            .OrderBy(x => x.UserName)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.DisplayName,
                x.IsActive,
                x.CreatedAtUtc,
                x.LastLoginAtUtc,
                roleIds = x.Roles.Select(r => r.IdRole),
                warehouseIds = x.WarehouseAccess.Select(w => w.IdWarehouse)
            }).ToListAsync(cancellationToken);

        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Company.Name).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code, x.IdCompany, CompanyName = x.Company.Name })
            .ToListAsync(cancellationToken);

        return Ok(new { users, roles = userRoles, permissions = Array.Empty<object>(), warehouses });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUser([FromBody] SaveSystemUserInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.UserName) || string.IsNullOrWhiteSpace(input.DisplayName))
            return ValidationProblem(detail: "نام کاربری و نام نمایشی الزامی است.", statusCode: 400);
        if (input.UserName.Trim().Length > 100 || input.DisplayName.Trim().Length > 160)
            return ValidationProblem(detail: "طول نام کاربری یا نام نمایشی بیش از حد مجاز است.", statusCode: 400);
        if (input.Id is null && string.IsNullOrEmpty(input.Password))
            return ValidationProblem(detail: "برای کاربر جدید، گذرواژه الزامی است.", statusCode: 400);
        if (input.Password is { Length: > 200 })
            return ValidationProblem(detail: "گذرواژه حداکثر ۲۰۰ نویسه می‌تواند داشته باشد.", statusCode: 400);

        string normalized = input.UserName.Trim().ToUpperInvariant();
        AdminSystemUser? user = input.Id is null
            ? null
            : await db.AdminSystemUsers.Include(x => x.Roles).ThenInclude(x => x.Role).Include(x => x.WarehouseAccess)
                .SingleOrDefaultAsync(x => x.Id == input.Id, cancellationToken);
        if (input.Id is not null && user is null)
            return NotFound();
        if (await db.AdminSystemUsers.AnyAsync(x => x.NormalizedUserName == normalized && x.Id != (input.Id ?? 0),
                cancellationToken))
            return ValidationProblem(detail: "این نام کاربری قبلاً ثبت شده است.", statusCode: 400);

        long[] roleIds = input.RoleIds.Distinct().ToArray();
        long[] warehouseIds = input.WarehouseIds.Distinct().ToArray();
        if (await db.AdminRoles.CountAsync(x => roleIds.Contains(x.Id) && x.IsActive, cancellationToken) !=
            roleIds.Length)
            return ValidationProblem(detail: "یکی از نقش‌های انتخاب‌شده معتبر یا فعال نیست.", statusCode: 400);
        if (await db.Warehouses.CountAsync(x => warehouseIds.Contains(x.Id) && x.IsActive, cancellationToken) !=
            warehouseIds.Length)
            return ValidationProblem(detail: "یکی از انبارهای انتخاب‌شده معتبر یا فعال نیست.", statusCode: 400);

        bool targetHasFullAccess = user?.Roles.Any(x => x.Role.RoleKey == "system.full-access") == true;
        bool willHaveFullAccess =
            await db.AdminRoles.AnyAsync(x => roleIds.Contains(x.Id) && x.RoleKey == "system.full-access",
                cancellationToken);
        bool remainsActive = input.IsActive;
        if (targetHasFullAccess && (!willHaveFullAccess || !remainsActive)
                                && !await db.AdminSystemUsers.AnyAsync(x => x.IsActive && x.Id != user!.Id
                                        && x.Roles.Any(r => r.Role.RoleKey == "system.full-access" && r.Role.IsActive),
                                    cancellationToken))
            return ValidationProblem(detail: "سامانه باید دست‌کم یک کاربر فعال با دسترسی کامل داشته باشد.", statusCode: 400);

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
            db.AdminUserRoles.RemoveRange(user.Roles);
            db.AdminUserWarehouseAccesses.RemoveRange(user.WarehouseAccess);
        }

        foreach (long roleId in roleIds)
            db.AdminUserRoles.Add(new AdminUserRole { User = user, IdRole = roleId });
        foreach (long warehouseId in warehouseIds)
            db.AdminUserWarehouseAccesses.Add(new AdminUserWarehouseAccess { User = user, IdWarehouse = warehouseId });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, message = "اطلاعات کاربر ذخیره شد." });
    }

    [HttpPut("roles/{id:long}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRole(long id, [FromBody] SaveRoleInput input,
        CancellationToken cancellationToken)
    {
        AdminRole? role = await db.AdminRoles.Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role is null)
            return NotFound();
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160)
            return ValidationProblem(detail: "نام نقش الزامی است و حداکثر ۱۶۰ نویسه دارد.", statusCode: 400);
        if (role.RoleKey == "system.full-access" && !input.IsActive)
            return ValidationProblem(detail: "نقش دسترسی کامل قابل غیرفعال‌سازی نیست.", statusCode: 400);

        bool editRoleDetails = string.Equals(Request.Query["section"], "roles", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(Request.Query["section"].ToString());
        long[] permissionIds = [];
        if (!editRoleDetails)
        {
            permissionIds = input.PermissionIds.Distinct().ToArray();
            if (role.RoleKey == "system.full-access")
                permissionIds = await db.AdminPermissions.Where(x => x.IsActive).Select(x => x.Id)
                    .ToArrayAsync(cancellationToken);
            if (await db.AdminPermissions.CountAsync(x => permissionIds.Contains(x.Id) && x.IsActive, cancellationToken) !=
                permissionIds.Length)
                return ValidationProblem(detail: "یکی از مجوزهای انتخاب‌شده معتبر یا فعال نیست.", statusCode: 400);
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        role.Name = input.Name.Trim();
        role.IsActive = input.IsActive;
        if (!editRoleDetails)
        {
            db.AdminRolePermissions.RemoveRange(role.Permissions);
            foreach (long permissionId in permissionIds)
                db.AdminRolePermissions.Add(new AdminRolePermission { IdRole = role.Id, IdPermission = permissionId });
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(new { message = "تغییرهای نقش ذخیره شد و دسترسی کاربران در درخواست بعدی به‌روز می‌شود." });
    }

    [HttpPost("roles")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleInput input, CancellationToken cancellationToken)
    {
        string key = input.RoleKey.Trim().ToLowerInvariant();
        if (key.Length is < 2 or > 100 || key.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')))
            return ValidationProblem(detail: "کلید نقش فقط می‌تواند شامل حروف انگلیسی، عدد، نقطه، خط تیره و زیرخط باشد.", statusCode: 400);
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 160)
            return ValidationProblem(detail: "نام نقش الزامی است و حداکثر ۱۶۰ نویسه دارد.", statusCode: 400);
        if (await db.AdminRoles.AnyAsync(x => x.RoleKey == key, cancellationToken))
            return ValidationProblem(detail: "این کلید نقش قبلاً ثبت شده است.", statusCode: 400);
        db.AdminRoles.Add(new AdminRole { RoleKey = key, Name = input.Name.Trim(), IsActive = true });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "نقش جدید ساخته شد." });
    }

    [HttpPost("{id:long}/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.Password) || input.Password.Length > 200)
            return ValidationProblem(detail: "گذرواژه الزامی است و حداکثر ۲۰۰ نویسه می‌تواند داشته باشد.", statusCode: 400);
        AdminSystemUser? user = await db.AdminSystemUsers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null)
            return NotFound();
        user.PasswordHash = passwordHasher.HashPassword(user, input.Password);
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.SecurityStamp = Guid.NewGuid();
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "گذرواژهٔ کاربر تغییر کرد." });
    }
}

public sealed record SaveSystemUserInput(
    long? Id,
    string UserName,
    string DisplayName,
    string? Password,
    bool IsActive,
    long[] RoleIds,
    long[] WarehouseIds);

public sealed record SaveRoleInput(string Name, bool IsActive, long[] PermissionIds);

public sealed record CreateRoleInput(string RoleKey, string Name);

public sealed record ResetPasswordInput(string Password);
