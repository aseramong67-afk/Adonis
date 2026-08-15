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
}

public sealed class SetupForm : Form
{
    private readonly WebView2 _webView = new();

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
