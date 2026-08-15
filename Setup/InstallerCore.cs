using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace AdonisSetup;

internal sealed record ReleaseAsset(string Name, string Url);

internal static class InstallerCore
{
    private const string Owner = "aseramong67-afk";
    private const string Repo = "Adonis";

    public static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adonis");

    private static readonly string StateFile = Path.Combine(DataDir, "install.json");

    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler { UseProxy = false };
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Adonis-Setup/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static string DefaultInstallDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Adonis");

    public static string? InstalledDir()
    {
        try
        {
            if (!File.Exists(StateFile)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(StateFile));
            var dir = doc.RootElement.TryGetProperty("installDir", out var d) ? d.GetString() : null;
            return !string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir) ? dir : null;
        }
        catch
        {
            return null;
        }
    }

    public static void SaveInstalledDir(string dir)
    {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(StateFile, JsonSerializer.Serialize(new { installDir = dir }));
    }

    public static async Task<ReleaseAsset?> FindLatestAssetAsync()
    {
        using var res = await Http.GetAsync($"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");
        if (!res.IsSuccessStatusCode) return null;

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        if (!doc.RootElement.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (string.IsNullOrEmpty(name) || !name.StartsWith("Adonis-portable-", StringComparison.OrdinalIgnoreCase)
                || !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                continue;

            var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
            if (!string.IsNullOrEmpty(url))
                return new ReleaseAsset(name, url);
        }

        return null;
    }

    public static async Task<string> DownloadAsync(ReleaseAsset asset, string destPath, IProgress<int>? progress)
    {
        using var res = await Http.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead);
        res.EnsureSuccessStatusCode();

        var total = res.Content.Headers.ContentLength ?? 0;
        await using var src = await res.Content.ReadAsStreamAsync();
        await using var dst = File.Create(destPath);

        var buffer = new byte[81920];
        long read = 0;
        int chunk;
        while ((chunk = await src.ReadAsync(buffer)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, chunk));
            read += chunk;
            if (total > 0) progress?.Report((int)(read * 100 / total));
        }

        return destPath;
    }

    public static void Extract(string zipPath, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        using var zip = ZipFile.OpenRead(zipPath);
        foreach (var entry in zip.Entries)
        {
            var dest = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
            if (!dest.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Небезопасный путь в архиве: " + entry.FullName);

            if (entry.FullName.EndsWith("/"))
            {
                Directory.CreateDirectory(dest);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (entry.Length == 0)
            {
                File.WriteAllBytes(dest, []);
                continue;
            }
            entry.ExtractToFile(dest, overwrite: true);
        }
    }

    public static bool IsRunning() =>
        Process.GetProcessesByName("Adonis").Length > 0;

    public static void StopApp()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("Adonis"))
            {
                try { p.Kill(); } catch { }
                p.WaitForExit(5000);
            }
        }
        catch
        {
        }
    }

    public static void CreateDesktopShortcut(string appExe, string installDir)
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var lnk = Path.Combine(desktop, "Adonis.lnk");
            var shell = Type.GetTypeFromProgID("WScript.Shell");
            if (shell is null) return;
            dynamic ws = Activator.CreateInstance(shell)!;
            dynamic shortcut = ws.CreateShortcut(lnk);
            shortcut.TargetPath = appExe;
            shortcut.WorkingDirectory = installDir;
            shortcut.IconLocation = appExe;
            shortcut.Save();
        }
        catch
        {
        }
    }

    public static void RemoveDesktopShortcut()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var lnk = Path.Combine(desktop, "Adonis.lnk");
            if (File.Exists(lnk)) File.Delete(lnk);
        }
        catch
        {
        }
    }

    public static void RemoveInstalledDir(string dir)
    {
        StopApp();
        for (var i = 0; i < 3; i++)
        {
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                return;
            }
            catch
            {
                Thread.Sleep(500);
            }
        }
    }

    public static void ClearState() => RemoveDesktopShortcut();

    public static void RemoveState()
    {
        ClearState();
        try { if (File.Exists(StateFile)) File.Delete(StateFile); } catch { }
    }
}
