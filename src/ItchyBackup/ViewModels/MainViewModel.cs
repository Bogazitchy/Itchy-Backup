using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItchyBackup.Models;
using ItchyBackup.Services;
using System.Linq;

namespace ItchyBackup.ViewModels;

public enum ActivePanel { Backup, History, Scheduler, Settings, Restore, Result }

public partial class MainViewModel : ObservableObject
{
    public record IncrementalBaseOption(string Label, string? FolderPath);

    public ObservableCollection<BackupCategory> Categories { get; } = new();
    public ObservableCollection<BackupProfile> SavedProfiles { get; } = new();
    public ObservableCollection<HotBackupWarning> HotWarnings { get; } = new();
    public ObservableCollection<HistoryEntry> BackupHistory { get; } = new();
    public ObservableCollection<IncrementalBaseOption> IncrementalBaseOptions { get; } = new();
    public ObservableCollection<string> AdditionalDestinations { get; } = new();
    public ObservableCollection<PreflightCheckItem> PreflightChecks { get; } = new();
    public ObservableCollection<BackupPreset> BackupPresets { get; } = new();
    public ObservableCollection<CompareEntry> CompareEntries { get; } = new();

    // Panel navigasyon
    [ObservableProperty] private ActivePanel _activePanel = ActivePanel.Backup;
    public bool IsPanelBackup    => ActivePanel == ActivePanel.Backup;
    public bool IsPanelHistory   => ActivePanel == ActivePanel.History;
    public bool IsPanelScheduler => ActivePanel == ActivePanel.Scheduler;
    public bool IsPanelSettings  => ActivePanel == ActivePanel.Settings;
    public bool IsPanelRestore   => ActivePanel == ActivePanel.Restore;
    public bool IsPanelResult    => ActivePanel == ActivePanel.Result;

    partial void OnActivePanelChanged(ActivePanel value)
    {
        OnPropertyChanged(nameof(IsPanelBackup));
        OnPropertyChanged(nameof(IsPanelHistory));
        OnPropertyChanged(nameof(IsPanelScheduler));
        OnPropertyChanged(nameof(IsPanelSettings));
        OnPropertyChanged(nameof(IsPanelRestore));
        OnPropertyChanged(nameof(IsPanelResult));
        if (value == ActivePanel.History) LoadBackupHistory();
        if (value == ActivePanel.Restore) LoadAvailableBackups();
    }

    // Yedek seçenekleri
    [ObservableProperty] private string _destinationPath = "";
    [ObservableProperty] private bool _useZip = false;
    [ObservableProperty] private bool _usePassword = false;
    [ObservableProperty] private string _zipPassword = "";
    [ObservableProperty] private bool _useVss = true;
    [ObservableProperty] private bool _verifyChecksum = true;
    [ObservableProperty] private CompressionLevel _compressionLevel = CompressionLevel.Normal;
    [ObservableProperty] private bool _isBackingUp = false;
    [ObservableProperty] private bool _showProgressPanel = false;

    // Artımlı yedekleme
    [ObservableProperty] private bool _isIncremental = false;
    [ObservableProperty] private IncrementalBaseOption? _selectedIncrementalBase;

    // Ağ paylaşımı
    [ObservableProperty] private bool _useNetworkCredentials = false;
    [ObservableProperty] private string _networkUsername = "";
    [ObservableProperty] private string _networkPassword = "";
    [ObservableProperty] private string _networkDomain = "";

    // Yedek rotasyonu
    [ObservableProperty] private int _rotationPolicyIndex = 0;
    [ObservableProperty] private int _rotationKeepLastN = 5;
    [ObservableProperty] private int _rotationDeleteOlderThanDays = 30;

    partial void OnRotationPolicyIndexChanged(int value)
    {
        OnPropertyChanged(nameof(RotationIsKeepLastN));
        OnPropertyChanged(nameof(RotationIsDeleteOlderThan));
    }

    public bool RotationIsKeepLastN       => RotationPolicyIndex == 1;
    public bool RotationIsDeleteOlderThan => RotationPolicyIndex == 2;

    public bool IsNetworkPath => NetworkShareHelper.IsUncPath(DestinationPath);

    // Progress
    [ObservableProperty] private double _progressPercent = 0;
    [ObservableProperty] private string _progressText = "Hazır";
    [ObservableProperty] private string _currentFile = "";
    [ObservableProperty] private string _currentCategory = "";
    [ObservableProperty] private string _speedText = "";
    [ObservableProperty] private string _etaText = "--";
    [ObservableProperty] private string _selectedSummary = "Hiç öğe seçilmedi";
    [ObservableProperty] private string _selectedSizeText = "";
    [ObservableProperty] private string _fileCountText = "";
    [ObservableProperty] private string _preflightSummary = "Kontrol bekleniyor";
    [ObservableProperty] private bool _preflightHasErrors = false;
    [ObservableProperty] private AvailableBackup? _compareBackupA;
    [ObservableProperty] private AvailableBackup? _compareBackupB;
    [ObservableProperty] private string _compareSummary = "İki yedek seçip karşılaştırabilirsiniz.";
    [ObservableProperty] private bool _isComparingBackups = false;

