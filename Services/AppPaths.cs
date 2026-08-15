namespace ReskinManager.Services;

public static class AppPaths
{
    private static string? _dataDir;

    /// <summary>
    /// Каталог %APPDATA%\Adonis для пользовательских данных (бинды, настройки, сессии).
    /// Переносит данные из папки приложения при первом запуске (легаси-миграция).
    /// </summary>
    public static string DataDir()
    {
        if (_dataDir is null)
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _dataDir = Path.Combine(baseDir, "Adonis");
            Directory.CreateDirectory(_dataDir);
        }
        return _dataDir;
    }

    public static string FilePath(string fileName) => Path.Combine(DataDir(), fileName);

    /// <summary>
    /// Возвращает путь к файлу данных в AppData. Если там его ещё нет, а рядом с приложением
    /// лежит такой же файл (старая версия), копирует его туда.
    /// </summary>
    public static string Resolve(string fileName, string legacyDir)
    {
        var target = FilePath(fileName);
        if (!File.Exists(target))
        {
            var legacy = Path.Combine(legacyDir, fileName);
            if (File.Exists(legacy))
            {
                try { File.Copy(legacy, target); }
                catch { }
            }
        }
        return target;
    }
}
