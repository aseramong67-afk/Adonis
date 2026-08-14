namespace ReskinManager.Models;

public sealed class CatalogAddon
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorAvatar { get; set; } = "";
    public string Type { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public string Description { get; set; } = "";
    public string WorkshopUrl { get; set; } = "";
    public string Preview { get; set; } = "";
    public string Archive { get; set; } = "";
    public string[] Ignore { get; set; } = [];
    public long SizeBytes { get; set; }
}

public sealed class CatalogJson
{
    public string Version { get; set; } = "1";
    public string UpdatedAt { get; set; } = "";
    public List<CatalogAddon> Addons { get; set; } = new();
}
