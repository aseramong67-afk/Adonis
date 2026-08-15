using System.Diagnostics;

namespace AdonisSetup;

internal sealed class SetupForm : Form
{
    private readonly TextBox _pathBox;
    private readonly Button _browseBtn;
    private readonly Button _installBtn;
    private readonly Button _reinstallBtn;
    private readonly Button _uninstallBtn;
    private readonly Button _launchBtn;
    private readonly CheckBox _shortcutChk;
    private readonly CheckBox _keepDataChk;
    private readonly ProgressBar _progress;
    private readonly Label _status;
    private readonly Label _versionLabel;

    private string? _installedDir;

    public SetupForm()
    {
        Text = "Adonis Setup";
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(560, 420);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.FromArgb(232, 232, 236);
        Icon = ExtractIcon();

        var title = new Label
        {
            Text = "Adonis — установка",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 159, 28),
            Location = new Point(24, 18),
            AutoSize = true
        };

        var subtitle = new Label
        {
            Text = "Помощник для Garry's Mod: бинды, рескины, настройки.",
            ForeColor = Color.FromArgb(140, 140, 148),
            Location = new Point(24, 52),
            AutoSize = true
        };

        _versionLabel = new Label
        {
            Text = "Проверка версий…",
            ForeColor = Color.FromArgb(140, 140, 148),
            Location = new Point(24, 76),
            AutoSize = true
        };

        var pathLabel = new Label
        {
            Text = "Папка установки:",
            Location = new Point(24, 110),
            AutoSize = true
        };

        _pathBox = new TextBox
        {
            Location = new Point(24, 132),
            Width = 400,
            Text = InstallerCore.DefaultInstallDir,
            BackColor = Color.FromArgb(32, 32, 38),
            ForeColor = Color.FromArgb(232, 232, 236),
            BorderStyle = BorderStyle.FixedSingle
        };

        _browseBtn = new Button
        {
            Text = "Обзор…",
            Location = new Point(434, 130),
            Size = new Size(100, 28)
        };
        _browseBtn.Click += Browse_Click;

        _shortcutChk = new CheckBox
        {
            Text = "Создать ярлык на рабочем столе",
            Location = new Point(24, 172),
            AutoSize = true,
            Checked = true
        };

        _keepDataChk = new CheckBox
        {
            Text = "При удалении сохранять данные (бинды, настройки)",
            Location = new Point(24, 200),
            AutoSize = true,
            Checked = true
        };

        _installBtn = new Button
        {
            Text = "Установить",
            Location = new Point(24, 250),
            Size = new Size(170, 38),
            BackColor = Color.FromArgb(255, 159, 28),
            ForeColor = Color.FromArgb(20, 20, 24),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };
        _installBtn.Click += Install_Click;

        _reinstallBtn = new Button
        {
            Text = "Переустановить",
            Location = new Point(204, 250),
            Size = new Size(150, 38),
            BackColor = Color.FromArgb(48, 48, 56),
            ForeColor = Color.FromArgb(232, 232, 236),
            FlatStyle = FlatStyle.Flat
        };
        _reinstallBtn.Click += Reinstall_Click;

        _uninstallBtn = new Button
        {
            Text = "Удалить",
            Location = new Point(364, 250),
            Size = new Size(150, 38),
            BackColor = Color.FromArgb(90, 40, 40),
            ForeColor = Color.FromArgb(232, 232, 236),
            FlatStyle = FlatStyle.Flat
        };
        _uninstallBtn.Click += Uninstall_Click;

        _launchBtn = new Button
        {
            Text = "Запустить Adonis",
            Location = new Point(24, 300),
            Size = new Size(170, 32),
            BackColor = Color.FromArgb(48, 48, 56),
            ForeColor = Color.FromArgb(232, 232, 236),
            FlatStyle = FlatStyle.Flat,
            Visible = false
        };
        _launchBtn.Click += Launch_Click;

