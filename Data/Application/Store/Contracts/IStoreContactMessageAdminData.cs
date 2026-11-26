namespace Shop.Data.Application.Store.Contracts;

public interface IStoreContactMessageAdminData
{
    Task<IReadOnlyList<ContactMessageStatusData>> ReadStatusesAsync(CancellationToken cancellationToken);
    Task<bool> StatusExistsAsync(string code, CancellationToken cancellationToken);
    Task<(int Total, IReadOnlyList<ContactMessageAdminData> Items)> ReadMessagesAsync(string? status,
        string search, int offset, int pageSize, CancellationToken cancellationToken);
    Task<bool> SetStatusAsync(long id, string code, CancellationToken cancellationToken);
    Task<ContactMessageAdminData?> SaveReplyAsync(long id, string reply, CancellationToken cancellationToken);
    Task MarkReplyEmailSentAsync(decimal id, CancellationToken cancellationToken);
}

public sealed record ContactMessageStatusData(string Code, string NameFa, string NameEn);
public sealed record ContactMessageAdminData(decimal Id, string Name, string Phone, string? Email, string SubjectKey,
    string Message, string Status, string StatusName, string? AdminReply, DateTime? RepliedAtUtc,
    DateTime? ReplyEmailSentAtUtc, DateTime CreatedAtUtc);
