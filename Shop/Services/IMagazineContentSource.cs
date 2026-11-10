using Shop.Models;

namespace Shop.Services;

public interface IMagazineContentSource
{
    Task<MagazineIndexViewModel> ReadIndexAsync(CancellationToken cancellationToken = default);
    Task<MagazineDetailsModel?> ReadArticleAsync(string slug, CancellationToken cancellationToken = default);
}

public sealed class DatabaseMagazineContentSource(
    Shop.Data.Application.Store.Contracts.IStoreRuntimeDatasetRepository datasets) : IMagazineContentSource
{
    public async Task<MagazineIndexViewModel> ReadIndexAsync(CancellationToken cancellationToken = default)
    {
        MagazineDataset dataset = await ReadDatasetAsync(cancellationToken);
        return new(dataset.Articles.Select(post => new MagazineArchivePost(
            post.Article.Slug, post.Branch, post.Topic, post.Area, post.Article.Category,
            post.Article.Title, post.Article.Summary, post.Article.ContentImage,
            post.Article.PublishedAt, post.ReadMinutes)).ToArray());
    }

    public async Task<MagazineDetailsModel?> ReadArticleAsync(string slug,
        CancellationToken cancellationToken = default) =>
        (await ReadDatasetAsync(cancellationToken)).Articles
        .Select(item => item.Article)
        .FirstOrDefault(article => string.Equals(article.Slug, slug, StringComparison.OrdinalIgnoreCase));

    private async Task<MagazineDataset> ReadDatasetAsync(CancellationToken cancellationToken)
    {
        string json = await datasets.ReadMagazineJsonAsync(cancellationToken)
                      ?? throw new InvalidDataException("The magazine dataset is missing from the database.");
        return System.Text.Json.JsonSerializer.Deserialize<MagazineDataset>(json,
                   new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
               ?? throw new InvalidDataException("The magazine dataset is invalid.");
    }
}

public sealed record MagazineDataset(IReadOnlyList<MagazineDatasetArticle> Articles);

public sealed record MagazineDatasetArticle(
    string Branch,
    string Topic,
    string Area,
    int ReadMinutes,
    MagazineDetailsModel Article);