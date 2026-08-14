using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReskinManager.Services;

public sealed class BindEntry
{
    public string Key { get; set; } = "";
    public string Command { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Group { get; set; } = "";
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
        var list = new List<BindEntry>();
        try
        {
            if (File.Exists(_file))
            {
                list = JsonSerializer.Deserialize<List<BindEntry>>(File.ReadAllText(_file), JsonOpts) ?? [];
            }
        }
        catch { list = []; }

        if (list.Count == 0)
            return DefaultBinds;

        var merged = MergeDefaults(list);
        if (merged != list)
        {
            try
            {
                File.WriteAllText(_file, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
        return merged;
    }

    private static List<BindEntry> MergeDefaults(List<BindEntry> list)
    {
        var result = new List<BindEntry>(list.Count + DefaultBinds.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var changed = false;

        foreach (var b in list)
        {
            var cmd = b.Command.Trim();
            if (cmd.Length == 0) continue;

            if (!seen.Add(cmd))
            {
                changed = true;
                continue;
            }

            var def = DefaultBinds.FirstOrDefault(d =>
                d.Command.Equals(cmd, StringComparison.OrdinalIgnoreCase));

            if (def is not null &&
                (!def.Description.Equals(b.Description ?? "", StringComparison.Ordinal) ||
                 !def.Category.Equals(b.Category ?? "", StringComparison.Ordinal) ||
                 !def.Group.Equals(b.Group ?? "", StringComparison.Ordinal)))
            {
                b.Description = def.Description;
                b.Category = def.Category;
                b.Group = def.Group;
                changed = true;
            }

            result.Add(b);
        }

        foreach (var d in DefaultBinds)
        {
            var cmd = d.Command.Trim();
            if (seen.Add(cmd))
            {
                result.Add(new BindEntry
                {
                    Key = d.Key,
                    Command = d.Command,
                    Description = d.Description,
                    Category = d.Category,
                    Group = d.Group,
                    Enabled = d.Enabled,
                    Favorite = d.Favorite
                });
                changed = true;
            }
        }

        return changed ? result : list;
    }

    private static readonly List<BindEntry> DefaultBinds =
    [
        new() { Key = "G", Command = "say /buyhealth", Description = "Купить здоровье", Category = "Магазин", Enabled = true, Favorite = true },
        new() { Key = "H", Command = "say /buyarmor", Description = "Купить броню", Category = "Магазин", Enabled = true, Favorite = true },
        new() { Key = "кнопка", Command = "darkrp buyleaves", Description = "Купить листья", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "darkrp buybakingsoda", Description = "Купить содовую", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "darkrp buywaters", Description = "Купить воду", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "darkrp buypot", Description = "Купить кастрюлю", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "darkrp buygas", Description = "Купить газ", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "darkrp buybucket", Description = "Купить ведро", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "say /buyammo ar2", Description = "Патроны для винтовок", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "say /buyammo buckshot", Description = "Патроны для дробовиков", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "say /buyammo smg1", Description = "Патроны для пистолетов-пулемётов", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "say /buyammo pistol", Description = "Патроны для пистолетов", Category = "Магазин", Enabled = false },
        new() { Key = "кнопка", Command = "say /amode", Description = "Админ-мод", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "ulx cloak; ulx god", Description = "Невидимость + бессмертие", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "ulx uncloak; ulx ungod", Description = "Снять невидимость и бессмертие", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "ulx noclip", Description = "Режим полёта", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "ulx armor ^ 100", Description = "Выдать себе 100 брони", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "ulx hp ^ 100", Description = "Выдать себе 100 здоровья", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "fspectate", Description = "Режим наблюдателя", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "say /adminmode", Description = "Админ-мод через чат", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "say !cloak", Description = "Невидимость через чат", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "say !noclip", Description = "Ноклип через чат", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "say !armor ^ 100", Description = "100 брони через чат", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "say !hp ^ 100", Description = "100 здоровья через чат", Category = "Админ", Enabled = false },
        new() { Key = "кнопка", Command = "say /me Показал(а) лицензию", Description = "Показать лицензию", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /try Проверил(а) лицензию", Description = "Проверить лицензию", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /y Стоять! Лицом к стене!", Description = "Крик: стоять у стены", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /y Ушел 1", Description = "Крик: Ушёл 1", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /y Ушел 2", Description = "Крик: Ушёл 2", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /y Ушел 3", Description = "Крик: Ушёл 3", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "ctp", Description = "3-е лицо (камера)", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /me Показал(а) значок FBI", Description = "Показать значок FBI", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /y Лицом к небу, не шевелиться 1...2...3...", Description = "Крик: лицом к небу, не шевелиться", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /me Связал. Отобрал все вещи, положил в рюкзак", Description = "Связал, отобрал вещи", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say /roll", Description = "Кинуть кубики", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "say !gang", Description = "Меню банды", Category = "Чат / РП", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1642", Description = "Танец", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1610", Description = "Пальцы вверх", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1611", Description = "Иди за мной", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1612", Description = "Лук", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1613", Description = "Бессловесное нет", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1615", Description = "Махать рукой", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1616", Description = "Поза льва", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1617", Description = "Sexy Dance", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "_DarkRP_DoAnimation 1618", Description = "Смех", Category = "Анимации", Enabled = false },
        new() { Key = "кнопка", Command = "say /citizen", Description = "Гражданин", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /buis", Description = "Бизнесмен", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /ohotr", Description = "Охотник на собак", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /parkour", Description = "Паркурист", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /sec", Description = "Охранник", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /bankir", Description = "Банкир", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /bit", Description = "Битмайнер", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /fishman", Description = "Рыбак", Category = "Профессии", Group = "Профессии", Enabled = false },
        new() { Key = "кнопка", Command = "say /hobo", Description = "Бездомный", Category = "Профессии", Group = "Бездомные", Enabled = false },
        new() { Key = "кнопка", Command = "say /hoboo", Description = "Глава бездомных", Category = "Профессии", Group = "Бездомные", Enabled = false },
        new() { Key = "кнопка", Command = "say /coop", Description = "Офицер полиции", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /chief", Description = "Начальник полиции", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /omon", Description = "Спецназ", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /domon", Description = "Медик спецназа", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /somon", Description = "Снайпер спецназа", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /rspecnaz", Description = "Командир спецназа", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /fbi", Description = "Агент FBI", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /jaga", Description = "Джаггернаут", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /mayor", Description = "Мэр", Category = "Профессии", Group = "Правопорядок", Enabled = false },
        new() { Key = "кнопка", Command = "say /narko", Description = "Гровер", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /chemicgangster", Description = "Наркодилер", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /murder", Description = "Маньяк", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /killer", Description = "Наёмный убийца", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /assasin", Description = "Ассасин", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /gangster", Description = "Бандит", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /mobboss", Description = "Глава бандитов", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /egangster", Description = "Элитный бандит", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /vor", Description = "Вор", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /statvipvorom", Description = "Умелый вор", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /shpion", Description = "Шпион", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /grabitel", Description = "Грабитель", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /hacker", Description = "Хакер", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /zmlab2_MethCook", Description = "Варщик мета", Category = "Профессии", Group = "Криминал", Enabled = false },
        new() { Key = "кнопка", Command = "say /gundealer", Description = "Продавец оружия", Category = "Профессии", Group = "Торговцы", Enabled = false },
        new() { Key = "кнопка", Command = "say /medic", Description = "Врач", Category = "Профессии", Group = "Торговцы", Enabled = false },
        new() { Key = "кнопка", Command = "say /store", Description = "Торговец", Category = "Профессии", Group = "Торговцы", Enabled = false },
        new() { Key = "кнопка", Command = "say /cook", Description = "Повар", Category = "Профессии", Group = "Торговцы", Enabled = false },
        new() { Key = "кнопка", Command = "say /doge", Description = "Собака", Category = "Профессии", Group = "Другие", Enabled = false },
        new() { Key = "кнопка", Command = "say /nonrp", Description = "Администрация", Category = "Профессии", Group = "Другие", Enabled = false },
        new() { Key = "кнопка", Command = "use tmp", Description = "Бинд на TMP", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use spas 12", Description = "Бинд на SPAS-12", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use weapon_shotgun", Description = "Дробовик (хорош на флагах)", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use weapon_FlechetteGun", Description = "Ковбойка", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use awpdragon", Description = "Длор", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use m9k_usas", Description = "Юсас", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use m9k_dbarrel", Description = "Дабла", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use weapon_mad_2b", Description = "Катана", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use m9k_barret_m82", Description = "Баретка", Category = "Оружие", Enabled = false },
        new() { Key = "кнопка", Command = "use itemstore_pickup", Description = "Инвентарь", Category = "Разное", Enabled = false },
        new() { Key = "Z", Command = "gmod_undo", Description = "Отменить последнее действие", Category = "Разное", Enabled = false },
        new() { Key = "кнопка", Command = "noclip", Description = "Ноклип (одиночная игра)", Category = "Разное", Enabled = false },
        new() { Key = "кнопка", Command = "say !spectate", Description = "Спек для админов", Category = "Админ", Enabled = false }
    ];

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

    public List<BindEntry> ScanGameConfigs()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null || !Directory.Exists(cfgDir)) return [];

        var found = new List<BindEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var files = Directory.GetFiles(cfgDir, "*.cfg")
            .Where(f => !Path.GetFileName(f).Equals("adonis_binds.cfg", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).StartsWith("360controller", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            string[] lines;
            try { lines = File.ReadAllLines(file); }
            catch { continue; }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//")) continue;

                var m = Regex.Match(line, @"^bind\s+([^\s]+)\s+(.+)$", RegexOptions.IgnoreCase);
                if (!m.Success) continue;

                var key = m.Groups[1].Value.Trim().Trim('"');
                var command = m.Groups[2].Value.Trim().TrimEnd(';');
                if (command.Length >= 2 && command[0] == '"' && command[^1] == '"')
                    command = command.Substring(1, command.Length - 2).Trim();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(command)) continue;
                if (IsSystemBind(command) || IsSystemKey(key)) continue;

                var id = key + "\u0001" + command;
                if (!seen.Add(id)) continue;

                var known = Current.FirstOrDefault(x =>
                    x.Command.Equals(command, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(x.Description));

                found.Add(new BindEntry
                {
                    Key = key,
                    Command = command,
                    Description = known?.Description ?? "",
                    Category = known?.Category ?? "",
                    Group = known?.Group ?? "",
                    Enabled = true,
                    Favorite = false
                });
            }
        }

        return found;
    }

    private static bool IsSystemBind(string command)
    {
        var first = command.Split(' ', ';')[0].ToLowerInvariant();
        if (first.Length == 0) return true;
        if (first.StartsWith('+')) return true;
        if (first.StartsWith("slot")) return true;
        return first is "impulse" or "invnext" or "invprev" or "invlast"
            or "messagemode" or "messagemode2" or "messagemode4" or "toggleconsole"
            or "escape" or "jpeg" or "kill" or "disconnect" or "quit" or "pause"
            or "save" or "load"
            or "kp_next" or "kp_prev" or "kp_slot1" or "kp_slot2" or "kp_slot3"
            or "kp_slot4" or "kp_slot5" or "kp_slot6" or "kp_slot7" or "kp_slot8"
            or "kp_slot9" or "kp_slot10" or "kp_down" or "kp_up" or "kp_left" or "kp_right"
            or "joy_use_forward" or "joy_use_back" or "joy_use_left" or "joy_use_right"
            or "joy_attack" or "joy_attack2" or "joy_duck" or "joy_usesimple" or "joy_zoom"
            or "menu_accept" or "menu_cancel" or "menu_left" or "menu_right" or "menu_up" or "menu_down"
            or "showscores" or "showmap" or "showbriefing";
    }

    private static bool IsSystemKey(string key) => key.ToLowerInvariant() switch
    {
        "espace" or "escape" or "esc" => true,
        "f1" or "f2" or "f3" or "f4" => true,
        "mouse1" or "mouse2" or "mouse3" or "mouse4" or "mouse5" => true,
        "joy1" or "joy2" or "joy3" or "joy4" or "joy5" or "joy6" or "joy7" or "joy8" or "joy9" or "joy10" => true,
        "stick1" or "stick2" or "pov_up" or "pov_down" or "pov_left" or "pov_right" => true,
        _ => false
    };

    public (int Found, int Added) MergeScanned()
    {
        var found = ScanGameConfigs();
        if (found.Count == 0) return (0, 0);

        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in Current)
            known.Add(b.Command.Trim());

        var added = 0;
        var merged = new List<BindEntry>(Current);
        foreach (var b in found)
        {
            if (known.Contains(b.Command.Trim())) continue;
            known.Add(b.Command.Trim());
            merged.Add(b);
            added++;
        }

        if (added == 0) return (found.Count, 0);

        Current = merged;
        try
        {
            File.WriteAllText(_file, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }

        ApplyToCfg();
        return (found.Count, added);
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
                Group = (b.Group ?? "").Trim(),
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

    public OperationResult ApplyHands()
    {
        var cfgDir = CfgDir();
        if (cfgDir is null)
            return new OperationResult(false, "Не задана папка установки аддонов. Укажите её в настройках.");

        try
        {
            Directory.CreateDirectory(cfgDir);

            var handsCfg = Path.Combine(cfgDir, "adonis_hands.cfg");
            var s = _settings.Current;
            var lines = new List<string>
            {
                "// ==== Adonis hands ====",
                s.HandsEnabled ? $"viewmodel_fov {s.HandsFov}" : "// убрать руки отключено",
                "// ==== /Adonis hands ===="
            };
            File.WriteAllLines(handsCfg, lines);

            var autoexec = Path.Combine(cfgDir, "autoexec.cfg");
            var content = File.Exists(autoexec) ? File.ReadAllText(autoexec) : "";
            const string marker = "exec adonis_hands.cfg";
            if (!content.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                var tail = content.Length == 0 ? "" : (content.EndsWith("\n") ? "" : "\n");
                content += tail + "\n" + marker + "\n";
                File.WriteAllText(autoexec, content);
            }

            return new OperationResult(true, "Убрать руки: " + (s.HandsEnabled ? $"включено (FOV {s.HandsFov})" : "выключено"));
        }
        catch (Exception ex)
        {
            return new OperationResult(false, "Ошибка записи конфига: " + ex.Message);
        }
    }
}
