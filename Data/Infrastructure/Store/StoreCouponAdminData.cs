using System.Data;
using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreCouponAdminData(ShopDbContext db) : IStoreCouponAdminData
{
    public async Task<CouponIndexData> ReadAsync(string search, string status, int page, DateOnly today,
        CancellationToken ct)
    {
        var query = db.AccountCoupons.AsNoTracking().Where(item => search == "" || item.Code.Contains(search) ||
            db.AccountProfiles.Any(profile => profile.Id == item.IdProfile && profile.Email.Contains(search)));
        query = status switch
        {
            "disabled" => query.Where(item => !item.Enabled),
            "expired" => query.Where(item => item.Enabled && item.ExpiresOn < today),
            "exhausted" => query.Where(item => item.Enabled && item.ExpiresOn >= today &&
                db.StoreCouponUsages.Count(usage => usage.IdCoupon == item.Id) >= item.UsageLimit),
            "active" => query.Where(item => item.Enabled && item.ExpiresOn >= today &&
                db.StoreCouponUsages.Count(usage => usage.IdCoupon == item.Id) < item.UsageLimit),
            _ => query
        };
        int total = await query.CountAsync(ct);
        page = Math.Min(page, Math.Max(1, (int)Math.Ceiling(total / 25d)));
        int uses = await db.StoreCouponUsages.CountAsync(usage => query.Any(item => item.Id == usage.IdCoupon), ct);
        decimal discount = await db.StoreCustomerOrders
            .Where(order => order.IdCoupon != null && query.Any(item => item.Id == order.IdCoupon))
            .SumAsync(order => (decimal?)order.CouponDiscountAmount, ct) ?? 0;
        var coupons = await query.OrderByDescending(item => item.Id).Skip((page - 1) * 25).Take(25)
            .Select(item => new CouponRowData(item.Id, item.Code, item.DiscountPercent, item.UsageLimit, item.Enabled,
                item.ExpiresOn, db.StoreCouponUsages.Count(usage => usage.IdCoupon == item.Id),
                db.StoreCustomerOrders.Where(order => order.IdCoupon == item.Id)
                    .Sum(order => (decimal?)order.CouponDiscountAmount) ?? 0,
                db.StoreCustomerOrders.Any(order => order.IdCoupon == item.Id), item.IdProfile,
                db.AccountProfiles.Where(profile => profile.Id == item.IdProfile).Select(profile => profile.Email)
                    .FirstOrDefault())).ToListAsync(ct);
        return new CouponIndexData(coupons, search, status, page, total, uses, discount, today);
    }

    public async Task<CouponReportData?> ReadReportAsync(decimal id, int page, CancellationToken ct)
    {
        var coupon = await db.AccountCoupons.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        if (coupon is null) return null;
        var orders = db.StoreCouponUsages.AsNoTracking()
            .Join(db.StoreCustomerOrders.AsNoTracking(), usage => usage.IdCustomerOrder, order => order.Id,
                (usage, order) => new { usage, order })
            .Join(db.AccountProfiles.AsNoTracking(), row => row.order.IdProfile, profile => profile.Id,
                (row, profile) => new { row.usage, row.order, profile })
            .Join(db.OrderStatuses.AsNoTracking(), row => row.order.IdStatus, status => status.Id,
                (row, status) => new { row.usage, row.order, row.profile, status })
            .Where(row => row.usage.IdCoupon == id)
            .Select(row => new CouponOrderRowData(row.order.Id, "",
                row.profile.FirstName + " " + row.profile.LastName, row.status.NameFa, row.order.CreatedAtUtc,
                row.order.SubtotalAmount, row.order.CouponDiscountAmount, row.order.TotalAmount));
        int total = await orders.CountAsync(ct);
        decimal discount = await orders.SumAsync(order => (decimal?)order.Discount, ct) ?? 0;
        decimal subtotal = await orders.SumAsync(order => (decimal?)order.Subtotal, ct) ?? 0;
        decimal payable = await orders.SumAsync(order => (decimal?)order.Total, ct) ?? 0;
        var rows = await orders.OrderByDescending(order => order.CreatedAtUtc).ThenByDescending(order => order.Id)
            .Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        rows = rows.Select(row => row with
        {
            OrderNumber = row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
        }).ToList();
        return new CouponReportData(coupon.Id, coupon.Code, coupon.UsageLimit, total, subtotal, discount, payable,
            page, rows);
    }

    public async Task<CouponMutationResult> DeleteAsync(decimal id, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var coupon = await db.AccountCoupons.SingleOrDefaultAsync(item => item.Id == id, ct);
        if (coupon is null) return CouponMutationResult.NotFound;
        if (await db.StoreCouponUsages.AnyAsync(usage => usage.IdCoupon == id, ct) ||
            await db.StoreCustomerOrders.AnyAsync(order => order.IdCoupon == id, ct))
            return CouponMutationResult.HasHistory;
        db.AccountCoupons.Remove(coupon);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CouponMutationResult.Deleted;
    }

    public async Task<CouponMutationResult> SaveAsync(long? id, string code, int discountPercent, int usageLimit,
        DateOnly expiresOn, bool enabled, string audience, string? customerEmail, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        long? profileId = null;
        if (audience == "personal")
        {
            string email = (customerEmail ?? "").Trim().ToLowerInvariant();
            profileId = email.Length is > 0 and <= 254
                ? await db.AccountProfiles.Where(profile => profile.Email == email)
                    .Select(profile => (long?)profile.Id).SingleOrDefaultAsync(ct)
                : null;
            if (profileId is null) return CouponMutationResult.CustomerNotFound;
        }
        var item = id is null ? null : await db.AccountCoupons.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (id is not null && item is null) return CouponMutationResult.NotFound;
        if (await db.AccountCoupons.AnyAsync(x => x.Code == code && x.Id != id, ct))
            return CouponMutationResult.DuplicateCode;
        int uses = item is null ? 0 : await db.StoreCouponUsages.CountAsync(x => x.IdCoupon == item.Id, ct);
        if (item is not null && item.Code != code && (uses > 0 ||
            await db.StoreCustomerOrders.AnyAsync(x => x.IdCoupon == item.Id, ct)))
            return CouponMutationResult.UsedCode;
        if (usageLimit < uses) return CouponMutationResult.UsageLimitTooLow;
        if (item is null)
        {
            item = new AccountCoupon { CouponKey = Guid.NewGuid().ToString("N") };
            db.AccountCoupons.Add(item);
        }
        item.Code = code; item.IdProfile = profileId; item.DiscountPercent = discountPercent;
        item.UsageLimit = usageLimit; item.ExpiresOn = expiresOn; item.Enabled = enabled;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return CouponMutationResult.Saved;
    }
}
