namespace Shop.Admin.Models;

public sealed record FeaturePageViewModel(
    string AreaTitle,
    string FeatureId,
    string Title,
    string Description,
    IReadOnlyList<string> Capabilities,
    string DataStatus);
