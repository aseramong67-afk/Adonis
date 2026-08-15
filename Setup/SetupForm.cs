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
    private readonly Label _stateLabel;

    private string? _installedDir;

    public SetupForm()
    {
        Text = "Adonis Setup";
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(560, 470);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(11, 11, 13);
        ForeColor = Color.FromArgb(232, 232, 236);
        Icon = ExtractIcon();
        AutoScaleMode = AutoScaleMode.Dpi;
        ShowInTaskbar = true;

        // фон в точку, как в основной программе
        BackgroundImage = CreateDotPattern();
        BackgroundImageLayout = ImageLayout.Tile;

        var title = new Label
        {
            Text = "Adonis",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 159, 28),
            Location = new Point(28, 20),
            AutoSize = true
        };

        var subtitle = new Label
        {
            Text = "Помощник для Garry's Mod — бинды, рескины, настройки.",
            ForeColor = Color.FromArgb(140, 140, 148),
            Location = new Point(28, 60),
            AutoSize = true
        };

        _versionLabel = new Label
        {
            Text = "Проверка версий…",
            ForeColor = Color.FromArgb(140, 140, 148),
            Location = new Point(28, 86),
            AutoSize = true
        };

        // панель состояния установки
        _stateLabel = new Label
        {
            Text = "Проверка состояния…",
            ForeColor = Color.FromArgb(232, 232, 236),
            Location = new Point(28, 118),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };

        var divider = new Panel
        {
            BackColor = Color.FromArgb(38, 38, 43),
            Location = new Point(28, 150),
            Size = new Size(504, 1)
        };

        var pathLabel = new Label
        {
            Text = "ПАПКА УСТАНОВКИ",
            ForeColor = Color.FromArgb(95, 95, 105),
            Location = new Point(28, 166),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };

        _pathBox = new TextBox
        {
            Location = new Point(28, 188),
            Width = 400,
            Text = InstallerCore.DefaultInstallDir,
            BackColor = Color.FromArgb(19, 19, 22),
            ForeColor = Color.FromArgb(232, 232, 236),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5f)
        };

        _browseBtn = new Button
        {
            Text = "Обзор…",
            Location = new Point(438, 187),
            Size = new Size(94, 28),
            BackColor = Color.FromArgb(26, 26, 30),
            ForeColor = Color.FromArgb(232, 232, 236),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _browseBtn.FlatAppearance.BorderColor = Color.FromArgb(58, 58, 65);
        _browseBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 41);
        _browseBtn.Click += Browse_Click;

        _shortcutChk = new CheckBox
        {
            Text = "Создать ярлык на рабочем столе",
            Location = new Point(28, 228),
            AutoSize = true,
            Checked = true,
            ForeColor = Color.FromArgb(200, 200, 208)
        };

        _keepDataChk = new CheckBox
        {
            Text = "При удалении сохранять данные (бинды, настройки)",
            Location = new Point(28, 254),
            AutoSize = true,
            Checked = true,
            ForeColor = Color.FromArgb(200, 200, 208)
        };

        _installBtn = MakePrimaryButton("Установить", 28, 300);
        _installBtn.Click += Install_Click;

        _reinstallBtn = MakeGhostButton("Переустановить", 210, 300);
        _reinstallBtn.Click += Reinstall_Click;

        _uninstallBtn = MakeDangerButton("Удалить", 392, 300);
        _uninstallBtn.Click += Uninstall_Click;

        _launchBtn = MakeGhostButton("Запустить Adonis", 28, 354);
        _launchBtn.Click += Launch_Click;

        _progress = new ProgressBar
        {
            Location = new Point(28, 410),
            Width = 504,
            Height = 6,
            Style = ProgressBarStyle.Continuous,
            BackColor = Color.FromArgb(26, 26, 30),
            ForeColor = Color.FromArgb(255, 159, 28)
        };

        _status = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(160, 160, 170),
            Location = new Point(28, 428),
            AutoSize = true
        };

        Controls.Add(title);
        Controls.Add(subtitle);
        Controls.Add(_versionLabel);
        Controls.Add(_stateLabel);
        Controls.Add(divider);
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

        _reinstallBtn.Visible = false;
        _uninstallBtn.Visible = false;
        _launchBtn.Visible = false;

        Shown += SetupForm_Shown;
    }

    private static Image CreateDotPattern()
    {
        var bmp = new Bitmap(22, 22);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(11, 11, 13));
        g.FillEllipse(new SolidBrush(Color.FromArgb(24, 24, 27)), 10, 10, 2, 2);
        return bmp;
    }

    private Button MakePrimaryButton(string text, int x, int y)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(170, 40),
            BackColor = Color.FromArgb(255, 159, 28),
            ForeColor = Color.FromArgb(20, 20, 24),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(217, 135, 24);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 178, 74);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 143, 22);
        return btn;
    }

    private Button MakeGhostButton(string text, int x, int y)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(170, 40),
            BackColor = Color.FromArgb(26, 26, 30),
            ForeColor = Color.FromArgb(232, 232, 236),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(58, 58, 65);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 41);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 30, 35);
        return btn;
    }

    private Button MakeDangerButton(string text, int x, int y)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(140, 40),
            BackColor = Color.FromArgb(26, 26, 30),
            ForeColor = Color.FromArgb(248, 113, 113),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(140, 70, 70);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 28, 28);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(38, 24, 24);
        return btn;
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
                : "Доступная версия: " + asset.Name.Replace("Adonis-portable-", "").Replace("-win-x64.zip", "");
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
            _stateLabel.Text = "✓ Установлено";
            _stateLabel.ForeColor = Color.FromArgb(74, 222, 128);
            _status.Text = "Установлено в: " + _installedDir;
            _pathBox.Text = _installedDir;
            _installBtn.Text = "Обновить";
            _reinstallBtn.Visible = true;
            _uninstallBtn.Visible = true;
            _launchBtn.Visible = true;
        }
        else
        {
            _stateLabel.Text = "○ Не установлено";
            _stateLabel.ForeColor = Color.FromArgb(140, 140, 148);
            _status.Text = "Adonis не установлен.";
            _pathBox.Text = InstallerCore.DefaultInstallDir;
            _installBtn.Text = "Установить";
            _reinstallBtn.Visible = false;
            _uninstallBtn.Visible = false;
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
