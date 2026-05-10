using System.IO;

namespace ItchyBackup.Services;

public static class LogService
{
    private static string _logPath = "";
    private static readonly object _lock = new();

    public static void Initialize(string? customPath = null)
    {
        var logDir = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ItchyBackup", "Logs");
        Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, $"itchy_{DateTime.Now:yyyy-MM-dd}.log");
    }

    public static void InitializeForBackup(string backupFolder)
    {
        _logPath = Path.Combine(backupFolder, $"backup_log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
    }

    public static void Info(string message) => Write("INFO ", message);
    public static void Warn(string message) => Write("WARN ", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Error(string message, Exception ex) => Write("ERROR", $"{message} | {ex.Message}");

    private static void Write(string level, string message)
    {
        if (string.IsNullOrEmpty(_logPath)) return;
        lock (_lock)
        {
            try
            {
                var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch { }
        }
    }

    public static string GetLogPath() => _logPath;
}
