namespace Shop.Database.Application.Store.Models;

public sealed record ProductFeedbackSnapshot(
    IReadOnlyList<ProductFeedbackItem> Items,
    long TotalCount,
    decimal? AverageRating);

public sealed record ProductFeedbackItem(
    long Id,
    string Name,
    string Text,
    byte? Rating,
    string Status,
    string? Reply,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReplyAt);
