namespace ReskinManager.Models;

public sealed class GitHubOptions
{
    public string Owner { get; set; } = "";
    public string Repo { get; set; } = "";
    public string Branch { get; set; } = "main";
    public string ReleaseAsset { get; set; } = "Adonis.zip";
}
