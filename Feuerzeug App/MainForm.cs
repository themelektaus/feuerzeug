using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Feuerzeug;

public partial class MainForm : Form
{
    BlazorWebView blazorWebView;
    FileSystemWatcher fileWatcher;

    public MainForm()
    {
        SuspendLayout();

        Text = $"{AppInfo.name} - v{AppInfo.version}";
        Icon = Properties.Resources.Icon;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.Manual;
        Margin = Padding.Empty;

        SetupBlazorWebView();

        Opacity = 0;

        ResumeLayout(true);

        SetupFileWatcher();
    }

    void SetupBlazorWebView()
    {
        blazorWebView = new BlazorWebView
        {
            HostPage = AppInfo.hostPage,
            Location = Point.Empty,
            Margin = Padding.Empty,
            Dock = DockStyle.Fill
        };

        blazorWebView.WebView.DefaultBackgroundColor = Color.Transparent;

        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif

        blazorWebView.Services = services.BuildServiceProvider();

        blazorWebView.RootComponents.Add<Web.Root>("#root");

        Controls.Add(blazorWebView);
    }

    void SetupFileWatcher()
    {
        fileWatcher = new(".");
        fileWatcher.Created += (_, e) =>
        {
            if (e.Name == nameof(SingleInstance.Show))
                Invoke(() => SingleInstance.Show(Handle));
        };
        fileWatcher.EnableRaisingEvents = true;
    }

    public void OnAfterFirstRender()
    {
        RefreshWindow();

        Opacity = 1;

        Activate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fileWatcher.Dispose();

            var config = Config.Instance;

            if (WindowState == FormWindowState.Maximized)
            {
                config.maximized = true;
            }
            else
            {
                config.bounds = Bounds;
                config.maximized = false;
            }
        }

        base.Dispose(disposing);
    }

    public void RefreshWindow()
    {
        Config.Instance.SetupBounds();
        RefreshWindowInternal();
    }

    public void ResetWindow()
    {
        Config.Instance.ResetBounds();
        RefreshWindowInternal();
    }

    void RefreshWindowInternal()
    {
        RefreshWindowInternal(zoomFactor: 1, Config.Instance.bounds, Config.Instance.maximized);
    }

    void RefreshWindowInternal(float zoomFactor, Rectangle bounds, bool maximized)
    {
        WindowState = maximized ? FormWindowState.Maximized : FormWindowState.Normal;

        blazorWebView.WebView.ZoomFactor = zoomFactor;

        Point location;
        Size size;

        var targetLocation = bounds.Location;

        if (targetLocation.IsEmpty || targetLocation.X <= -16000 || targetLocation.Y <= -16000)
        {
            var dpiFactor = blazorWebView.WebView.DeviceDpi / 96f;
            size = (bounds.Size * zoomFactor * dpiFactor).ToSize();

            var screenSize = Screen.PrimaryScreen.Bounds.Size;
            location = (Point) ((screenSize - size) / 2);
        }
        else
        {
            location = bounds.Location;
            size = bounds.Size;
        }

        if (Size != size || Location != location)
        {
            Size = size;
            Location = location;
        }
    }

#if RELEASE
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true;
        WindowState = FormWindowState.Minimized;
        Hide();
    }
#endif

    //protected override void WndProc(ref Message message)
    //{
    //    SingleInstance.ProcessWindow(Handle, message.Msg);
    //    base.WndProc(ref message);
    //}
}
