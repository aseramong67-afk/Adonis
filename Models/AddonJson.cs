namespace ReskinManager.Models;

public sealed class AddonJson
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string AuthorAvatar { get; set; } = "";
    public string Type { get; set; } = "";
    public string[] Tags { get; set; } = [];
    public string Description { get; set; } = "";
    public string WorkshopUrl { get; set; } = "";
    public string[] Ignore { get; set; } = [];
}