    // Profil düzenleme modu
    [ObservableProperty] private BackupProfile? _editingProfile;

    partial void OnEditingProfileChanged(BackupProfile? value)
        => OnPropertyChanged(nameof(IsEditingProfile));

    public bool IsEditingProfile => EditingProfile != null;

    // Yedek sonucu
    [ObservableProperty] private bool _showResultReport = false;
    [ObservableProperty] private BackupResult? _lastBackupResult;

    // Ayarlar
    [ObservableProperty] private bool _startWithWindows = false;
    [ObservableProperty] private bool _minimizeToTray = false;
    [ObservableProperty] private bool _autoChecksum = true;
    [ObservableProperty] private bool _soundNotification = true;
    [ObservableProperty] private bool _openFolderAfterBackup = false;
    [ObservableProperty] private string _defaultDestination = "";
    [ObservableProperty] private string _themeName = "Dark";
    [ObservableProperty] private string _accentColor = "#007A4D";
    [ObservableProperty] private bool _enableWebhookNotifications = false;
    [ObservableProperty] private string _webhookUrl = "";
    [ObservableProperty] private bool _enableEmailNotifications = false;
    [ObservableProperty] private string _smtpHost = "mail.itchy.com.tr";
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private bool _smtpSsl = true;
    [ObservableProperty] private string _smtpUsername = "info@itchy.com.tr";
    [ObservableProperty] private string _smtpPassword = "";
    [ObservableProperty] private string _emailFrom = "info@itchy.com.tr";
    [ObservableProperty] private string _emailTo = "";

    private bool _suppressThemeApply;

    public bool IsDarkTheme => ThemeName == "Dark";

