using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AdonisSetup;

internal static class SteamLocator
{
    public static string? FindGmodAddonsPath()
    {
        foreach (var steamApps in EnumerateSteamLibraries())
        {
            var candidate = Path.Combine(steamApps, "common", "GarrysMod", "garrysmod", "addons");
            if (Directory.Exists(candidate)) return candidate;
        }
        return null;
    }

    private static IEnumerable<string> EnumerateSteamLibraries()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roots = new List<string>();

        var registryPath = ReadRegistrySteamPath();
        if (!string.IsNullOrWhiteSpace(registryPath)) roots.Add(registryPath);

        var x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var prog = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        foreach (var p in new[] { Path.Combine(x86, "Steam"), Path.Combine(prog, "Steam") })
        {
            if (Directory.Exists(p) && !roots.Contains(p, StringComparer.OrdinalIgnoreCase)) roots.Add(p);
        }

        foreach (var root in roots)
        {
            var steamApps = Path.Combine(root, "steamapps");
            if (!Directory.Exists(steamApps)) continue;
            if (seen.Add(steamApps)) yield return steamApps;

            var vdf = Path.Combine(steamApps, "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;

            foreach (var lib in ReadLibraryPaths(vdf))
            {
                var libSteamApps = Path.Combine(lib, "steamapps");
                if (Directory.Exists(libSteamApps) && seen.Add(libSteamApps)) yield return libSteamApps;
            }
        }
    }

    private static string? ReadRegistrySteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> ReadLibraryPaths(string vdf)
    {
        var paths = new List<string>();
        try
        {
            var text = File.ReadAllText(vdf);
            var matches = Regex.Matches(text, "\"path\"\\s*\"([^\"]*)\"");
            foreach (Match m in matches)
            {
                var path = m.Groups[1].Value.Replace("\\\\", "\\");
                if (!string.IsNullOrWhiteSpace(path)) paths.Add(path);
            }
        }
        catch
        {
        }
        return paths;
    }
}
