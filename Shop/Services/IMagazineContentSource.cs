using Shop.Models;

namespace Shop.Services;

public interface IMagazineContentSource
{
    Task<MagazineIndexViewModel> ReadIndexAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoMagazineContentSource : IMagazineContentSource
{
    public Task<MagazineIndexViewModel> ReadIndexAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var posts = MagazineArchiveCatalog.Items.Select((post, index) =>
        {
            MagazineDetailsModel article = JournalArticles.Find(post.Slug)
                ?? throw new InvalidDataException($"Archive article '{post.Slug}' was not found.");
            return new MagazineArchivePost(
                article.Slug, post.Branch, post.Topic, post.Area, article.Category,
                article.Title, article.Summary, article.ContentImage, null, 4 + index % 4);
        }).ToArray();
        return Task.FromResult(new MagazineIndexViewModel(posts));
    }
}
