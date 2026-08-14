using System.Text.Json;

namespace ReskinManager.Services;

public sealed class BindEntry
{
    public string Key { get; set; } = "";
    public string Command { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Author { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool Favorite { get; set; }
}

public sealed class BindsService
{
    private readonly string _file;
    private readonly SettingsService _settings;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BindsService(IWebHostEnvironment env, SettingsService settings)
    {
        _file = Path.Combine(env.ContentRootPath, "binds.json");
        _settings = settings;
        Current = Load();
    }

    public List<BindEntry> Current { get; private set; }

    private List<BindEntry> Load()
    {
        try
        {
            if (File.Exists(_file))
                return JsonSerializer.Deserialize<List<BindEntry>>(File.ReadAllText(_file), JsonOpts) ?? [];
        }
        catch { }

        return [];
    }

    public string? CfgDir()
    {
        var configured = _settings.Current.AddonsTargetPath?.Trim().TrimEnd('\\') ?? "";
        var target = !string.IsNullOrWhiteSpace(configured)
            ? configured
            : SteamLocator.FindGmodAddonsPath() ?? "";

        if (string.IsNullOrWhiteSpace(target)) return null;

        var gmod = Directory.GetParent(target)?.FullName;
        return gmod is null ? null : Path.Combine(gmod, "cfg");
    }

    public OperationResult Save(List<BindEntry> binds)
    {
        var valid = binds
            .Where(b => !string.IsNullOrWhiteSpace(b.Key) && !string.IsNullOrWhiteSpace(b.Command))
            .Select(b => new BindEntry
            {
                Key = b.Key.Trim(),
                Command = b.Command.Trim(),
                Description = (b.Description ?? "").Trim(),
                Category = (b.Category ?? "").Trim(),
                Author = (b.Author ?? "").Trim(),
                Enabled = b.Enabled,
                Favorite = b.Favorite
            })
            .ToList();

        Current = valid;

        try
        {
            File.WriteAllText(_file, JsonSerializer.Serialize(valid, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка сохранения списка биндов: " + ex.Message);
        }

        return ApplyToCfg();
    }

    public OperationResult ApplyToCfg()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null)
            return new OperationResult(false, "Не задана папка установки аддонов. Укажите её в настройках.");

        try
        {
            Directory.CreateDirectory(cfgDir);

            var bindsCfg = Path.Combine(cfgDir, "adonis_binds.cfg");
            var lines = new List<string> { "// ==== Adonis binds ====" };
            foreach (var b in Current.Where(x => x.Enabled))
                lines.Add($"bind \"{b.Key}\" \"{b.Command}\"");
            lines.Add("// ==== /Adonis binds ====");
            File.WriteAllLines(bindsCfg, lines);

            var autoexec = Path.Combine(cfgDir, "autoexec.cfg");
            var content = File.Exists(autoexec) ? File.ReadAllText(autoexec) : "";
            const string marker = "exec adonis_binds.cfg";
            if (!content.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                var tail = content.Length == 0 ? "" : (content.EndsWith("\n") ? "" : "\n");
                content += tail + "\n" + marker + "\n";
                File.WriteAllText(autoexec, content);
            }

            return new OperationResult(true, "Бинды записаны: " + bindsCfg);
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка записи конфига: " + ex.Message);
        }
    }
}
