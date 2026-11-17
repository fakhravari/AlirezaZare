namespace Shop.Data.Domain.Store.Entities;

public static class ProductMediaTypes
{
    public const int Image = 1;
    public const int Video = 2;
    public const int Document = 3;
}

public sealed class StoreProductMediaType
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
}

public sealed class StoreProductAsset
{
    public long Id { get; set; }
    public long IdProduct { get; set; }
    public int IdMediaType { get; set; }
    public string Path { get; set; } = "";
    public string ThumbnailPath { get; set; } = "";
    public string CardPath { get; set; } = "";
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
