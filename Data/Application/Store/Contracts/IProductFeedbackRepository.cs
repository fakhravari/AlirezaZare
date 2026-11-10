using Shop.Data.Application.Store.Models;

namespace Shop.Data.Application.Store.Contracts;

public interface IProductFeedbackRepository
{
    Task<ProductFeedbackSnapshot?> ReadAsync(int productId, string type, long? profileId,
        CancellationToken cancellationToken);

    Task<long?> AddAsync(int productId, long? profileId, string type, string name, string text, byte? rating,
        CancellationToken cancellationToken);

    Task<int?> ImportLegacyAsync(int productId, long? profileId, string type,
        IReadOnlyList<ProductFeedbackLegacyItem> items,
        CancellationToken cancellationToken);
}