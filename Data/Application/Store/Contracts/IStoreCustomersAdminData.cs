namespace Shop.Data.Application.Store.Contracts;

public interface IStoreCustomersAdminData
{
    Task<CustomerAdminPage> ReadAsync(string search, int offset, int pageSize, CancellationToken cancellationToken);
    Task<CustomerAdminDetails?> ReadDetailsAsync(decimal id, CancellationToken cancellationToken);
    Task<CustomerCreateDataResult> CreateAsync(string email, string firstName, string lastName, string? phone,
        CancellationToken cancellationToken);
    Task<CustomerMutationResult> UpdateAsync(long id, CustomerAdminUpdate input, CancellationToken cancellationToken);
    Task<CustomerMutationResult> DeleteAsync(long id, CancellationToken cancellationToken);
}

public sealed record CustomerAdminRow(decimal Id, string FirstName, string LastName, string Email, string? Phone,
    DateTime CreatedAtUtc, int OrderCount);
public sealed record CustomerAdminPage(IReadOnlyList<CustomerAdminRow> Items, int Total);
public sealed record CustomerAdminDetails(CustomerAdminRow Customer, string AddressesJson, string OrdersJson,
    string FeedbackJson, string CartJson, int FavoriteCount, int PriceAlertCount, int ArticleInteractionCount);
public sealed record CustomerAdminUpdate(string Email, string FirstName, string LastName, string? Phone,
    string ExpectedEmail, string ExpectedFirstName, string ExpectedLastName, string? ExpectedPhone);
public sealed record CustomerCreateDataResult(CustomerMutationResult Result, decimal? Id);
public enum CustomerMutationResult { Saved, Deleted, NotFound, EmailExists, Changed, HasOrdersOrCart }