        _progress = new ProgressBar
        {
            Location = new Point(24, 352),
            Width = 510,
            Height = 16,
            Style = ProgressBarStyle.Continuous
        };

        _status = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(180, 180, 188),
            Location = new Point(24, 376),
            AutoSize = true
        };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(_versionLabel);
        Controls.Add(pathLabel);
        Controls.Add(_pathBox);
        Controls.Add(_browseBtn);
        Controls.Add(_shortcutChk);
        Controls.Add(_keepDataChk);
        Controls.Add(_installBtn);
        Controls.Add(_reinstallBtn);
        Controls.Add(_uninstallBtn);
        Controls.Add(_launchBtn);
        Controls.Add(_progress);
        Controls.Add(_status);

        Shown += SetupForm_Shown;
    }

    private static Icon? ExtractIcon()
    {
        try
        {
            var stream = typeof(SetupForm).Assembly.GetManifestResourceStream("AdonisSetup.app.ico");
            return stream is null ? null : new Icon(stream);
        }
        catch
        {
            return null;
        }
    }

    private async void SetupForm_Shown(object? sender, EventArgs e)
    {
        _installedDir = InstallerCore.InstalledDir();
        RefreshState();
        await CheckVersionAsync();
    }

    private async Task CheckVersionAsync()
    {
        try
        {
            var asset = await InstallerCore.FindLatestAssetAsync();
            _versionLabel.Text = asset is null
                ? "Не удалось получить список версий."
                : $"Доступна версия: {asset.Name.Replace("Adonis-portable-", "").Replace("-win-x64.zip", "")}";
        }
        catch (Exception ex)
        {
            _versionLabel.Text = "Ошибка проверки версий: " + ex.Message;
        }
    }

    private void RefreshState()
    {
        _installedDir = InstallerCore.InstalledDir();
        if (_installedDir is not null)
        {
            _status.Text = "Установлено в: " + _installedDir;
            _pathBox.Text = _installedDir;
            _uninstallBtn.Enabled = true;
            _reinstallBtn.Enabled = true;
            _installBtn.Text = "Обновить";
            _launchBtn.Visible = true;
        }
        else
        {
            _status.Text = "Adonis не установлен.";
            _pathBox.Text = InstallerCore.DefaultInstallDir;
            _uninstallBtn.Enabled = false;
            _reinstallBtn.Enabled = false;
            _installBtn.Text = "Установить";
            _launchBtn.Visible = false;
        }
    }

    private void Browse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Выберите папку установки Adonis";
        if (_pathBox.Text.Length > 0 && Directory.Exists(_pathBox.Text))
            dialog.SelectedPath = _pathBox.Text;
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _pathBox.Text = dialog.SelectedPath;
    }

    private async void Install_Click(object? sender, EventArgs e)
    {
        var dir = _pathBox.Text.Trim().TrimEnd('\\');
        if (dir.Length == 0 || !Path.IsPathRooted(dir))
        {
            SetStatus("Укажите корректную папку установки.");
            return;
        }

        if (dir == InstallerCore.DefaultInstallDir)
        {
            // совпадает с папкой данных — нельзя, чтобы не стереть данные
        }

        if (InstallerCore.IsRunning())
        {
            SetStatus("Останавливаю запущенный Adonis…");
            InstallerCore.StopApp();
            await Task.Delay(500);
        }

        SetBusy(true);
        SetStatus("Загрузка последней версии…");
        try
        {
            var asset = await InstallerCore.FindLatestAssetAsync();
            if (asset is null)
            {
                SetStatus("Ошибка: не удалось найти portable-архив в релизе.");
                return;
            }

            var tmp = Path.Combine(Path.GetTempPath(), "adonis_setup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var zip = Path.Combine(tmp, asset.Name);
            var progress = new Progress<int>(p => _progress.Value = Math.Min(p, 100));

            SetStatus($"Скачивание {asset.Name}…");
            await InstallerCore.DownloadAsync(asset, zip, progress);

            _progress.Value = 100;
            SetStatus("Распаковка…");
            Directory.CreateDirectory(dir);
            InstallerCore.Extract(zip, dir);

            try { Directory.Delete(tmp, true); } catch { }

            InstallerCore.SaveInstalledDir(dir);
            if (_shortcutChk.Checked)
                InstallerCore.CreateDesktopShortcut(Path.Combine(dir, "Adonis.exe"), dir);

            RefreshState();
            SetStatus("Установка завершена. Adonis готов к запуску.");
            _launchBtn.Visible = true;
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка установки: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void Reinstall_Click(object? sender, EventArgs e)
    {
        var dir = _pathBox.Text.Trim().TrimEnd('\\');
        if (dir.Length == 0 || !Path.IsPathRooted(dir))
        {
            SetStatus("Укажите корректную папку установки.");
            return;
        }

        if (MessageBox.Show(this, "Переустановить Adonis в папку:\n" + dir +
            "\n\nДанные (бинды, настройки) будут сохранены.", "Переустановка",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            return;

        SetBusy(true);
        try
        {
            SetStatus("Удаление старых файлов…");
            InstallerCore.StopApp();
            await Task.Delay(500);
            InstallerCore.RemoveInstalledDir(dir);

            var asset = await InstallerCore.FindLatestAssetAsync();
            if (asset is null)
            {
                SetStatus("Ошибка: не удалось найти portable-архив в релизе.");
                return;
            }

            var tmp = Path.Combine(Path.GetTempPath(), "adonis_setup_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            var zip = Path.Combine(tmp, asset.Name);
            var progress = new Progress<int>(p => _progress.Value = Math.Min(p, 100));

            SetStatus($"Скачивание {asset.Name}…");
            await InstallerCore.DownloadAsync(asset, zip, progress);

            _progress.Value = 100;
            SetStatus("Распаковка…");
            Directory.CreateDirectory(dir);
            InstallerCore.Extract(zip, dir);

            try { Directory.Delete(tmp, true); } catch { }

            InstallerCore.SaveInstalledDir(dir);
            if (_shortcutChk.Checked)
                InstallerCore.CreateDesktopShortcut(Path.Combine(dir, "Adonis.exe"), dir);

            RefreshState();
            SetStatus("Переустановка завершена.");
            _launchBtn.Visible = true;
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка переустановки: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Uninstall_Click(object? sender, EventArgs e)
    {
        var dir = _installedDir;
        if (dir is null)
        {
            SetStatus("Adonis не установлен.");
            return;
        }

        var msg = "Удалить Adonis из папки:\n" + dir + "\n";
        if (_keepDataChk.Checked)
            msg += "\nДанные (бинды, настройки) будут сохранены в " + InstallerCore.DataDir;
        else
            msg += "\nВнимание: данные в " + InstallerCore.DataDir + " будут удалены.";

        if (MessageBox.Show(this, msg, "Удаление",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            return;

        SetBusy(true);
        SetStatus("Удаление…");
        try
        {
            InstallerCore.StopApp();
            InstallerCore.RemoveInstalledDir(dir);
            InstallerCore.RemoveState();
            if (!_keepDataChk.Checked)
            {
                try { if (Directory.Exists(InstallerCore.DataDir)) Directory.Delete(InstallerCore.DataDir, true); } catch { }
            }

            RefreshState();
            SetStatus("Adonis удалён.");
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка удаления: " + ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void Launch_Click(object? sender, EventArgs e)
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
            SetStatus("Ошибка запуска: " + ex.Message);
        }
    }

    private void SetBusy(bool busy)
    {
        _installBtn.Enabled = !busy;
        _reinstallBtn.Enabled = !busy && _installedDir is not null;
        _uninstallBtn.Enabled = !busy && _installedDir is not null;
        _browseBtn.Enabled = !busy;
        _pathBox.Enabled = !busy;
        _progress.Value = 0;
    }

    private void SetStatus(string text)
    {
        _status.Text = text;
        Application.DoEvents();
    }
}
