namespace Shop.Models;

public sealed record MagazineArchivePost(
    string Slug, string Branch, string Topic, string Area, string Category,
    string Title, string Summary, string Image, string? PublishedAt, int ReadMinutes);

public sealed record MagazineIndexViewModel(IReadOnlyList<MagazineArchivePost> Posts)
{
    public int TotalCount => Posts.Count;
    public int BuyingCount => Posts.Count(post => post.Branch == "buying");
    public int EducationCount => Posts.Count(post => post.Branch == "education");
    public int SafetyCount => Posts.Count(post => post.Branch == "safety");
    public int ToolsCount => Posts.Count(post => post.Area == "tools");
    public int ElectricalCount => Posts.Count(post => post.Area == "electrical");
    public int WorkshopCount => Posts.Count(post => post.Area == "workshop");

}

internal static class MagazineArchiveCatalog
{
    internal sealed record Seed(string Slug, string Branch, string Topic, string Area);

    internal static IReadOnlyList<Seed> Items { get; } =
    [
        new("choosing-work-drill", "buying", "tools", "tools"),
        new("multimeter-buying-guide", "buying", "measurement", "electrical"),
        new("soldering-station-basics", "education", "soldering", "tools"),
        new("worklight-selection", "buying", "lighting", "workshop"),
        new("drill-bit-matching", "education", "tools", "tools"),
        new("electrical-tool-safety", "safety", "safety", "electrical"),
        new("battery-tool-care", "safety", "battery", "workshop"),
        new("soldering-tip-care", "safety", "soldering", "tools"),
        new("power-tool-buying-checklist", "buying", "tools", "tools"),
        new("multimeter-measurement-safety", "safety", "measurement", "electrical"),
        new("workshop-lighting-guide", "education", "lighting", "workshop"),
        new("soldering-station-setup", "education", "soldering", "tools"),
        new("choose-cordless-platform", "buying", "battery", "tools"),
        new("extension-cord-selection", "buying", "electrical", "electrical"),
        new("workshop-task-light", "buying", "lighting", "workshop"),
        new("workshop-luminaire-layout", "buying", "lighting", "workshop"),
        new("bit-speed-material", "education", "tools", "tools"),
        new("multimeter-continuity", "education", "measurement", "electrical"),
        new("multimeter-display-resolution", "education", "measurement", "electrical"),
        new("soldering-tip-temperature", "education", "soldering", "tools"),
        new("verify-safe-measurement", "safety", "measurement", "electrical"),
        new("extension-cord-inspection", "safety", "electrical", "electrical"),
        new("battery-storage-care", "safety", "battery", "workshop"),
        new("esd-workbench", "safety", "components", "workshop"),
        new("hammer-drill-vs-impact-driver", "buying", "tools", "tools"),
        new("choose-drill-torque", "buying", "tools", "tools"),
        new("led-worklight-buying-guide", "buying", "lighting", "workshop"),
        new("soldering-station-buying-guide", "buying", "soldering", "tools"),
        new("multimeter-symbols-guide", "education", "measurement", "electrical"),
        new("lumens-lux-basics", "education", "lighting", "workshop"),
        new("solder-joint-basics", "education", "soldering", "tools"),
        new("drill-speed-settings", "education", "tools", "tools"),
        new("inspect-power-tool-cord", "safety", "electrical", "electrical"),
        new("protect-eyes-workshop", "safety", "tools", "tools"),
        new("soldering-workspace-ventilation", "safety", "soldering", "tools"),
        new("clean-store-power-tools", "safety", "tools", "tools"),
    ];
}



