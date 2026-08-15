using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.WinForms;

namespace AdonisSetup;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public sealed class SetupBridge
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    private readonly SetupForm _form;

    public SetupBridge(SetupForm form) => _form = form;

    public string GetState()
    {
        var dir = InstallerCore.InstalledDir();
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            installed = dir is not null,
            installDir = dir ?? "",
            defaultDir = InstallerCore.DefaultInstallDir,
            dataDir = InstallerCore.DataDir
        });
    }

    public string BrowseFolder()
    {
        return _form.PickFolder();
    }

    public void BeginDrag()
    {
        ReleaseCapture();
        SendMessage(_form.Handle, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
    }

    public void Minimize() => _form.WindowState = FormWindowState.Minimized;

    public void Close() => _form.Close();

    public void Install(string dir) => _form.StartInstall(dir);

    public void Reinstall(string dir) => _form.StartReinstall(dir);

    public void Uninstall(bool keepData) => _form.StartUninstall(keepData);

    public void Launch() => _form.LaunchApp();

    public string GetAddonsState()
    {
        var path = _form.AddonsPath;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            addonsPath = path ?? "",
            found = path is not null
        });
    }

    public string GetAddonsCatalog()
    {
        var result = _form.GetAddonsCatalog();
        return result ?? "[]";
    }

    public string BrowseAddonsFolder() => _form.PickAddonsFolder();

    public void InstallAddon(string json) => _form.StartAddonInstall(json);

    public void UninstallAddon(string id) => _form.StartAddonUninstall(id);
}

public sealed class SetupForm : Form
{
    private readonly WebView2 _webView = new();
    private string? _addonsPath;

    public string? AddonsPath => _addonsPath;

