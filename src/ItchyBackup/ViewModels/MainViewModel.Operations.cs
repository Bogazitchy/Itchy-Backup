using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItchyBackup.Models;
using ItchyBackup.Services;

namespace ItchyBackup.ViewModels;

public partial class MainViewModel
{
    public ObservableCollection<DashboardCard> DashboardCards { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; } = new();
    public ObservableCollection<ScheduledTaskInfo> ScheduledTasks { get; } = new();
    public ObservableCollection<BackupValidationIssue> BackupValidationIssues { get; } = new();

    [ObservableProperty] private string _dashboardStatus = "Kontrol bekleniyor";
    [ObservableProperty] private string _logFilterText = "";
    [ObservableProperty] private int _logLevelFilterIndex = 0;
    [ObservableProperty] private string _backupValidationSummary = "Yedek seçip test edebilirsiniz.";
    [ObservableProperty] private int _backupValidationScore = 0;
    [ObservableProperty] private bool _isValidatingBackup = false;

    partial void OnLogFilterTextChanged(string value) => RefreshLogs();
    partial void OnLogLevelFilterIndexChanged(int value) => RefreshLogs();

    [RelayCommand]
    public void RefreshDashboard()
    {
        DashboardCards.Clear();
        LoadBackupHistory();
        var last = BackupHistory.FirstOrDefault();
        var taskCount = ScheduledTaskService.ListItchyTasks().Count(t => t.Exists);
        var targetReady = !string.IsNullOrWhiteSpace(DestinationPath) && Directory.Exists(DestinationPath);
        var warningCount = ReadRecentLogLines()
            .SelectMany(kv => kv.Value)
            .Count(l => l.Contains("[WARN", StringComparison.OrdinalIgnoreCase) || l.Contains("[ERROR", StringComparison.OrdinalIgnoreCase));

        DashboardCards.Add(new DashboardCard
        {
            Title = "Son Yedek",
            Value = last?.FolderName ?? "Yok",
            Detail = last?.Summary ?? "Henüz yedek geçmişi bulunamadı.",
            Icon = "",
            Status = last?.HasErrors == true ? "Danger" : "Ok"
        });
        DashboardCards.Add(new DashboardCard
        {
            Title = "Zamanlayıcı",
            Value = taskCount > 0 ? $"{taskCount} görev" : "Kapalı",
            Detail = taskCount > 0 ? "Windows görevleri kayıtlı." : "Otomatik yedekleme tanımlı değil.",
            Icon = "",
            Status = taskCount > 0 ? "Ok" : "Info"
        });
        DashboardCards.Add(new DashboardCard
        {
            Title = "Yedek Hedefi",
            Value = targetReady ? "Hazır" : "Eksik",
            Detail = string.IsNullOrWhiteSpace(DestinationPath) ? "Varsayılan hedef seçilmemiş." : DestinationPath,
            Icon = "",
            Status = targetReady ? "Ok" : "Warning"
        });
        DashboardCards.Add(new DashboardCard
        {
            Title = "Son Loglar",
            Value = warningCount == 0 ? "Temiz" : $"{warningCount} uyarı",
            Detail = "Son uygulama loglarından hesaplandı.",
            Icon = "",
            Status = warningCount == 0 ? "Ok" : "Warning"
        });

        DashboardStatus = $"{DateTime.Now:HH:mm} kontrolü • {DashboardCards.Count} kart";
    }

    [RelayCommand]
    public void RefreshLogs()
    {
        LogEntries.Clear();
        var filter = LogFilterText?.Trim() ?? "";
        var lines = ReadRecentLogLines();

        foreach (var (line, source) in lines.SelectMany(kv => kv.Value.Select(l => (l, kv.Key))))
        {
            var entry = ParseLogLine(line, source);
            if (!PassesLogFilter(entry, filter)) continue;
            LogEntries.Add(entry);
        }
    }

    [RelayCommand]
    public void RefreshScheduledTasks()
    {
        ScheduledTasks.Clear();
        foreach (var task in ScheduledTaskService.ListItchyTasks())
            ScheduledTasks.Add(task);
    }

    [RelayCommand]
    public void DeleteScheduledTask(ScheduledTaskInfo? task)
    {
        if (task?.Exists != true) return;
        ScheduledTaskService.DeleteTask(task.TaskName);
        RefreshScheduledTasks();
        RefreshDashboard();
    }

    [RelayCommand]
    public void RunScheduledTask(ScheduledTaskInfo? task)
    {
        if (task?.Exists != true) return;
        ScheduledTaskService.RunTask(task.TaskName);
        RefreshScheduledTasks();
    }

    [RelayCommand]
    public async Task ValidateSelectedBackupAsync()
    {
        if (SelectedBackupForRestore == null)
        {
            System.Windows.MessageBox.Show("Test edilecek yedeği seçin.", "Yedek Testi",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsValidatingBackup = true;
        BackupValidationIssues.Clear();
        BackupValidationSummary = "Yedek test ediliyor...";
        try
        {
            var result = await BackupValidationService.ValidateAsync(
                SelectedBackupForRestore.Path,
                SelectedBackupForRestore.IsZip ? RestoreZipPassword : null,
                CancellationToken.None);
            BackupValidationScore = result.Score;
            BackupValidationSummary = result.Summary;
            foreach (var issue in result.Issues)
                BackupValidationIssues.Add(issue);
        }
        catch (Exception ex)
        {
            BackupValidationSummary = $"Test hatası: {ex.Message}";
        }
        finally
        {
            IsValidatingBackup = false;
        }
    }

    private Dictionary<string, List<string>> ReadRecentLogLines()
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in LogService.GetRecentLogFiles())
        {
            try
            {
                result[Path.GetFileName(file)] = File.ReadLines(file).Reverse().Take(250).ToList();
            }
            catch { }
        }

        try
        {
            LoadBackupHistory();
            foreach (var backup in BackupHistory.Take(8))
            {
                var log = Directory.Exists(backup.DestinationPath)
                    ? Directory.GetFiles(backup.DestinationPath, "backup_log_*.txt")
                        .OrderByDescending(File.GetLastWriteTime)
                        .FirstOrDefault()
                    : null;
                if (log == null) continue;
                result[Path.GetFileName(backup.DestinationPath) + "\\" + Path.GetFileName(log)] =
                    File.ReadLines(log).Reverse().Take(250).ToList();
            }
        }
        catch { }
        return result;
    }

    private static LogEntry ParseLogLine(string line, string source)
    {
        var match = Regex.Match(line, @"^\[(?<time>[^\]]+)\]\s+\[(?<level>[^\]]+)\]\s+(?<msg>.*)$");
        return match.Success
            ? new LogEntry
            {
                Time = match.Groups["time"].Value,
                Level = match.Groups["level"].Value.Trim(),
                Message = match.Groups["msg"].Value,
                SourceFile = source
            }
            : new LogEntry { Time = "", Level = "INFO", Message = line, SourceFile = source };
    }

    private bool PassesLogFilter(LogEntry entry, string filter)
    {
        if (LogLevelFilterIndex == 1 && !entry.Level.Contains("ERROR", StringComparison.OrdinalIgnoreCase)) return false;
        if (LogLevelFilterIndex == 2 && !entry.Level.Contains("WARN", StringComparison.OrdinalIgnoreCase)) return false;
        if (LogLevelFilterIndex == 3 && !entry.Level.Contains("INFO", StringComparison.OrdinalIgnoreCase)) return false;
        return string.IsNullOrWhiteSpace(filter)
            || entry.Message.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || entry.SourceFile.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
