using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ReskinManager.Models;

namespace ReskinManager.Services;

public sealed record OperationResult(bool Ok, string Message = "", string Data = "");

public sealed class AddonService
{
    private readonly string _reskinsRoot;
    private readonly SettingsService _settings;
    private readonly GitHubOptions _gitHub;
    private static readonly HttpClient _http = CreateHttpClient();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { UseProxy = false };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Adonis/1.0");
        return client;
    }

    private CatalogJson? _catalogCache;
    private DateTime _catalogFetchedAt = DateTime.MinValue;
    private const int CatalogCacheSeconds = 30;

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

    // ---------- publishing (admin) ----------

    public bool HasPublishToken => !string.IsNullOrWhiteSpace(_settings.Current.GitHubToken);

    public OperationResult SavePublishToken(string? token)
    {
        var data = _settings.Current;
        data.GitHubToken = (token ?? "").Trim();
        _settings.Save(data);
        return new OperationResult(true, "Токен сохранён.");
    }

    public async Task<OperationResult> PublishAddonAsync(string title, string author, string description,
        string type, string[] tags, string workshopUrl, byte[] zipBytes, string zipName,
        byte[]? previewBytes, string? previewName)
    {
        if (!IsGitHubConfigured)
            return new OperationResult(false, "GitHub не настроен.");
        if (string.IsNullOrWhiteSpace(_settings.Current.GitHubToken))
            return new OperationResult(false, "Не задан токен GitHub. Укажите его в настройках.");
        if (zipBytes is null || zipBytes.Length == 0)
            return new OperationResult(false, "Не выбран архив аддона.");
        if (string.IsNullOrWhiteSpace(title))
            return new OperationResult(false, "Укажите название аддона.");

        var token = _settings.Current.GitHubToken.Trim();
        var client = new HttpClient(new HttpClientHandler { UseProxy = false })
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Adonis/1.0");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        var id = MakeSlug(title);
        if (string.IsNullOrWhiteSpace(id)) id = "addon-" + DateTime.UtcNow.ToString("yyMMddHHmmss");
        var archiveName = string.IsNullOrWhiteSpace(zipName) ? $"{id}.zip" : zipName;
        if (!archiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            archiveName += ".zip";

        try
        {
            // 1) архив
            var put = await PutFileAsync(client, $"reskins/zips/{archiveName}", zipBytes, null,
                $"Добавлен аддон: {title}");
            if (!put) return new OperationResult(false, "Не удалось загрузить архив в GitHub. Проверьте токен.");

            // 2) превью
            var preview = "";
            if (previewBytes is not null && previewBytes.Length > 0)
            {
                var ext = Path.GetExtension(previewName ?? "preview.png");
                if (string.IsNullOrWhiteSpace(ext)) ext = ".png";
                var previewPath = $"reskins/previews/{id}{ext}";
                var ok = await PutFileAsync(client, previewPath, previewBytes, null, $"Превью аддона: {title}");
                if (ok) preview = $"previews/{id}{ext}";
            }

            // 3) каталог
            var (catalogJson, sha) = await GetCatalogForUpdateAsync(client);
            if (catalogJson is null) return new OperationResult(false, "Не удалось получить каталог из GitHub.");

            var catalog = JsonSerializer.Deserialize<CatalogJson>(catalogJson, JsonOpts) ?? new CatalogJson();
            catalog.Addons.RemoveAll(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            catalog.Addons.Insert(0, new CatalogAddon
            {
                Id = id,
                Title = title,
                Author = author,
                Type = string.IsNullOrWhiteSpace(type) ? "reskin" : type,
                Tags = tags,
                Description = description,
                WorkshopUrl = workshopUrl,
                Preview = preview,
                Archive = $"zips/{archiveName}",
                SizeBytes = zipBytes.Length
            });
            catalog.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var newJson = JsonSerializer.Serialize(catalog,
                new JsonSerializerOptions { WriteIndented = true });
            var okCatalog = await PutFileAsync(client, "reskins/catalog.json",
                Encoding.UTF8.GetBytes(newJson), sha, $"Каталог аддонов: добавлен {title}");

            if (!okCatalog)
                return new OperationResult(false, "Архив загружен, но каталог не обновлён. Проверьте токен.");

            _catalogCache = null;
            return new OperationResult(true, $"Аддон «{title}» опубликован и появился у всех.", id);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка публикации: " + ex.Message);
        }
    }

    private static async Task<bool> PutFileAsync(HttpClient client, string path, byte[] bytes, string? sha, string message)
    {
        var payload = new Dictionary<string, object?>
        {
            ["message"] = message,
            ["content"] = Convert.ToBase64String(bytes),
            ["branch"] = "main"
        };
        if (!string.IsNullOrEmpty(sha)) payload["sha"] = sha;

        using var req = new HttpRequestMessage(HttpMethod.Put,
            $"https://api.github.com/repos/aseramong67-afk/Adonis/contents/{Uri.EscapeDataString(path)}")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        using var res = await client.SendAsync(req);
        return res.IsSuccessStatusCode;
    }

    private static async Task<(string? json, string? sha)> GetCatalogForUpdateAsync(HttpClient client)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/aseramong67-afk/Adonis/contents/reskins/catalog.json?ref=main");
        using var res = await client.SendAsync(req);
        if (!res.IsSuccessStatusCode) return (null, null);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var content = doc.RootElement.TryGetProperty("content", out var c) ? c.GetString() : null;
        var sha = doc.RootElement.TryGetProperty("sha", out var s) ? s.GetString() : null;
        if (content is null) return (null, sha);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(content.Replace("\n", "").Replace("\r", "")));
        return (json, sha);
    }

    private static string MakeSlug(string title)
    {
        var translit = new Dictionary<char, string>
        {
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e", ['ё'] = "e",
            ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m",
            ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
            ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch",
            ['ъ'] = "", ['ы'] = "y", ['ь'] = "", ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
        };
        var sb = new StringBuilder();
        foreach (var ch in title.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                if (ch >= 'a' && ch <= 'z') sb.Append(ch);
                else if (ch >= '0' && ch <= '9') sb.Append(ch);
                else if (translit.TryGetValue(ch, out var r)) sb.Append(r);
            }
            else if (ch is ' ' or '-' or '_')
            {
                sb.Append('-');
            }
        }
        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        return slug;
    }

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
            Type = NormalizeType(a.Type),
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
                Type = NormalizeType(json.Type),
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

    private static string NormalizeType(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Аддон";
        return type.Trim().ToLowerInvariant() switch
        {
            "reskin" or "рескин" => "Рескин",
            _ => "Аддон"
        };
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