    public SetupForm()
    {
        Text = "Adonis Setup";
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(700, 520);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(11, 11, 13);

        _webView.Dock = DockStyle.Fill;
        _webView.DefaultBackgroundColor = Color.FromArgb(11, 11, 13);
        Controls.Add(_webView);

        Load += async (_, _) =>
        {
            try
            {
                await _webView.EnsureCoreWebView2Async();
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.AddHostObjectToScript("setupBridge", new SetupBridge(this));

                var html = LoadEmbeddedHtml();
                _webView.CoreWebView2.NavigateToString(html);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось инициализировать WebView2. Убедитесь, что установлен WebView2 Runtime.\n\n" + ex.Message,
                    "Adonis Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        };
    }

    public string PickFolder()
    {
        string result = "";
        Invoke(() =>
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Выберите папку установки Adonis";
            var installed = InstallerCore.InstalledDir();
            if (!string.IsNullOrEmpty(installed) && Directory.Exists(installed))
                dialog.SelectedPath = installed;
            else if (Directory.Exists(InstallerCore.DefaultInstallDir))
                dialog.SelectedPath = InstallerCore.DefaultInstallDir;

            if (dialog.ShowDialog(this) == DialogResult.OK)
                result = dialog.SelectedPath;
        });
        return result;
    }

    public string PickAddonsFolder()
    {
        string result = "";
        Invoke(() =>
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Выберите папку addons Garry's Mod";
            var found = SteamLocator.FindGmodAddonsPath();
            if (!string.IsNullOrEmpty(found) && Directory.Exists(found))
                dialog.SelectedPath = found;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _addonsPath = dialog.SelectedPath;
                result = dialog.SelectedPath;
            }
        });
        return result;
    }

    public async void StartInstall(string dir)
    {
        SetBusy(true);
        Post("log", "Загрузка последней версии…");
        try
        {
            var asset = await InstallerCore.FindLatestAssetAsync();
            if (asset is null)
            {
                Post("error", "Не удалось найти portable-архив в релизе.");
                return;
            }

            var tmp = Path.Combine(Path.GetTempPath(), "adonis_setup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var zip = Path.Combine(tmp, asset.Name);
            var progress = new Progress<int>(p => Post("progress", (p / 100f).ToString("0.##")));

            Post("log", "Скачивание " + asset.Name + "…");
            await InstallerCore.DownloadAsync(asset, zip, progress);

            Post("progress", "1");
            Post("log", "Распаковка…");
            Directory.CreateDirectory(dir);
            InstallerCore.Extract(zip, dir);

            try { Directory.Delete(tmp, true); } catch { }

            InstallerCore.SaveInstalledDir(dir);
            Post("log", "Создание ярлыка…");
            InstallerCore.CreateDesktopShortcut(Path.Combine(dir, "Adonis.exe"), dir);

            Post("done", "Установка завершена.", true);
        }
        catch (Exception ex)
        {
            Post("error", "Ошибка установки: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
            RefreshUi();
        }
    }

    public async void StartReinstall(string dir)
    {
        SetBusy(true);
        Post("log", "Останавливаю запущенный Adonis…");
        InstallerCore.StopApp();
        await Task.Delay(500);

        Post("log", "Удаление старых файлов…");
        InstallerCore.RemoveInstalledDir(dir);

        Post("log", "Загрузка последней версии…");
        try
        {
            var asset = await InstallerCore.FindLatestAssetAsync();
            if (asset is null)
            {
                Post("error", "Не удалось найти portable-архив в релизе.");
                return;
            }

            var tmp = Path.Combine(Path.GetTempPath(), "adonis_setup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var zip = Path.Combine(tmp, asset.Name);
            var progress = new Progress<int>(p => Post("progress", (p / 100f).ToString("0.##")));

            Post("log", "Скачивание " + asset.Name + "…");
            await InstallerCore.DownloadAsync(asset, zip, progress);

            Post("progress", "1");
            Post("log", "Распаковка…");
            Directory.CreateDirectory(dir);
            InstallerCore.Extract(zip, dir);

            try { Directory.Delete(tmp, true); } catch { }

            InstallerCore.SaveInstalledDir(dir);
            Post("log", "Создание ярлыка…");
            InstallerCore.CreateDesktopShortcut(Path.Combine(dir, "Adonis.exe"), dir);

            Post("done", "Переустановка завершена.", true);
        }
        catch (Exception ex)
        {
            Post("error", "Ошибка переустановки: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
            RefreshUi();
        }
    }

    public void StartUninstall(bool keepData)
    {
        var dir = InstallerCore.InstalledDir();
        if (dir is null)
        {
            Post("error", "Adonis не установлен.");
            return;
        }

        SetBusy(true);
        Post("log", "Удаление…");
        try
        {
            InstallerCore.StopApp();
            InstallerCore.RemoveInstalledDir(dir);
            InstallerCore.RemoveState();
            if (!keepData)
            {
                try { if (Directory.Exists(InstallerCore.DataDir)) Directory.Delete(InstallerCore.DataDir, true); } catch { }
            }

            Post("done", "Adonis удалён.", true);
        }
        catch (Exception ex)
        {
            Post("error", "Ошибка удаления: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
            RefreshUi();
        }
    }

    public void LaunchApp()
    {
        var dir = InstallerCore.InstalledDir();
        if (dir is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(Path.Combine(dir, "Adonis.exe"))
            {
                WorkingDirectory = dir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Post("error", "Ошибка запуска: " + ex.Message);
        }
    }

    private string? _addonsCatalogCache;

    public string? GetAddonsCatalog()
    {
        if (_addonsCatalogCache is not null) return _addonsCatalogCache;
        return null;
    }

    public async void LoadAddons()
    {
        SetBusy(true);
        Post("log", "Загрузка каталога аддонов…");
        try
        {
            if (string.IsNullOrWhiteSpace(_addonsPath))
                _addonsPath = SteamLocator.FindGmodAddonsPath();

            var catalog = await InstallerCore.GetCatalogAsync();
            var list = catalog.Addons.Select(a => new
            {
                a.Id,
                a.Title,
                a.Author,
                a.Type,
                a.Tags,
                a.Description,
                a.WorkshopUrl,
                Preview = string.IsNullOrWhiteSpace(a.Preview) ? "" :
                    (a.Preview.StartsWith("http://") || a.Preview.StartsWith("https://") ? a.Preview :
                        "https://raw.githubusercontent.com/aseramong67-afk/Adonis/main/reskins/" + a.Preview.TrimStart('/')),
                SizeText = FormatSize(a.SizeBytes),
                Installed = InstallerCore.AddonInstalled(_addonsPath, a.Id)
            }).ToList();

            _addonsCatalogCache = System.Text.Json.JsonSerializer.Serialize(list);
            Post("addons", _addonsCatalogCache);
        }
        catch (Exception ex)
        {
            Post("error", "Ошибка загрузки каталога: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    public async void StartAddonInstall(string json)
    {
        CatalogAddon? addon;
        try
        {
            addon = System.Text.Json.JsonSerializer.Deserialize<CatalogAddon>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            addon = null;
        }
        if (addon is null)
        {
            Post("error", "Ошибка параметров аддона.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_addonsPath))
            _addonsPath = SteamLocator.FindGmodAddonsPath();
        if (string.IsNullOrWhiteSpace(_addonsPath))
        {
            Post("error", "Папка addons не найдена. Нажмите «Выбрать папку» и укажите её вручную.");
            return;
        }

        SetBusy(true);
        Post("addonbusy", addon.Id);
        Post("log", "Загрузка аддона «" + addon.Title + "»…");
        try
        {
            var progress = new Progress<int>(p => Post("addonprogress", JsonMin(addon.Id, p)));
            var id = await InstallerCore.InstallAddonAsync(addon, _addonsPath, progress);
            Post("addondone", JsonMin(id, true));
        }
        catch (Exception ex)
        {
            Post("addonerror", JsonMin(addon.Id, ex.Message));
        }
        finally
        {
            SetBusy(false);
            RefreshUi();
        }
    }

    public void StartAddonUninstall(string id)
    {
        if (string.IsNullOrWhiteSpace(_addonsPath))
            _addonsPath = SteamLocator.FindGmodAddonsPath();
        if (string.IsNullOrWhiteSpace(_addonsPath))
        {
            Post("error", "Папка addons не найдена.");
            return;
        }

        SetBusy(true);
        Post("addonbusy", id);
        Post("log", "Удаление аддона…");
        try
        {
            InstallerCore.UninstallAddon(_addonsPath, id);
            Post("addondone", JsonMin(id, true));
        }
        catch (Exception ex)
        {
            Post("addonerror", JsonMin(id, ex.Message));
        }
        finally
        {
            SetBusy(false);
            RefreshUi();
        }
    }

    private static string JsonMin(string id, object value)
    {
        return System.Text.Json.JsonSerializer.Serialize(new { id, value });
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "";
        string[] units = ["Б", "КБ", "МБ", "ГБ"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return size.ToString(unit == 0 ? "0" : "0.#") + " " + units[unit];
    }

    private void RefreshUi()
    {
        if (IsHandleCreated) BeginInvoke(() => Post("state", "", false, true));
    }

    private void SetBusy(bool busy)
    {
        if (IsHandleCreated) BeginInvoke(() => Post("busy", busy ? "1" : "0"));
    }

    private void Post(string type, string text = "", bool ok = false, bool refresh = false)
    {
        if (_webView.CoreWebView2 is null) return;
        var msg = System.Text.Json.JsonSerializer.Serialize(new { type, text, ok, refresh });
        BeginInvoke(() =>
        {
            try { _webView.CoreWebView2.PostWebMessageAsJson(msg); } catch { }
        });
    }

    private static string LoadEmbeddedHtml()
    {
        using var stream = typeof(SetupForm).Assembly.GetManifestResourceStream("AdonisSetup.SetupUI.html");
        if (stream is null) return "<html><body style='background:#0b0b0d;color:#fff'>Error</body></html>";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