    partial void OnThemeNameChanged(string value)
    {
        if (!_suppressThemeApply)
        {
            ThemeService.Apply(value, AccentColor);
            SaveSettingsSilent();
        }
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    partial void OnAccentColorChanged(string value)
    {
        if (!_suppressThemeApply) ThemeService.Apply(ThemeName, value);
    }

    [RelayCommand]
    public void SetTheme(string theme) => ThemeName = theme;

    [RelayCommand]
    public void SetAccent(string hex) => AccentColor = hex;

    // Zamanlayıcı
    [ObservableProperty] private bool _schedulerEnabled = false;
    [ObservableProperty] private string _schedulerTime = "02:00";
    [ObservableProperty] private int _schedulerHour = 2;
    [ObservableProperty] private int _schedulerMinute = 0;
    [ObservableProperty] private string _schedulerProfile = "";
    public IReadOnlyList<int> SchedulerHours { get; } = Enumerable.Range(0, 24).ToList();
    public IReadOnlyList<int> SchedulerMinutes { get; } = Enumerable.Range(0, 60).ToList();

    partial void OnSchedulerHourChanged(int value)
        => SchedulerTime = $"{value:D2}:{SchedulerMinute:D2}";

    partial void OnSchedulerMinuteChanged(int value)
        => SchedulerTime = $"{SchedulerHour:D2}:{value:D2}";

    [RelayCommand] public void IncrementHour()   => SchedulerHour   = (SchedulerHour   + 1) % 24;
    [RelayCommand] public void DecrementHour()   => SchedulerHour   = (SchedulerHour   + 23) % 24;
    [RelayCommand] public void IncrementMinute() => SchedulerMinute = (SchedulerMinute + 1) % 60;
    [RelayCommand] public void DecrementMinute() => SchedulerMinute = (SchedulerMinute + 59) % 60;
    [ObservableProperty] private bool _schedMon = false;
    [ObservableProperty] private bool _schedulerTue = false;
    [ObservableProperty] private bool _schedWed = false;
    [ObservableProperty] private bool _schedThu = false;
    [ObservableProperty] private bool _schedFri = false;
    [ObservableProperty] private bool _schedSat = false;
    [ObservableProperty] private bool _schedSun = false;
    [ObservableProperty] private string _schedulerStatus = "Zamanlayıcı kapalı";

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _sizeEstimateCts;

    partial void OnDestinationPathChanged(string value)
    {
        OnPropertyChanged(nameof(IsNetworkPath));
        if (!NetworkShareHelper.IsUncPath(value))
            UseNetworkCredentials = false;
        if (IsIncremental) UpdateIncrementalBaseOptions();
    }

    partial void OnIsIncrementalChanged(bool value)
    {
        if (value) UpdateIncrementalBaseOptions();
        else IncrementalBaseOptions.Clear();
    }

    private void UpdateIncrementalBaseOptions()
    {
        IncrementalBaseOptions.Clear();
        IncrementalBaseOptions.Add(new IncrementalBaseOption("Otomatik — en son yedek", null));

        if (!string.IsNullOrEmpty(DestinationPath) && Directory.Exists(DestinationPath))
        {
            var machine = string.Concat(Environment.MachineName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var user = string.Concat(Environment.UserName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var folders = Directory.GetDirectories(DestinationPath, $"Yedek_{machine}_{user}_*")
                .OrderByDescending(d => d);
            foreach (var f in folders)
                IncrementalBaseOptions.Add(new IncrementalBaseOption(Path.GetFileName(f), f));
        }

        SelectedIncrementalBase = null;
    }

    public MainViewModel()
    {
        ProfileService.EnsureDefaultProfiles();
        LoadCategories();
        LoadPresetTemplates();
        LoadCustomFolders();
        LoadProfiles();
        LoadLastDestination();
        LoadSettings();
    }

    // ── Navigasyon ──────────────────────────────────────────────────────────
    [RelayCommand] public void OpenBackup()    { ActivePanel = ActivePanel.Backup; }
    [RelayCommand] public void OpenHistory()   { ActivePanel = ActivePanel.History; }
    [RelayCommand] public void OpenScheduler() { ActivePanel = ActivePanel.Scheduler; }
    [RelayCommand] public void OpenSettings()  { ActivePanel = ActivePanel.Settings; }
    [RelayCommand] public void OpenRestore()   { ActivePanel = ActivePanel.Restore; }
    [RelayCommand] public void CloseResult()   { ShowResultReport = false; ActivePanel = ActivePanel.Backup; }

    // ── Kategori yükleme ────────────────────────────────────────────────────
    private void LoadCategories()
    {
        var cats = CategoryBuilder.BuildAll();
        foreach (var c in cats)
        {
            foreach (var item in c.Items)
                item.PropertyChanged += (_, _) => UpdateSummary();
            Categories.Add(c);
        }
    }

    private void LoadProfiles()
    {
        SavedProfiles.Clear();
        foreach (var p in ProfileService.LoadAll())
            SavedProfiles.Add(p);
    }

    private void LoadPresetTemplates()
    {
        BackupPresets.Clear();
        BackupPresets.Add(new BackupPreset
        {
            Name = "Standart Servis",
            Description = "Masaüstü, belgeler, indirilenler, tarayıcılar ve WiFi profilleri.",
            Icon = "🧰",
            ItemIds = new() { "desktop", "documents", "downloads", "pictures", "chrome", "firefox", "edge", "wifiProfiles" }
        });
        BackupPresets.Add(new BackupPreset
        {
            Name = "Muhasebe PC",
            Description = "Belgeler, masaüstü, Outlook ve veritabanı odaklı seçim.",
            Icon = "₺",
            ItemIds = new() { "desktop", "documents", "outlookPst", "outlookOst", "firebird", "sqlite", "sqlserver", "access" }
        });
        BackupPresets.Add(new BackupPreset
        {
            Name = "Tarayıcı Kurtarma",
            Description = "Sık kullanılan tarayıcı profilleri ve bulut klasörleri.",
            Icon = "🌐",
            ItemIds = new() { "chrome", "firefox", "edge", "opera", "brave", "vivaldi", "onedrive", "googledrive", "dropbox" }
        });
        BackupPresets.Add(new BackupPreset
        {
            Name = "Tam Kullanıcı",
            Description = "Kullanıcı klasörleri, AppData, tarayıcılar ve bulut verileri.",
            Icon = "👤",
            ItemIds = new() { "desktop", "documents", "downloads", "pictures", "videos", "music", "appdata", "chrome", "firefox", "edge", "onedrive", "googledrive", "dropbox" }
        });
    }

    private void LoadLastDestination()
    {
        var p = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ItchyBackup", "last_dest.txt");
        if (File.Exists(p)) DestinationPath = File.ReadAllText(p).Trim();
    }

    private void SaveLastDestination()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ItchyBackup");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "last_dest.txt"), DestinationPath);
    }

    // ── Yedek seçim komutları ───────────────────────────────────────────────
    [RelayCommand]
    public void SelectAll()
    {
        foreach (var cat in Categories) cat.SetAllSelected(true);
        UpdateSummary();
    }

    [RelayCommand]
    public void ClearAll()
    {
        foreach (var cat in Categories) cat.SetAllSelected(false);
        UpdateSummary();
    }

    [RelayCommand]
    public void ApplyPreset(BackupPreset preset)
    {
        var ids = preset.ItemIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in Categories)
            foreach (var item in cat.Items)
                item.IsSelected = ids.Contains(item.Id);
        UpdateSummary();
        PreflightSummary = $"{preset.Name} şablonu uygulandı";
    }

    [RelayCommand]
    public async Task RunPreflightAsync()
    {
        await RefreshPreflightAsync();
    }

    [RelayCommand]
    public void BrowseDestination()
    {
        using var d = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Yedek hedef klasörünü seçin",
            UseDescriptionForTitle = true,
            SelectedPath = DestinationPath
        };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DestinationPath = d.SelectedPath;
            SaveLastDestination();
        }
    }

    [RelayCommand]
    public void AddDestination()
    {
        using var d = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Ek yedek hedef klasörünü seçin",
            UseDescriptionForTitle = true
        };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            AdditionalDestinations.Add(d.SelectedPath);
    }

    [RelayCommand]
    public void RemoveDestination(string dest) => AdditionalDestinations.Remove(dest);

    // ── Yedekleme ───────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task StartBackupAsync()
    {
        if (string.IsNullOrWhiteSpace(DestinationPath))
        {
            System.Windows.MessageBox.Show("Lütfen yedek hedef klasörünü seçin.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var selected = Categories.SelectMany(c => c.Items.Where(i => i.IsSelected)).ToList();
        if (!selected.Any())
        {
            System.Windows.MessageBox.Show("Lütfen en az bir öğe seçin.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (UsePassword && string.IsNullOrWhiteSpace(ZipPassword))
        {
            System.Windows.MessageBox.Show("AES-256 için şifre girmeniz gerekiyor.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        await RefreshPreflightAsync();
        if (PreflightHasErrors)
        {
            System.Windows.MessageBox.Show("Yedek öncesi kontrollerde kritik hata var. Lütfen kırmızı maddeleri düzeltin.",
                "Yedek Öncesi Kontrol", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Çalışan servis kontrolü
        var warnings = HotBackupDetector.DetectRunningServices(selected.Select(i => i.Id));
        if (warnings.Any())
        {
            var hotMsg = string.Join("\n\n", warnings.Select(w => $"⚠ {w.Message}"));
            var rh = System.Windows.MessageBox.Show(
                $"Çalışan servisler tespit edildi:\n\n{hotMsg}\n\nDevam edilsin mi?",
                "Hot Backup Uyarısı",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (rh != System.Windows.MessageBoxResult.Yes) return;
        }

        // Disk alanı kontrolü
        ShowProgressPanel = true;
        ProgressText = "Disk alanı kontrol ediliyor...";
        var estSize = await DiskSpaceChecker.EstimateBackupSize(selected);
        var spaceCheck = DiskSpaceChecker.CheckSpace(DestinationPath, estSize);

        if (!spaceCheck.IsSufficient && !spaceCheck.IsNetwork)
        {
            ShowProgressPanel = false;
            var rs = System.Windows.MessageBox.Show(
                $"Disk alanı yetersiz!\n\n" +
                $"Gerekli: {spaceCheck.RequiredText}\n" +
                $"Mevcut: {spaceCheck.AvailableText}\n\n" +
                $"{spaceCheck.Message}\n\nYine de devam edilsin mi?",
                "Disk Alanı Uyarısı",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (rs != System.Windows.MessageBoxResult.Yes) return;
            ShowProgressPanel = true;
        }

        IsBackingUp = true;
        ProgressPercent = 0;
        ProgressText = "Başlıyor...";
        CurrentFile = "";
        CurrentCategory = "";
        EtaText = "--";
        SpeedText = "";
        FileCountText = "";
        _cts = new CancellationTokenSource();

        var options = new BackupOptions
        {
            DestinationRoot          = DestinationPath,
            AdditionalDestinations   = AdditionalDestinations.ToList(),
            UseZip                   = UseZip,
            UsePassword              = UsePassword,
            Password                 = ZipPassword,
            UseVss                   = UseVss,
            VerifyChecksum           = VerifyChecksum,
            CompressionLevel         = CompressionLevel,
            SelectedItems            = selected,
            IncludeMachineInfo       = true,
            IsIncremental            = IsIncremental,
            IncrementalBaseFolder    = SelectedIncrementalBase?.FolderPath ?? "",
            UseNetworkCredentials    = UseNetworkCredentials && NetworkShareHelper.IsUncPath(DestinationPath),
            NetworkUsername          = NetworkUsername,
            NetworkPassword          = NetworkPassword,
            NetworkDomain            = NetworkDomain,
            RotationPolicy           = (RotationPolicy)RotationPolicyIndex,
            RotationKeepLastN        = RotationKeepLastN,
            RotationDeleteOlderThanDays = RotationDeleteOlderThanDays,
            ParallelCopyThreads      = 4,
        };

        var progress = new Progress<BackupProgress>(p =>
        {
            ProgressPercent  = p.PercentComplete;
            ProgressText     = $"{p.CompletedItems}/{p.TotalItems} kategori";
            CurrentFile      = p.CurrentFile;
            CurrentCategory  = p.CurrentCategory;
            SpeedText        = p.SpeedMBps > 0 ? p.SpeedText : "";
            EtaText          = p.EstimatedText;
            FileCountText    = p.FileCountText;
        });

        bool wasManualBackup = true;

        try
        {
            var engine = new BackupEngine(options, progress, _cts.Token);
            LastBackupResult = await engine.RunAsync();
            ProgressPercent = 100;
            ProgressText    = $"Tamamlandı! ({selected.Count} kategori)";
            SaveLastDestination();
            ShowResultReport = true;
            ActivePanel = ActivePanel.Result;
            await NotificationService.SendExternalAsync(BuildNotificationOptions(),
                "Itchy Backup tamamlandı",
                $"{selected.Count} kategori, {LastBackupResult.FilesCopied} dosya, {LastBackupResult.Errors.Count} hata. Rapor: {LastBackupResult.ReportPath}");
        }
        catch (OperationCanceledException)
        {
            ProgressText = "İptal edildi.";
            ProgressPercent = 0;
        }
        catch (Exception ex)
        {
            ProgressText = $"Hata: {ex.Message}";
            System.Windows.MessageBox.Show($"Yedekleme hatası:\n{ex.Message}",
                "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBackingUp = false;
            _cts?.Dispose();
        }

        // Manuel yedek tamamlandıysa ve ayar açıksa klasörü aç
        if (wasManualBackup && OpenFolderAfterBackup && LastBackupResult != null
            && Directory.Exists(LastBackupResult.BackupPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", LastBackupResult.BackupPath);
        }
    }

    private async Task RefreshPreflightAsync()
    {
        var selected = Categories.SelectMany(c => c.Items.Where(i => i.IsSelected)).ToList();
        PreflightChecks.Clear();
        var checks = await BackupPreflightService.RunAsync(
            DestinationPath,
            selected,
            UseVss,
            UseZip,
            UsePassword,
            IsIncremental);

        foreach (var check in checks)
            PreflightChecks.Add(check);

        var errors = checks.Count(c => c.Status == PreflightStatus.Error);
        var warnings = checks.Count(c => c.Status == PreflightStatus.Warning);
        PreflightHasErrors = errors > 0;
        PreflightSummary = $"{checks.Count} kontrol • {errors} hata • {warnings} uyarı";
    }

    private NotificationOptions BuildNotificationOptions() => new()
    {
        EnableWebhook = EnableWebhookNotifications,
        WebhookUrl = WebhookUrl,
        EnableEmail = EnableEmailNotifications,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpSsl = SmtpSsl,
        SmtpUsername = SmtpUsername,
        SmtpPassword = SmtpPassword,
        EmailFrom = EmailFrom,
        EmailTo = EmailTo
    };

    [RelayCommand] public void CancelBackup() => _cts?.Cancel();

    [RelayCommand]
    public void SaveProfile()
    {
        var dialog = new Views.SaveProfileDialog(EditingProfile?.ProfileName, EditingProfile?.Icon)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        var oldName = EditingProfile?.ProfileName;
        var createdAt = EditingProfile?.CreatedAt ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var profile = new BackupProfile
        {
            ProfileName              = dialog.ProfileName,
            Icon                     = dialog.SelectedIcon,
            CreatedAt                = createdAt,
            DefaultDestination       = DestinationPath,
            AdditionalDestinations   = AdditionalDestinations.ToList(),
            UseZip                   = UseZip,
            UsePassword              = UsePassword,
            UseVss                   = UseVss,
            VerifyChecksum           = VerifyChecksum,
            CompressionLevel         = CompressionLevel,
            SelectedItemIds          = Categories.SelectMany(c => c.Items.Where(i => i.IsSelected).Select(i => i.Id)).ToList(),
            IsIncremental            = IsIncremental,
            UseNetworkCredentials    = UseNetworkCredentials,
            NetworkUsername          = NetworkUsername,
            NetworkDomain            = NetworkDomain,
            RotationPolicy           = (RotationPolicy)RotationPolicyIndex,
            RotationKeepLastN        = RotationKeepLastN,
            RotationDeleteOlderThanDays = RotationDeleteOlderThanDays,
        };

        if (oldName != null && oldName != dialog.ProfileName)
            ProfileService.Delete(oldName);

        ProfileService.Save(profile);
        EditingProfile = null;
        LoadProfiles();
    }

    [RelayCommand]
    public void EditProfile(BackupProfile profile)
    {
        LoadProfile(profile);
        EditingProfile = profile;
        ActivePanel = ActivePanel.Backup;
    }

    [RelayCommand]
    public void CancelEditProfile()
    {
        EditingProfile = null;
    }

    [RelayCommand]
    public void LoadProfile(BackupProfile profile)
    {
        DestinationPath       = profile.DefaultDestination;
        UseZip                = profile.UseZip;
        UsePassword           = profile.UsePassword;
        UseVss                = profile.UseVss;
        VerifyChecksum        = profile.VerifyChecksum;
        CompressionLevel      = profile.CompressionLevel;
        IsIncremental         = profile.IsIncremental;
        UseNetworkCredentials = profile.UseNetworkCredentials;
        NetworkUsername       = profile.NetworkUsername;
        NetworkDomain         = profile.NetworkDomain;
        NetworkPassword       = "";
        RotationPolicyIndex   = (int)profile.RotationPolicy;
        RotationKeepLastN     = profile.RotationKeepLastN;
        RotationDeleteOlderThanDays = profile.RotationDeleteOlderThanDays;
        AdditionalDestinations.Clear();
        foreach (var d in profile.AdditionalDestinations)
            AdditionalDestinations.Add(d);
        var ids = new HashSet<string>(profile.SelectedItemIds);
        foreach (var cat in Categories)
            foreach (var item in cat.Items)
                item.IsSelected = ids.Contains(item.Id);
        UpdateSummary();
    }

    [RelayCommand]
    public void DeleteProfile(BackupProfile profile)
    {
        var r = System.Windows.MessageBox.Show(
            $"'{profile.ProfileName}' profilini silmek istediğinizden emin misiniz?",
            "Profil Sil",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (r != System.Windows.MessageBoxResult.Yes) return;
        ProfileService.Delete(profile.ProfileName);
        LoadProfiles();
    }

    [RelayCommand]
    public void ExportProfile(BackupProfile profile)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Profili dışa aktar",
            FileName = $"{profile.ProfileName}.itchyprofile.json",
            Filter = "Itchy Backup Profili (*.itchyprofile.json)|*.itchyprofile.json|JSON (*.json)|*.json"
        };
        if (dialog.ShowDialog() != true) return;

        ProfileService.Export(profile, dialog.FileName);
        System.Windows.MessageBox.Show("Profil dışa aktarıldı.", "Itchy Backup",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    public void ImportProfile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Profil içe aktar",
            Filter = "Itchy Backup Profili (*.itchyprofile.json;*.json)|*.itchyprofile.json;*.json|Tüm dosyalar (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        var profile = ProfileService.Import(dialog.FileName);
        LoadProfiles();
        System.Windows.MessageBox.Show($"Profil içe aktarıldı: {profile.ProfileName}", "Itchy Backup",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    [RelayCommand]
    public void OpenHistoryFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (Directory.Exists(path))
            System.Diagnostics.Process.Start("explorer.exe", path);
        else
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                System.Diagnostics.Process.Start("explorer.exe", parent);
        }
    }

    [RelayCommand]
    public void OpenLogFolder()
    {
        var p = LogService.GetLogPath();
        if (!string.IsNullOrEmpty(p) && File.Exists(p))
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{p}\"");
        else
            System.Diagnostics.Process.Start("explorer.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ItchyBackup", "Logs"));
    }

    [RelayCommand]
    public void OpenBackupReport()
    {
        var path = LastBackupResult?.ReportPath;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    [RelayCommand]
    public async Task VerifyHistoryBackupAsync(HistoryEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.DestinationPath) || !Directory.Exists(entry.DestinationPath))
        {
            System.Windows.MessageBox.Show("Doğrulanacak yedek klasörü bulunamadı.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            var report = await ChecksumService.VerifyManifestAsync(entry.DestinationPath, null, CancellationToken.None);
            System.Windows.MessageBox.Show(report.Summary, "Yedek Doğrulama",
                System.Windows.MessageBoxButton.OK,
                report.IsAllValid ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Doğrulama hatası:\n{ex.Message}", "Hata",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task CompareSelectedBackupsAsync()
    {
        if (CompareBackupA == null || CompareBackupB == null)
        {
            System.Windows.MessageBox.Show("Karşılaştırmak için iki yedek seçin.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var pathA = Directory.Exists(CompareBackupA.Path) ? CompareBackupA.Path : Path.GetDirectoryName(CompareBackupA.Path) ?? "";
        var pathB = Directory.Exists(CompareBackupB.Path) ? CompareBackupB.Path : Path.GetDirectoryName(CompareBackupB.Path) ?? "";
        CompareEntries.Clear();
        IsComparingBackups = true;
        CompareSummary = "Karşılaştırılıyor...";

        try
        {
            var report = await BackupCompareService.CompareAsync(pathA, pathB, new Progress<string>(s => CompareSummary = s), CancellationToken.None);
            CompareSummary = report.Summary;
            foreach (var entry in report.Modified.Concat(report.OnlyInA).Concat(report.OnlyInB).Take(100))
                CompareEntries.Add(entry);
        }
        catch (Exception ex)
        {
            CompareSummary = $"Karşılaştırma hatası: {ex.Message}";
        }
        finally
        {
            IsComparingBackups = false;
        }
    }

    // ── Geçmiş ──────────────────────────────────────────────────────────────
    private void LoadBackupHistory()
    {
        BackupHistory.Clear();
        LoadAvailableBackups();
        if (string.IsNullOrEmpty(DestinationPath) || !Directory.Exists(DestinationPath)) return;
        var dirs = Directory.GetDirectories(DestinationPath, "Yedek_*")
            .OrderByDescending(d => d).Take(50);
        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            var logFile = Directory.GetFiles(dir, "backup_log_*.txt").FirstOrDefault();
            bool hasErrors = false;
            string summary = "Detay yok";
            if (logFile != null)
            {
                var lines = File.ReadAllLines(logFile);
                hasErrors = lines.Any(l => l.Contains("[ERROR]"));
                var okCount = lines.Count(l => l.Contains("OK:"));
                summary = $"{okCount} kategori yedeklendi";
            }
            BackupHistory.Add(new HistoryEntry
            {
                FolderName = name,
                Date = name.Replace("Yedek_", "").Replace("_", " "),
                Summary = summary,
                HasErrors = hasErrors,
                DestinationPath = dir
            });
        }
    }

    // ── Zamanlayıcı ─────────────────────────────────────────────────────────
    [RelayCommand]
    public void SaveScheduler()
    {
        if (!SchedulerEnabled)
        {
            RemoveScheduledTask();
            SchedulerStatus = "Zamanlayıcı kapalı";
            return;
        }

        var days = new List<string>();
        if (SchedMon) days.Add("MON");
        if (SchedulerTue) days.Add("TUE");
        if (SchedWed) days.Add("WED");
        if (SchedThu) days.Add("THU");
        if (SchedFri) days.Add("FRI");
        if (SchedSat) days.Add("SAT");
        if (SchedSun) days.Add("SUN");

        if (!days.Any())
        {
            System.Windows.MessageBox.Show("En az bir gün seçin.", "Zamanlayıcı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(SchedulerProfile))
        {
            System.Windows.MessageBox.Show("Bir profil seçin.", "Zamanlayıcı",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        CreateScheduledTask(days, SchedulerTime, SchedulerProfile);
        SchedulerStatus = $"Aktif — {string.Join(", ", days)} saat {SchedulerTime}";
        System.Windows.MessageBox.Show(
            $"Zamanlayıcı oluşturuldu!\n\nGünler: {string.Join(", ", days)}\nSaat: {SchedulerTime}\nProfil: {SchedulerProfile}",
            "Zamanlayıcı", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void CreateScheduledTask(List<string> days, string time, string profile)
    {
        try
        {
            var exePath = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, "ItchyBackup.exe");
            var parts = time.Split(':');
            var hour = parts[0];
            var minute = parts.Length > 1 ? parts[1] : "00";
            var timeStr = $"{hour}:{minute}";

            foreach (var day in days)
            {
                var taskName = $"ItchyBackup_{day}";
                var args = $"/create /f /tn \"{taskName}\" /tr \"\\\"{exePath}\\\" --autobackup \\\"{profile}\\\"\" /sc weekly /d {day} /st {timeStr} /rl HIGHEST";
                var psi = new System.Diagnostics.ProcessStartInfo("schtasks", args)
                {
                    CreateNoWindow = true, UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi)?.WaitForExit();
            }
            LogService.Info($"Zamanlayıcı oluşturuldu: {string.Join(",", days)} {timeStr}");
        }
        catch (Exception ex)
        {
            LogService.Error("Zamanlayıcı oluşturulamadı", ex);
            System.Windows.MessageBox.Show($"Zamanlayıcı oluşturulamadı:\n{ex.Message}",
                "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void RemoveScheduledTask()
    {
        try
        {
            foreach (var day in new[] { "MON","TUE","WED","THU","FRI","SAT","SUN" })
            {
                var args = $"/delete /f /tn \"ItchyBackup_{day}\"";
                var psi = new System.Diagnostics.ProcessStartInfo("schtasks", args)
                { CreateNoWindow = true, UseShellExecute = false };
                System.Diagnostics.Process.Start(psi)?.WaitForExit();
            }
        }
        catch { }
    }

    // ── Ayarlar ─────────────────────────────────────────────────────────────
    [RelayCommand]
    public void SaveSettings()
    {
        var vm = new SettingsViewModel
        {
            StartWithWindows      = StartWithWindows,
            MinimizeToTray        = MinimizeToTray,
            AutoChecksum          = AutoChecksum,
            SoundNotification     = SoundNotification,
            OpenFolderAfterBackup = OpenFolderAfterBackup,
            DefaultDestination    = DefaultDestination,
            ThemeName             = ThemeName,
            AccentColor           = AccentColor,
            EnableWebhookNotifications = EnableWebhookNotifications,
            WebhookUrl = WebhookUrl,
            EnableEmailNotifications = EnableEmailNotifications,
            SmtpHost = SmtpHost,
            SmtpPort = SmtpPort,
            SmtpSsl = SmtpSsl,
            SmtpUsername = SmtpUsername,
            SmtpPassword = SmtpPassword,
            EmailFrom = EmailFrom,
            EmailTo = EmailTo,
        };
        vm.Save();
        StartupService.SetEnabled(StartWithWindows);
        if (!string.IsNullOrEmpty(DefaultDestination) && string.IsNullOrEmpty(DestinationPath))
            DestinationPath = DefaultDestination;
        System.Windows.MessageBox.Show("Ayarlar kaydedildi.", "Itchy Backup",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void SaveSettingsSilent()
    {
        var vm = new SettingsViewModel
        {
            StartWithWindows      = StartWithWindows,
            MinimizeToTray        = MinimizeToTray,
            AutoChecksum          = AutoChecksum,
            SoundNotification     = SoundNotification,
            OpenFolderAfterBackup = OpenFolderAfterBackup,
            DefaultDestination    = DefaultDestination,
            ThemeName             = ThemeName,
            AccentColor           = AccentColor,
            EnableWebhookNotifications = EnableWebhookNotifications,
            WebhookUrl = WebhookUrl,
            EnableEmailNotifications = EnableEmailNotifications,
            SmtpHost = SmtpHost,
            SmtpPort = SmtpPort,
            SmtpSsl = SmtpSsl,
            SmtpUsername = SmtpUsername,
            SmtpPassword = SmtpPassword,
            EmailFrom = EmailFrom,
            EmailTo = EmailTo,
        };
        vm.Save();
        StartupService.SetEnabled(StartWithWindows);
    }

    private void LoadSettings()
    {
        var vm = new SettingsViewModel();
        StartWithWindows      = vm.StartWithWindows;
        MinimizeToTray        = vm.MinimizeToTray;
        AutoChecksum          = vm.AutoChecksum;
        SoundNotification     = vm.SoundNotification;
        OpenFolderAfterBackup = vm.OpenFolderAfterBackup;
        DefaultDestination    = vm.DefaultDestination;
        EnableWebhookNotifications = vm.EnableWebhookNotifications;
        WebhookUrl = vm.WebhookUrl;
        EnableEmailNotifications = vm.EnableEmailNotifications;
        SmtpHost = vm.SmtpHost;
        SmtpPort = vm.SmtpPort;
        SmtpSsl = vm.SmtpSsl;
        SmtpUsername = vm.SmtpUsername;
        SmtpPassword = vm.SmtpPassword;
        EmailFrom = vm.EmailFrom;
        EmailTo = vm.EmailTo;
        _suppressThemeApply = true;
        ThemeName   = vm.ThemeName;
        AccentColor = vm.AccentColor == "#6C5CE7" ? "#007A4D" : vm.AccentColor;
        _suppressThemeApply = false;
        ThemeService.Apply(ThemeName, AccentColor);
        if (string.IsNullOrEmpty(DestinationPath) && !string.IsNullOrEmpty(DefaultDestination))
            DestinationPath = DefaultDestination;
    }

    [RelayCommand]
    public void BrowseDefaultDestination()
    {
        using var d = new System.Windows.Forms.FolderBrowserDialog
        { Description = "Varsayılan yedek klasörü", SelectedPath = DefaultDestination };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            DefaultDestination = d.SelectedPath;
    }

    [RelayCommand]
    public async Task TestNotificationsAsync()
    {
        var options = BuildNotificationOptions();
        const string title = "Itchy Backup test bildirimi";
        var message = $"Itchy Backup bildirim sistemi çalışıyor.\nBilgisayar: {Environment.MachineName}\nKullanıcı: {Environment.UserName}";

        if (EnableEmailNotifications)
        {
            var result = await NotificationService.TestEmailAsync(options, title, message);
            if (!result.Success)
            {
                System.Windows.MessageBox.Show(result.Message, "E-posta Gönderilemedi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
        }

        if (EnableWebhookNotifications && !string.IsNullOrWhiteSpace(WebhookUrl))
            await NotificationService.SendExternalAsync(options, title, message, includeEmail: false);

        NotificationService.ShowSuccess("Itchy Backup", "Test bildirimi gönderildi.");
        System.Windows.MessageBox.Show(
            EnableEmailNotifications
                ? $"Test e-postası {EmailTo} adresine info@itchy.com.tr üzerinden gönderildi."
                : "Etkin bildirim kanallarına test bildirimi gönderildi.",
            "Itchy Backup", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    // ── Yardımcı ────────────────────────────────────────────────────────────
    private void UpdateSummary()
    {
        var selected = Categories.SelectMany(c => c.Items).Where(i => i.IsSelected).ToList();
        SelectedSummary = selected.Count == 0 ? "Hiç öğe seçilmedi" : $"{selected.Count} öğe seçili";
        _ = EstimateSizeAsync(selected);
    }

    private async Task EstimateSizeAsync(List<BackupItem> selected)
    {
        _sizeEstimateCts?.Cancel();
        _sizeEstimateCts = new CancellationTokenSource();
        var ct = _sizeEstimateCts.Token;
        if (!selected.Any())
        {
            SelectedSizeText = "";
            return;
        }
        SelectedSizeText = "hesaplanıyor...";
        try
        {
            var bytes = await DiskSpaceChecker.EstimateBackupSize(selected, ct);
            if (!ct.IsCancellationRequested)
                SelectedSizeText = $"~{DiskSpaceChecker.FormatBytes(bytes)}";
        }
        catch (OperationCanceledException) { }
    }
}
