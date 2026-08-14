using System.Text.Json;

namespace ReskinManager.Services;

public sealed class SettingsData
{
    public string AddonsTargetPath { get; set; } = "";
    public string AccentColor { get; set; } = "#ff9f1c";
    public List<string> OptimizationOptions { get; set; } = new();
}

public sealed class SettingsService
{
    private readonly string _file;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public SettingsData Current { get; private set; }

    public SettingsService(IWebHostEnvironment env)
    {
        _file = Path.Combine(env.ContentRootPath, "settings.json");
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
}
