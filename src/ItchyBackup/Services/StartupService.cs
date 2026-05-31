using Microsoft.Win32;

namespace ItchyBackup.Services;

public static class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ItchyBackup";

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath
                    ?? System.IO.Path.Combine(AppContext.BaseDirectory, "ItchyBackup.exe");
                key.SetValue(ValueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            LogService.Warn($"Windows başlangıç ayarı uygulanamadı: {ex.Message}");
        }
    }
}
