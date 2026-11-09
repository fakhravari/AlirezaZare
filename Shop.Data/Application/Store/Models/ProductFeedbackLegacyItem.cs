namespace Shop.Database.Application.Store.Models;

public sealed record ProductFeedbackLegacyItem(string Name, string Text, byte? Rating, DateTimeOffset? CreatedAt);