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
    [ObservableProperty] private string _accentColor = "#6C5CE7";

    private bool _suppressThemeApply;

    partial void OnThemeNameChanged(string value)
    {
        if (!_suppressThemeApply) ThemeService.Apply(value, AccentColor);
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

    // ── Geçmiş ──────────────────────────────────────────────────────────────
    private void LoadBackupHistory()
    {
        BackupHistory.Clear();
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
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location
                .Replace(".dll", ".exe");
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
        };
        vm.Save();
        if (!string.IsNullOrEmpty(DefaultDestination) && string.IsNullOrEmpty(DestinationPath))
            DestinationPath = DefaultDestination;
        System.Windows.MessageBox.Show("Ayarlar kaydedildi.", "Itchy Backup",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
        _suppressThemeApply = true;
        ThemeName   = vm.ThemeName;
        AccentColor = vm.AccentColor;
        _suppressThemeApply = false;
        ThemeService.Apply(vm.ThemeName, vm.AccentColor);
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
