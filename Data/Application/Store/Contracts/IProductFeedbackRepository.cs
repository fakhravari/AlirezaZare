using Shop.Data.Application.Store.Models;

namespace Shop.Data.Application.Store.Contracts;

public interface IProductFeedbackRepository
{
    Task<ProductFeedbackSnapshot?> ReadAsync(int productId, string type, long? profileId,
        CancellationToken cancellationToken);

    Task<decimal?> AddAsync(int productId, long? profileId, string type, string name, string text, byte? rating,
        CancellationToken cancellationToken);

    Task<int?> ImportLegacyAsync(int productId, long? profileId, string type,
        IReadOnlyList<ProductFeedbackLegacyItem> items,
        CancellationToken cancellationToken);
    Task<bool> StatusExistsAsync(int statusId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductFeedbackStatusData>> ReadStatusesAsync(CancellationToken cancellationToken);
    Task<(int Total, IReadOnlyList<ProductFeedbackAdminItem> Items)> ReadAdminAsync(string type, int? statusId,
        string search, DateTime? fromUtc, DateTime? untilUtc, int offset, int pageSize,
        CancellationToken cancellationToken);
    Task<bool> ModerateAsync(long id, string type, int statusId, string? reply,
        CancellationToken cancellationToken);
}

public sealed record ProductFeedbackStatusData(int Id, string Name, string NameEn);
public sealed record ProductFeedbackAdminItem(decimal Id, decimal IdProduct, string ProductKey, string ProductName,
    string DisplayName, string Text, byte? Rating, int StatusId, string StatusName, string? AdminReply,
    DateTime CreatedAtUtc, DateTime? RepliedAtUtc);
