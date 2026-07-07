using System.Diagnostics;
using System.Text.RegularExpressions;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class ScheduledTaskService
{
    private static readonly string[] Days = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    public static List<ScheduledTaskInfo> ListItchyTasks()
    {
        var result = new List<ScheduledTaskInfo>();
        foreach (var day in Days)
        {
            var taskName = $"ItchyBackup_{day}";
            var info = QueryTask(taskName);
            info.Day = ToTurkishDay(day);
            result.Add(info);
        }
        return result;
    }

    public static void DeleteTask(string taskName)
    {
        RunSchtasks($"/delete /f /tn \"{taskName}\"");
        LogService.Info($"Zamanlayıcı görevi silindi: {taskName}");
    }

    public static void RunTask(string taskName)
    {
        RunSchtasks($"/run /tn \"{taskName}\"");
        LogService.Info($"Zamanlayıcı görevi test çalıştırıldı: {taskName}");
    }

    private static ScheduledTaskInfo QueryTask(string taskName)
    {
        var output = RunSchtasks($"/query /tn \"{taskName}\" /fo list /v");
        if (string.IsNullOrWhiteSpace(output) ||
            output.Contains("ERROR:", StringComparison.OrdinalIgnoreCase) ||
            output.Contains("HATA:", StringComparison.OrdinalIgnoreCase))
        {
            return new ScheduledTaskInfo { TaskName = taskName, Exists = false, Status = "Yok" };
        }

        return new ScheduledTaskInfo
        {
            TaskName = taskName,
            Exists = true,
            Status = ReadField(output, "Status", "Durum"),
            LastRunTime = ReadField(output, "Last Run Time", "Son Çalışma Zamanı"),
            NextRunTime = ReadField(output, "Next Run Time", "Sonraki Çalışma Zamanı"),
            LastResult = ReadField(output, "Last Result", "Son Sonuç")
        };
    }

    private static string RunSchtasks(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo("schtasks", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(psi);
            if (process == null) return "";
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(5000);
            return output + Environment.NewLine + error;
        }
        catch (Exception ex)
        {
            LogService.Warn($"schtasks çalıştırılamadı: {ex.Message}");
            return "";
        }
    }

    private static string ReadField(string text, params string[] names)
    {
        foreach (var name in names)
        {
            var match = Regex.Match(text, $"^{Regex.Escape(name)}:\\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }
        return "-";
    }

    private static string ToTurkishDay(string day) => day switch
    {
        "MON" => "Pazartesi",
        "TUE" => "Salı",
        "WED" => "Çarşamba",
        "THU" => "Perşembe",
        "FRI" => "Cuma",
        "SAT" => "Cumartesi",
        "SUN" => "Pazar",
        _ => day
    };
}
