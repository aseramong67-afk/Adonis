namespace ReskinManager.Models;

public sealed class AddonInfo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorAvatar { get; set; } = "";
    public string AddedAtText { get; set; } = "";
    public string Type { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public string Description { get; set; } = "";
    public string WorkshopUrl { get; set; } = "";
    public string PreviewImageUrl { get; set; } = "";
    public bool IsInstalled { get; set; }
    public string InstallPath { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SizeText { get; set; } = "";
}
