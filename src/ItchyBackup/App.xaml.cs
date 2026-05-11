using ItchyBackup.Services;
using ItchyBackup.Views;
using System.Windows;

namespace ItchyBackup;

public partial class App : System.Windows.Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    public bool IsQuitting { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LogService.Initialize();
        LogService.Info("Itchy Backup başlatıldı.");

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Ana pencereyi oluştur; MainViewModel yüklenir ve tema uygulanır
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;

        // Splash göster
        var splash = new SplashWindow();
        splash.Show();

        // Tepsi ikonunu kur
        SetupTrayIcon();

        // 1.8 saniye sonra splash'ı kapat ve ana pencereyi göster
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1800)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            splash.Close();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            mainWindow.Show();
        };
        timer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        LogService.Info("Itchy Backup kapatıldı.");
        base.OnExit(e);
    }

    private void SetupTrayIcon()
    {
        System.Drawing.Icon icon;
        try
        {
            var sri = GetResourceStream(new Uri("Resources/Icons/app.ico", UriKind.Relative));
            icon = sri != null ? new System.Drawing.Icon(sri.Stream) : System.Drawing.SystemIcons.Application;
        }
        catch { icon = System.Drawing.SystemIcons.Application; }

        var menu = new System.Windows.Forms.ContextMenuStrip();

        var openItem = new System.Windows.Forms.ToolStripMenuItem("Itchy Backup'ı Aç")
        {
            Font = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold)
        };
        openItem.Click += (_, _) => ShowMainWindow();

        var quitItem = new System.Windows.Forms.ToolStripMenuItem("Çıkış");
        quitItem.Click += (_, _) => QuitApp();

        menu.Items.Add(openItem);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(quitItem);

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon    = icon,
            Text    = "Itchy Backup",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null) return;
        MainWindow.Show();
        if (MainWindow.WindowState == WindowState.Minimized)
            MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    public void QuitApp()
    {
        IsQuitting = true;
        _trayIcon?.Dispose();
        _trayIcon = null;
        Shutdown();
    }
}
