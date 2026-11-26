using Microsoft.EntityFrameworkCore;
using Shop.Data.Application.Store.Contracts;
using Shop.Data.Infrastructure.Persistence;

namespace Shop.Data.Infrastructure.Store;

public sealed class StoreContactMessageAdminData(ShopDbContext db) : IStoreContactMessageAdminData
{
    public async Task<IReadOnlyList<ContactMessageStatusData>> ReadStatusesAsync(CancellationToken cancellationToken) =>
        await db.ContactMessageStatuses.AsNoTracking().OrderBy(status => status.Code)
            .Select(status => new ContactMessageStatusData(status.Code, status.NameFa, status.NameEn))
            .ToListAsync(cancellationToken);

    public Task<bool> StatusExistsAsync(string code, CancellationToken cancellationToken) =>
        db.ContactMessageStatuses.AsNoTracking().AnyAsync(status => status.Code == code, cancellationToken);

    public async Task<(int Total, IReadOnlyList<ContactMessageAdminData> Items)> ReadMessagesAsync(string? status,
        string search, int offset, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.StoreContactMessages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(item => item.StatusReference.Code == status);
        if (search.Length > 0)
            query = query.Where(item => item.Name.Contains(search) || item.Phone.Contains(search) ||
                item.Email != null && item.Email.Contains(search) || item.Message.Contains(search));
        int total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Skip(offset).Take(pageSize)
            .Select(item => new ContactMessageAdminData(item.Id, item.Name, item.Phone, item.Email, item.SubjectKey,
                item.Message, item.StatusReference.Code, item.StatusReference.NameFa, item.AdminReply,
                item.RepliedAtUtc, item.ReplyEmailSentAtUtc, item.CreatedAtUtc)).ToListAsync(cancellationToken);
        return (total, items);
    }

    public async Task<bool> SetStatusAsync(long id, string code, CancellationToken cancellationToken)
    {
        var statusId = await db.ContactMessageStatuses.AsNoTracking().Where(status => status.Code == code)
            .Select(status => (int?)status.Id).SingleOrDefaultAsync(cancellationToken);
        if (!statusId.HasValue) return false;
        var item = await db.StoreContactMessages.SingleOrDefaultAsync(message => message.Id == id,
            cancellationToken);
        if (item is null) return false;
        item.IdStatus = statusId.Value;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ContactMessageAdminData?> SaveReplyAsync(long id, string reply,
        CancellationToken cancellationToken)
    {
        var item = await db.StoreContactMessages.SingleOrDefaultAsync(message => message.Id == id,
            cancellationToken);
        if (item is null) return null;
        item.AdminReply = Shop.Data.Application.Store.StoreHtmlSanitizer.Sanitize(reply);
        item.RepliedAtUtc = DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;
        item.IdStatus = await db.ContactMessageStatuses.Where(status => status.Code == "closed")
            .Select(status => status.Id).SingleAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await db.StoreContactMessages.AsNoTracking().Where(message => message.Id == id)
            .Select(message => new ContactMessageAdminData(message.Id, message.Name, message.Phone, message.Email,
                message.SubjectKey, message.Message, message.StatusReference.Code, message.StatusReference.NameFa,
                message.AdminReply, message.RepliedAtUtc, message.ReplyEmailSentAtUtc, message.CreatedAtUtc))
            .SingleAsync(cancellationToken);
    }

    public async Task MarkReplyEmailSentAsync(decimal id, CancellationToken cancellationToken)
    {
        var item = await db.StoreContactMessages.SingleOrDefaultAsync(message => message.Id == id,
            cancellationToken);
        if (item is null) return;
        item.ReplyEmailSentAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
