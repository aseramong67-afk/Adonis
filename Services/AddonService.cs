using System.Text.Json;
using ReskinManager.Models;

namespace ReskinManager.Services;

public sealed record OperationResult(bool Ok, string Message = "", string Data = "");

public sealed class AddonService
{
    private readonly string _reskinsRoot;
    private readonly SettingsService _settings;
    private readonly GitHubOptions _gitHub;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private CatalogJson? _catalogCache;
    private DateTime _catalogFetchedAt = DateTime.MinValue;
    private const int CatalogCacheSeconds = 120;

    public AddonService(IWebHostEnvironment env, SettingsService settings, IConfiguration config)
    {
        _reskinsRoot = Path.Combine(env.ContentRootPath, "addons-src");
        _settings = settings;
        _gitHub = config.GetSection("GitHub").Get<GitHubOptions>() ?? new GitHubOptions();
        Directory.CreateDirectory(_reskinsRoot);
    }

    private string RawBase =>
        $"https://raw.githubusercontent.com/{_gitHub.Owner}/{_gitHub.Repo}/{_gitHub.Branch}";

    private string CatalogUrl => $"{RawBase}/reskins/catalog.json";

    public string TargetBase
    {
        get
        {
            var configured = _settings.Current.AddonsTargetPath?.Trim().TrimEnd('\\') ?? "";
            if (!string.IsNullOrWhiteSpace(configured)) return configured;
            return SteamLocator.FindGmodAddonsPath() ?? "";
        }
    }

    public string? TargetExistsError()
    {
        if (string.IsNullOrWhiteSpace(TargetBase)) return "Не задана папка установки аддонов.";
        if (!Directory.Exists(TargetBase)) return "Папка установки не существует.";
        return null;
    }

    public bool IsGitHubConfigured =>
        !string.IsNullOrWhiteSpace(_gitHub.Owner) && !string.IsNullOrWhiteSpace(_gitHub.Repo);

    // ---------- catalog ----------

    public async Task<CatalogJson> GetCatalogAsync()
    {
        if (!IsGitHubConfigured) return new CatalogJson();

        if (_catalogCache is not null &&
            DateTime.UtcNow - _catalogFetchedAt < TimeSpan.FromSeconds(CatalogCacheSeconds))
            return _catalogCache;

        try
        {
            var json = await _http.GetStringAsync(CatalogUrl);
            var catalog = JsonSerializer.Deserialize<CatalogJson>(json, JsonOpts) ?? new CatalogJson();
            _catalogCache = catalog;
            _catalogFetchedAt = DateTime.UtcNow;
            return catalog;
        }
        catch
        {
            return _catalogCache ?? new CatalogJson();
        }
    }

    public async Task<IEnumerable<AddonInfo>> GetAddons()
    {
        var catalog = await GetCatalogAsync();
        var entries = catalog.Addons;
        if (entries.Count == 0 && IsGitHubConfigured)
            return GetLocalAddons();

        return entries.Select(a => new AddonInfo
        {
            Id = a.Id,
            Title = string.IsNullOrWhiteSpace(a.Title) ? a.Id : a.Title,
            Author = a.Author,
            AuthorAvatar = ToUrl(a.AuthorAvatar, a.Id),
            AddedAtText = FormatAddedAt(a.Id),
            Type = a.Type,
            Tags = a.Tags,
            Description = a.Description,
            WorkshopUrl = a.WorkshopUrl,
            PreviewImageUrl = ToUrl(a.Preview, a.Id),
            IsInstalled = IsInstalled(a.Id),
            InstallPath = IsInstalled(a.Id) ? Path.Combine(TargetBase, a.Id) : "",
            SizeBytes = a.SizeBytes,
            SizeText = FormatSize(a.SizeBytes),
        });
    }

