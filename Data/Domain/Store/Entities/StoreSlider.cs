namespace Shop.Data.Domain.Store.Entities;

public sealed class StoreSlider
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public bool ShowOnMobile { get; set; }
    public bool ShowOnDesktop { get; set; }
}
