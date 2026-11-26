using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Domain.Account.Entities;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreAccountMessageAdminData(ShopDbContext db) : IStoreAccountMessageAdminData
{
    public async Task<IReadOnlyList<AdminCustomerMatchData>> FindCustomersAsync(string search,
        CancellationToken cancellationToken) => await db.AccountProfiles.AsNoTracking()
        .Where(profile => profile.Email.Contains(search) || profile.FirstName.Contains(search) ||
                          profile.LastName.Contains(search))
        .OrderBy(profile => profile.FirstName).ThenBy(profile => profile.LastName).Take(20)
        .Select(profile => new AdminCustomerMatchData(profile.Id, profile.Email, profile.FirstName, profile.LastName))
        .ToListAsync(cancellationToken);

    public async Task<(int Total, IReadOnlyList<AdminAccountMessageData> Items)> ReadMessagesAsync(string search,
        DateTime? fromUtc, DateTime? untilUtc, int offset, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.AccountMessages.AsNoTracking().Join(db.AccountProfiles.AsNoTracking(), message => message.IdProfile,
            profile => profile.Id, (message, profile) => new { message, profile });
        if (fromUtc.HasValue) query = query.Where(row => row.message.CreatedAtUtc >= fromUtc.Value);
        if (untilUtc.HasValue) query = query.Where(row => row.message.CreatedAtUtc < untilUtc.Value);
        if (search.Length > 0)
            query = query.Where(row => row.profile.Email.Contains(search) || row.profile.FirstName.Contains(search) ||
                                       row.profile.LastName.Contains(search));
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(row => row.message.CreatedAtUtc)
            .ThenByDescending(row => row.message.Id).Skip(offset).Take(pageSize)
            .Select(row => new AdminAccountMessageData(row.message.Id, row.message.Title, row.message.Category,
                row.message.Body, row.message.IsRead, row.message.CreatedAtUtc, row.profile.Id, row.profile.Email,
                row.profile.FirstName, row.profile.LastName)).ToListAsync(cancellationToken);
        return (total, items);
    }

    public async Task<bool> SendMessageAsync(decimal profileId, string title, string category, string body,
        CancellationToken cancellationToken)
    {
        if (!await db.AccountProfiles.AnyAsync(profile => profile.Id == profileId, cancellationToken)) return false;
        db.AccountMessages.Add(new AccountMessage
        {
            IdProfile = profileId, Title = title, Category = category, Body = body, IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