    public async Task<CatalogAddon?> FindAddonAsync(string id)
    {
        var catalog = await GetCatalogAsync();
        return catalog.Addons.FirstOrDefault(a =>
            a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private string ToUrl(string asset, string addonId)
    {
        if (string.IsNullOrWhiteSpace(asset)) return "";
        if (asset.StartsWith("http://") || asset.StartsWith("https://")) return asset;
        return $"{RawBase}/reskins/{asset.TrimStart('/')}";
    }

    // ---------- local fallback (offline / не настроен GitHub) ----------

    private IEnumerable<AddonInfo> GetLocalAddons()
    {
        foreach (var dir in Directory.EnumerateDirectories(_reskinsRoot))
        {
            var jsonPath = Path.Combine(dir, "addon.json");
            if (!File.Exists(jsonPath)) continue;

            AddonJson json;
            try
            {
                json = JsonSerializer.Deserialize<AddonJson>(File.ReadAllText(jsonPath), JsonOpts) ?? new AddonJson();
            }
            catch
            {
                continue;
            }

            var id = Path.GetFileName(dir);

            yield return new AddonInfo
            {
                Id = id,
                Title = string.IsNullOrWhiteSpace(json.Title) ? id : json.Title,
                Author = json.Author,
                AuthorAvatar = json.AuthorAvatar,
                AddedAtText = FormatAddedAt(id),
                Type = json.Type,
                Tags = json.Tags,
                Description = json.Description,
                WorkshopUrl = json.WorkshopUrl,
                PreviewImageUrl = FindPreview(dir),
                IsInstalled = IsInstalled(id),
                InstallPath = IsInstalled(id) ? Path.Combine(TargetBase, id) : "",
                SizeBytes = DirSize(dir),
                SizeText = FormatSize(DirSize(dir)),
            };
        }
    }

    // ---------- install / uninstall ----------

    public async Task<OperationResult> InstallAsync(string rawId)
    {
        var id = Path.GetFileName(rawId);
        if (string.IsNullOrEmpty(id) || id != rawId)
            return new OperationResult(false, "Некорректный идентификатор аддона.");

        var targetError = TargetExistsError();
        if (targetError != null) return new OperationResult(false, targetError);

        var dst = Path.Combine(TargetBase, id);
        if (Directory.Exists(dst)) return new OperationResult(false, "Аддон уже установлен.");

        var tmpDir = Path.Combine(Path.GetTempPath(), "adonis_dl_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tmpDir);
            var addon = await FindAddonAsync(id);

            if (addon is not null && IsGitHubConfigured)
            {
                var archive = string.IsNullOrWhiteSpace(addon.Archive) ? $"{id}.zip" : addon.Archive;
                var url = $"{RawBase}/reskins/{archive}";
                var zipPath = Path.Combine(tmpDir, Path.GetFileName(archive));

                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(zipPath, bytes);

                var extractDir = Path.Combine(tmpDir, "x");
                Directory.CreateDirectory(extractDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

                var ignore = new HashSet<string>(addon.Ignore ?? [], StringComparer.OrdinalIgnoreCase);
                CopyDirectory(extractDir, dst, ignore);
            }
            else
            {
                var src = Path.GetFullPath(Path.Combine(_reskinsRoot, id));
                if (!src.StartsWith(Path.GetFullPath(_reskinsRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return new OperationResult(false, "Некорректный идентификатор аддона.");
                if (!Directory.Exists(src) || !File.Exists(Path.Combine(src, "addon.json")))
                    return new OperationResult(false, "Аддон не найден в каталоге.");

                HashSet<string> ignore = ReadIgnore(src);
                CopyDirectory(src, dst, ignore);
            }

            return new OperationResult(true, "Аддон установлен.", dst);
        }
        catch (Exception ex)
        {
            try { if (Directory.Exists(dst)) Directory.Delete(dst, true); } catch { }
            return new OperationResult(false, "Ошибка установки: " + ex.Message);
        }
        finally
        {
            try { if (Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }
        }
    }

    public OperationResult UninstallAsync(string rawId)
    {
        if (string.IsNullOrWhiteSpace(TargetBase)) return new OperationResult(false, "Не задана папка установки аддонов.");

        var id = Path.GetFileName(rawId);
        if (string.IsNullOrEmpty(id) || id != rawId)
            return new OperationResult(false, "Некорректный идентификатор аддона.");

        var dir = Path.Combine(TargetBase, id);
        if (!Directory.Exists(dir)) return new OperationResult(false, "Аддон не установлен.");

        try
        {
            Directory.Delete(dir, true);
            return new OperationResult(true, "Аддон удалён.", dir);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка удаления: " + ex.Message);
        }
    }

    private bool IsInstalled(string id) =>
        !string.IsNullOrWhiteSpace(TargetBase) && Directory.Exists(Path.Combine(TargetBase, id));

    private static HashSet<string> ReadIgnore(string dir)
    {
        var jsonPath = Path.Combine(dir, "addon.json");
        try
        {
            var json = JsonSerializer.Deserialize<AddonJson>(File.ReadAllText(jsonPath), JsonOpts);
            return new HashSet<string>(json?.Ignore ?? [], StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return [];
        }
    }

    private static void CopyDirectory(string source, string dest, HashSet<string> ignore)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            var name = Path.GetFileName(file);
            if (ignore.Contains(name)) continue;
            File.Copy(file, Path.Combine(dest, name));
        }
        foreach (var sub in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(sub);
            if (ignore.Contains(name)) continue;
            CopyDirectory(sub, Path.Combine(dest, name), ignore);
        }
    }

    private string FindPreview(string dir)
    {
        foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" })
        {
            var file = Path.Combine(dir, "preview" + ext);
            if (File.Exists(file)) return "/reskins/" + Path.GetFileName(dir) + "/" + Path.GetFileName(file);
        }

        var first = Directory
            .EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
            .FirstOrDefault(f => IsImage(f));

        return first is null ? "" : "/reskins/" + Path.GetRelativePath(_reskinsRoot, first).Replace('\\', '/');
    }

    private static bool IsImage(string file)
    {
        var ext = Path.GetExtension(file);
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".bmp";
    }

    private static long DirSize(string dir) =>
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);

    private static string FormatSize(long bytes)
    {
        string[] units = ["Б", "КБ", "МБ", "ГБ"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.#} {units[unit]}";
    }

    private string FormatAddedAt(string id)
    {
        try
        {
            var dir = Path.Combine(_reskinsRoot, id);
            if (Directory.Exists(dir)) return Directory.GetCreationTime(dir).ToString("dd.MM.yyyy");
        }
        catch { }
        return DateTime.Now.ToString("dd.MM.yyyy");
    }
}
