namespace Shop.Data.Application.Store.Contracts;

public interface IStoreAccountMessageAdminData
{
    Task<IReadOnlyList<AdminCustomerMatchData>> FindCustomersAsync(string search, CancellationToken cancellationToken);
    Task<(int Total, IReadOnlyList<AdminAccountMessageData> Items)> ReadMessagesAsync(string search, DateTime? fromUtc,
        DateTime? untilUtc, int offset, int pageSize, CancellationToken cancellationToken);
    Task<bool> SendMessageAsync(decimal profileId, string title, string category, string body,
        CancellationToken cancellationToken);
}

public sealed record AdminCustomerMatchData(decimal Id, string Email, string FirstName, string LastName);
public sealed record AdminAccountMessageData(long Id, string Title, string Category, string Body, bool IsRead,
    DateTime CreatedAtUtc, decimal ProfileId, string Email, string FirstName, string LastName);
