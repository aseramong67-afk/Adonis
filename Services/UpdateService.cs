using System.Diagnostics;
using System.Text.Json;
using ReskinManager.Models;

namespace ReskinManager.Services;

public sealed record UpdateInfo(
    bool HasUpdate,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseName = "",
    string ReleaseNotes = "",
    string AssetUrl = "");

public sealed record ReleaseNote(string Version, string Name, string Notes);

public sealed class UpdateService
{
    private readonly GitHubOptions _gitHub;
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { UseProxy = false };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Adonis-Updater/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public string CurrentVersion { get; } = GetCurrentVersion();

    public string? LastError { get; private set; }

    public UpdateService(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _gitHub = config.GetSection("GitHub").Get<GitHubOptions>() ?? new GitHubOptions();
    }

    private static string GetCurrentVersion()
    {
        try
        {
            var asm = typeof(UpdateService).Assembly;
            var exePath = string.IsNullOrWhiteSpace(asm.Location)
                ? Path.Combine(AppContext.BaseDirectory, $"{asm.GetName().Name}.exe")
                : asm.Location;
            var info = FileVersionInfo.GetVersionInfo(exePath);
            if (!string.IsNullOrWhiteSpace(info.ProductVersion))
            {
                var v = info.ProductVersion.Split('+')[0].Trim();
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
        }
        catch { }
        return "1.0.0";
    }

    private bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_gitHub.Owner) && !string.IsNullOrWhiteSpace(_gitHub.Repo);

