using System.Text.Json;

namespace ReskinManager.Services;

public sealed class SettingsData
{
    public string AddonsTargetPath { get; set; } = "";
    public string AccentColor { get; set; } = "#ff9f1c";
    public List<string> OptimizationOptions { get; set; } = new();
    public List<string> LaunchOptions { get; set; } = new();
    public bool HandsEnabled { get; set; } = true;
    public int HandsFov { get; set; } = 90;
    public string GitHubToken { get; set; } = "";
}

public sealed record LaunchOption(string Key, string Arg, string Title, string Description);

public sealed class SettingsService
{
    private readonly string _file;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SettingsData Current { get; private set; }

    public SettingsService(IWebHostEnvironment env)
    {
        _file = AppPaths.Resolve("settings.json", env.ContentRootPath);
        Current = Load();
    }

    private SettingsData Load()
    {
        try
        {
            if (File.Exists(_file))
                return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(_file), JsonOpts) ?? new SettingsData();
        }
        catch { }

        return new SettingsData();
    }

    public void Save(SettingsData data)
    {
        Current = data;
        File.WriteAllText(_file, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static readonly LaunchOption[] LaunchOptions =
    [
        new("novid", "-novid", "Без заставки", "пропускает стартовый ролик"),
        new("nojoy", "-nojoy", "Без джойстика", "отключает поддержку геймпада"),
        new("dxlevel", "-dxlevel 95", "DirectX 9.5", "использует DirectX 9.5"),
        new("matqueue", "+mat_queue_mode 2", "Ускоренный рендеринг", "включает многопоточный рендеринг"),
        new("high", "-high", "Высокий приоритет", "высокий приоритет для процессора")
    ];

    public List<string> EnabledLaunchKeys()
    {
        var stored = Current.LaunchOptions ?? [];
        var valid = LaunchOptions.Select(o => o.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return stored
            .Where(k => !string.IsNullOrWhiteSpace(k) && valid.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(k => k.Trim())
            .ToList();
    }

    public void SaveLaunchKeys(List<string> keys)
    {
        var data = Current;
        data.LaunchOptions = keys;
        Save(data);
    }

    public string BuildLaunchUrl()
    {
        var enabled = EnabledLaunchKeys();
        if (enabled.Count == 0) return "steam://rungameid/4000";

        var args = LaunchOptions
            .Where(o => enabled.Contains(o.Key, StringComparer.OrdinalIgnoreCase))
            .Select(o => o.Arg);
        return $"steam://run/4000//{Uri.EscapeDataString(string.Join(" ", args))}/";
    }
}
