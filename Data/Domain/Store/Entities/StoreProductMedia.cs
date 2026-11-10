namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreProductMedia
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public string MediaKey { get; set; } = string.Empty;
    public string? MainPath { get; set; }
    public string? CardPath { get; set; }
    public string? ThumbnailPath { get; set; }
    public string? ImagesJson { get; set; }
    public string? VideosJson { get; set; }
}
