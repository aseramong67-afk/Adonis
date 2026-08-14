namespace ReskinManager.Services;

public sealed record OptimizationOption(string Key, string Title, string Description, string[] Commands);

public sealed class OptimizationService
{
    public static readonly OptimizationOption[] Options =
    [
        new("particles", "Отключить частицы", "Дым, искры и визуальные эффекты", ["r_drawparticles 0"]),
        new("detail", "Убрать детализацию", "Скрывает мелкие детали уровня", ["cl_detailfade 0", "cl_detaildist 0"]),
        new("decals", "Сократить декали", "Ограничивает пятна от попаданий", ["r_decals 10"]),
        new("modeldecals", "Декали на моделях", "Убирает декали с оружия и моделей", ["r_drawmodeldecals 0"]),
        new("specular", "Отключить блики", "Зеркальные отражения и блики", ["mat_specular 0"]),
        new("bumpmap", "Отключить рельеф", "Карты нормалей на поверхностях", ["mat_bumpmap 0"]),
        new("fps", "Лимит FPS", "Ограничивает частоту кадров", ["fps_max 300"])
    ];

    private const string CfgFileName = "adonis_optimization.cfg";
    private const string ExecMarker = "exec adonis_optimization.cfg";

    private readonly SettingsService _settings;

    public OptimizationService(SettingsService settings) => _settings = settings;

    public List<string> EnabledKeys()
    {
        var stored = _settings.Current.OptimizationOptions ?? [];
        var valid = Options.Select(o => o.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return stored
            .Where(k => !string.IsNullOrWhiteSpace(k) && valid.Contains(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(k => k.Trim())
            .ToList();
    }

    public void SaveEnabledKeys(List<string> keys)
    {
        var data = _settings.Current;
        data.OptimizationOptions = keys;
        _settings.Save(data);
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

    public bool IsApplied()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null) return false;

        var optFile = Path.Combine(cfgDir, CfgFileName);
        if (!File.Exists(optFile)) return false;

        var autoexec = Path.Combine(cfgDir, "autoexec.cfg");
        return File.Exists(autoexec)
            && File.ReadAllText(autoexec).Contains(ExecMarker, StringComparison.OrdinalIgnoreCase);
    }

    public OperationResult Apply()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null)
            return new OperationResult(false, "Не задана папка установки аддонов. Укажите её в настройках.");

        try
        {
            Directory.CreateDirectory(cfgDir);

            var enabled = EnabledKeys();
            var lines = new List<string> { "// ==== Adonis optimization ====" };
            foreach (var opt in Options)
            {
                if (enabled.Contains(opt.Key, StringComparer.OrdinalIgnoreCase))
                    lines.AddRange(opt.Commands);
            }
            lines.Add("// ==== /Adonis optimization ====");
            File.WriteAllLines(Path.Combine(cfgDir, CfgFileName), lines);

            EnsureExecInAutoexec(cfgDir);
            return new OperationResult(true, "Конфиг оптимизации применён");
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка записи конфига: " + ex.Message);
        }
    }

    public OperationResult Remove()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null)
            return new OperationResult(false, "Не задана папка установки аддонов. Укажите её в настройках.");

        try
        {
            var optFile = Path.Combine(cfgDir, CfgFileName);
            if (File.Exists(optFile)) File.Delete(optFile);

            var autoexec = Path.Combine(cfgDir, "autoexec.cfg");
            if (File.Exists(autoexec))
            {
                var lines = File.ReadAllLines(autoexec)
                    .Where(l => !l.Trim().Equals(ExecMarker, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1]))
                    lines.RemoveAt(lines.Count - 1);
                File.WriteAllLines(autoexec, lines);
            }

            return new OperationResult(true, "Конфиг оптимизации отключён");
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка отключения конфига: " + ex.Message);
        }
    }

    public void Ensure()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null) return;

        if (!File.Exists(Path.Combine(cfgDir, CfgFileName))) return;

        try
        {
            EnsureExecInAutoexec(cfgDir);
        }
        catch { }
    }

    private static void EnsureExecInAutoexec(string cfgDir)
    {
        var autoexec = Path.Combine(cfgDir, "autoexec.cfg");
        var content = File.Exists(autoexec) ? File.ReadAllText(autoexec) : "";
        if (!content.Contains(ExecMarker, StringComparison.OrdinalIgnoreCase))
        {
            var tail = content.Length == 0 ? "" : (content.EndsWith("\n") ? "" : "\n");
            content += tail + "\n" + ExecMarker + "\n";
            File.WriteAllText(autoexec, content);
        }
    }
}
