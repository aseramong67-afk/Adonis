using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.WinForms;

namespace ReskinManager;

public static class DesktopApp
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void Run(string url)
    {
        try
        {
            var h = GetConsoleWindow();
            if (h != IntPtr.Zero)
            {
                ShowWindow(h, 0);
                FreeConsole();
            }
        }
        catch
        {
        }

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(url));
    }
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public sealed class AdonisBridge
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;
    private const int HTLEFT = 0x000A;
    private const int HTRIGHT = 0x000B;
    private const int HTTOP = 0x000C;
    private const int HTTOPLEFT = 0x000D;
    private const int HTTOPRIGHT = 0x000E;
    private const int HTBOTTOM = 0x000F;
    private const int HTBOTTOMLEFT = 0x0010;
    private const int HTBOTTOMRIGHT = 0x0011;

    private static readonly Dictionary<int, int> ResizeEdgeCodes = new()
    {
        [10] = HTLEFT,
        [11] = HTRIGHT,
        [12] = HTTOP,
        [13] = HTTOPLEFT,
        [14] = HTTOPRIGHT,
        [15] = HTBOTTOM,
        [16] = HTBOTTOMLEFT,
        [17] = HTBOTTOMRIGHT
    };

    private readonly MainForm _form;

    public AdonisBridge(MainForm form) => _form = form;

    public void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
        }
    }

    public void Minimize() => OnUi(() => _form.WindowState = FormWindowState.Minimized);

    public void ToggleMaximize() => OnUi(() => _form.WindowState =
        _form.WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized);

    public void Close() => OnUi(_form.Close);

    public void BeginDrag()
    {
        ReleaseCapture();
        SendMessage(_form.Handle, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), IntPtr.Zero);
    }

    public void BeginResize(int edge)
    {
        if (!ResizeEdgeCodes.TryGetValue(edge, out var code)) return;
        ReleaseCapture();
        SendMessage(_form.Handle, WM_NCLBUTTONDOWN, new IntPtr(code), IntPtr.Zero);
    }

    private void OnUi(Action action)
    {
        if (_form.IsHandleCreated && _form.InvokeRequired)
            _form.BeginInvoke(action);
        else
            action();
    }
}

public sealed class MainForm : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    private readonly WebView2 _webView = new();

    public MainForm(string url)
    {
        Text = "Adonis";
        FormBorderStyle = FormBorderStyle.None;
        ClientSize = new Size(1500, 920);
        MinimumSize = new Size(1100, 700);
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
                _webView.CoreWebView2.AddHostObjectToScript("adonisBridge", new AdonisBridge(this));
                _webView.Source = new Uri(url);
            }
            catch
            {
                MessageBox.Show(
                    "Не удалось инициализировать WebView2. Убедитесь, что установлен WebView2 Runtime.",
                    "Adonis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        var pref = DWMWCP_ROUND;
        try { DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); } catch { }
        try { MaximizedBounds = Screen.FromHandle(Handle).WorkingArea; } catch { }
    }
}