    public async Task<UpdateInfo> CheckForUpdateAsync(bool force = false)
    {
        if (!IsConfigured)
            return new UpdateInfo(false, CurrentVersion, CurrentVersion);

        try
        {
            var url = $"https://api.github.com/repos/{_gitHub.Owner}/{_gitHub.Repo}/releases/latest";
            using var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                LastError = $"HTTP {(int)res.StatusCode} {res.ReasonPhrase}";
                return new UpdateInfo(false, CurrentVersion, CurrentVersion);
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : "";
            var name = root.TryGetProperty("name", out var n) ? n.GetString() : "";
            var body = root.TryGetProperty("body", out var b) ? b.GetString() : "";

            var latest = Normalize(tag ?? "");
            var hasUpdate = !string.IsNullOrWhiteSpace(latest) &&
                            CompareVersions(latest, CurrentVersion) > 0;

            var assetUrl = "";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var an) &&
                        an.GetString()?.Equals(_gitHub.ReleaseAsset, StringComparison.OrdinalIgnoreCase) == true &&
                        asset.TryGetProperty("browser_download_url", out var au))
                    {
                        assetUrl = au.GetString() ?? "";
                        break;
                    }
                }
            }

            return new UpdateInfo(hasUpdate, CurrentVersion, latest, name ?? "", body ?? "", assetUrl);
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name + ": " + ex.Message;
            return new UpdateInfo(false, CurrentVersion, CurrentVersion);
        }
    }

    /// <summary>Скачивает zip релиза и возвращает путь к распакованной папке.</summary>
    public async Task<string> DownloadReleaseAsync(UpdateInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.AssetUrl))
            throw new InvalidOperationException("В релизе нет архива.");

        var updateDir = Path.Combine(Path.GetTempPath(), "adonis_update");
        if (Directory.Exists(updateDir)) Directory.Delete(updateDir, true);
        Directory.CreateDirectory(updateDir);

        var zipPath = Path.Combine(updateDir, _gitHub.ReleaseAsset);
        var bytes = await _http.GetByteArrayAsync(info.AssetUrl);
        await File.WriteAllBytesAsync(zipPath, bytes);

        var extractDir = Path.Combine(updateDir, "extracted");
        Directory.CreateDirectory(extractDir);
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

        return extractDir;
    }

    /// <summary>
    /// Готовит файл обновления: скачивает zip, распаковывает в папку рядом с exe
    /// и возвращает путь к командному файлу, который применит обновление после закрытия приложения.
    /// </summary>
    public async Task<OperationResult> PrepareAndApplyAsync()
    {
        if (!IsConfigured)
            return new OperationResult(false, "GitHub не настроен.");

        try
        {
            var info = await CheckForUpdateAsync();
            if (!info.HasUpdate)
                return new OperationResult(false, "Обновления не найдены.");

            var extractDir = await DownloadReleaseAsync(info);

            var appDir = AppContext.BaseDirectory;
            var scriptDir = Path.Combine(appDir, ".update");
            if (Directory.Exists(scriptDir)) Directory.Delete(scriptDir, true);
            Directory.CreateDirectory(scriptDir);

            var stageDir = Path.Combine(scriptDir, "stage");
            if (Directory.Exists(stageDir)) Directory.Delete(stageDir, true);
            Directory.Move(extractDir, stageDir);

            var script = Path.Combine(scriptDir, "apply.cmd");
            var content =
                "@echo off\r\n" +
                "cd /d \"%~dp0..\"\r\n" +
                "timeout /t 1 /nobreak >nul\r\n" +
                "taskkill /f /im Adonis.exe >nul 2>&1\r\n" +
                "timeout /t 2 /nobreak >nul\r\n" +
                "xcopy /e /y /q \"%~dp0stage\\*\" \"%~dp0..\\\" >nul\r\n" +
                "start \"\" \"%~dp0..\\Adonis.exe\"\r\n" +
                "timeout /t 1 /nobreak >nul\r\n" +
                "rmdir /s /q \"%~dp0stage\" >nul 2>&1\r\n" +
                "del \"%~f0\" >nul 2>&1\r\n";
            await File.WriteAllTextAsync(script, content);

            return new OperationResult(true, "Обновление загружено. Приложение будет перезапущено.", script);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка обновления: " + ex.Message);
        }
    }

    /// <summary>
    /// Получает список всех релизов, которые новее текущей версии (от старых к новым),
    /// с их описаниями. Нужен, чтобы показать пользователю изменения всех промежуточных версий.
    /// </summary>
    public async Task<List<ReleaseNote>> GetPendingReleasesAsync()
    {
        if (!IsConfigured) return [];

        try
        {
            var url = $"https://api.github.com/repos/{_gitHub.Owner}/{_gitHub.Repo}/releases?per_page=100";
            using var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) return [];

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var list = new List<ReleaseNote>();
            foreach (var release in doc.RootElement.EnumerateArray())
            {
                var tag = release.TryGetProperty("tag_name", out var t) ? t.GetString() : "";
                var name = release.TryGetProperty("name", out var n) ? n.GetString() : "";
                var body = release.TryGetProperty("body", out var b) ? b.GetString() : "";

                var version = Normalize(tag ?? "");
                if (string.IsNullOrWhiteSpace(version)) continue;
                if (CompareVersions(version, CurrentVersion) <= 0) continue;

                list.Add(new ReleaseNote(version, name ?? "", body ?? ""));
            }

            list.Sort((x, y) => CompareVersions(x.Version, y.Version));
            return list;
        }
        catch
        {
            return [];
        }
    }

    public static void RestartNow(string scriptPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(scriptPath) { UseShellExecute = true });
        }
        catch { }
    }

    private static string Normalize(string tag)
    {
        var v = (tag ?? "").Trim().TrimStart('v', 'V');
        if (v.Count(c => c == '.') == 1) v += ".0";
        if (v.Count(c => c == '.') == 0) v += ".0.0";
        return v;
    }

    private static int CompareVersions(string a, string b)
    {
        var pa = a.Split('+')[0].Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        var pb = b.Split('+')[0].Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        for (var i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            var x = i < pa.Length ? pa[i] : 0;
            var y = i < pb.Length ? pb[i] : 0;
            if (x != y) return x.CompareTo(y);
        }
        return 0;
    }
}
