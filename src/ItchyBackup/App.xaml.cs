using ItchyBackup.Services;
using ItchyBackup.Views;
using ItchyBackup.ViewModels;
using System.IO;
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
        LogService.Info($"Itchy Backup başlatıldı. Sürüm={AppInfo.DisplayVersion}, OS={Environment.OSVersion}, User={Environment.UserName}, Args={string.Join(" ", e.Args)}");

        if (TryGetAutoBackupArgs(e.Args, out var profileName, out var destinationPath))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunAutoBackupAndShutdownAsync(profileName, destinationPath);
            return;
        }

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
        LogService.Info($"Itchy Backup kapatıldı. ExitCode={e.ApplicationExitCode}");
        base.OnExit(e);
    }

    private static bool TryGetAutoBackupArgs(string[] args, out string profileName, out string destinationPath)
    {
        profileName = "";
        destinationPath = "";

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--autobackup", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                profileName = args[++i];
            else if (string.Equals(args[i], "--destination", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                destinationPath = args[++i];
        }

        return !string.IsNullOrWhiteSpace(profileName);
    }

    private async Task RunAutoBackupAndShutdownAsync(string profileName, string destinationPath)
    {
        try
        {
            LogService.Info($"Zamanlayıcı yedeği başladı. Profil={profileName}, Hedef={destinationPath}");
            ProfileService.EnsureDefaultProfiles();
            var profile = ProfileService.Load(profileName)
                ?? throw new InvalidOperationException($"Profil bulunamadı: {profileName}");

            var selectedIds = new HashSet<string>(profile.SelectedItemIds, StringComparer.OrdinalIgnoreCase);
            var categories = CategoryBuilder.BuildAll();
            LoadCustomFoldersForAutoBackup(categories);
            var selectedItems = categories.SelectMany(c => c.Items)
                .Where(i => selectedIds.Contains(i.Id))
                .ToList();

            if (selectedItems.Count == 0)
                throw new InvalidOperationException($"Profilde yedeklenecek kaynak bulunamadı: {profileName}");

            var target = string.IsNullOrWhiteSpace(destinationPath)
                ? profile.DefaultDestination
                : destinationPath;
            if (string.IsNullOrWhiteSpace(target))
                throw new InvalidOperationException("Zamanlayıcı hedef klasörü boş.");

            Directory.CreateDirectory(target);

            var options = new BackupOptions
            {
                DestinationRoot = target,
                AdditionalDestinations = profile.AdditionalDestinations,
                UseZip = profile.UseZip,
                UsePassword = false,
                Password = null,
                UseVss = profile.UseVss,
                VerifyChecksum = profile.VerifyChecksum,
                CompressionLevel = profile.CompressionLevel,
                SelectedItems = selectedItems,
                IncludeMachineInfo = true,
                IsIncremental = profile.IsIncremental,
                UseNetworkCredentials = false,
                RotationPolicy = profile.RotationPolicy,
                RotationKeepLastN = profile.RotationKeepLastN,
                RotationDeleteOlderThanDays = profile.RotationDeleteOlderThanDays,
                ParallelCopyThreads = 4
            };

            var engine = new BackupEngine(options, null, CancellationToken.None);
            var result = await engine.RunAsync();
            LogService.Info($"Zamanlayıcı yedeği tamamlandı. Dosya={result.FilesCopied}, Hata={result.Errors.Count}, Rapor={result.ReportPath}");

            var settings = new SettingsViewModel();
            await NotificationService.SendExternalAsync(new NotificationOptions
            {
                EnableWebhook = settings.EnableWebhookNotifications,
                WebhookUrl = settings.WebhookUrl,
                EnableEmail = settings.EnableEmailNotifications,
                SmtpHost = settings.SmtpHost,
                SmtpPort = settings.SmtpPort,
                SmtpSsl = settings.SmtpSsl,
                SmtpUsername = settings.SmtpUsername,
                SmtpPassword = settings.SmtpPassword,
                EmailFrom = settings.EmailFrom,
                EmailTo = settings.EmailTo
            }, "Itchy Backup zamanlayıcı yedeği tamamlandı",
            $"{profileName} profili tamamlandı. Dosya: {result.FilesCopied}, hata: {result.Errors.Count}, rapor: {result.ReportPath}");
        }
        catch (Exception ex)
        {
            LogService.Error("Zamanlayıcı yedeği başarısız oldu.", ex);
        }
        finally
        {
            Shutdown();
        }
    }

    private static void LoadCustomFoldersForAutoBackup(List<ItchyBackup.Models.BackupCategory> categories)
    {
        try
        {
            var customCategory = categories.FirstOrDefault(c => c.Type == ItchyBackup.Models.CategoryType.CustomFolders);
            if (customCategory == null) return;

            var file = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ItchyBackup", "custom_folders.json");
            if (!File.Exists(file)) return;

            var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(File.ReadAllText(file));
            if (paths == null) return;

            foreach (var path in paths)
            {
                if (customCategory.Items.Any(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var item = CategoryBuilder.CreateCustomFolderItem(path);
                item.Parent = customCategory;
                customCategory.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            LogService.Warn($"Zamanlayıcı özel klasörleri yükleyemedi: {ex.Message}");
        }
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
