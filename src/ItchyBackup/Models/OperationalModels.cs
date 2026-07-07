using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ItchyBackup.Models;

public class DashboardCard
{
    public string Title { get; set; } = "";
    public string Value { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Status { get; set; } = "Info";
}

public class LogEntry
{
    public string Time { get; set; } = "";
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public string LevelBrushKey => Level.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ? "Danger" :
        Level.Contains("WARN", StringComparison.OrdinalIgnoreCase) ? "Warning" : "AccentLight";
}

public class ScheduledTaskInfo
{
    public string TaskName { get; set; } = "";
    public string Day { get; set; } = "";
    public string Status { get; set; } = "";
    public string LastRunTime { get; set; } = "";
    public string NextRunTime { get; set; } = "";
    public string LastResult { get; set; } = "";
    public bool Exists { get; set; }
    public string Summary => Exists
        ? $"{Day} • Son: {LastRunTime} • Sıradaki: {NextRunTime}"
        : $"{Day} • görev yok";
}

public class BackupValidationIssue
{
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
}

public class BackupValidationResult
{
    public string Status { get; set; } = "Bekliyor";
    public string Summary { get; set; } = "Yedek seçilmedi.";
    public int Score { get; set; }
    public ObservableCollection<BackupValidationIssue> Issues { get; } = new();
}
