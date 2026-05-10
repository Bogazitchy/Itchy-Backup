using System.Diagnostics;

namespace ItchyBackup.Services;

public static class NotificationService
{
    public static void ShowSuccess(string title, string message)
    {
        ShowBalloon(title, message, "info");
    }

    public static void ShowError(string title, string message)
    {
        ShowBalloon(title, message, "error");
    }

    public static void ShowWarning(string title, string message)
    {
        ShowBalloon(title, message, "warning");
    }

    private static void ShowBalloon(string title, string message, string type)
    {
        try
        {
            // Windows 10+ Toast bildirimi için PowerShell kullan
            // Bu yaklaşım her .NET versiyonunda çalışır, ekstra bağımlılık gerektirmez
            var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
$textNodes = $template.GetElementsByTagName('text')
$textNodes[0].AppendChild($template.CreateTextNode('{EscapeForPowerShell(title)}')) | Out-Null
$textNodes[1].AppendChild($template.CreateTextNode('{EscapeForPowerShell(message)}')) | Out-Null
$toast = [Windows.UI.Notifications.ToastNotification]::new($template)
$notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Itchy Backup')
$notifier.Show($toast)
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NonInteractive -WindowStyle Hidden -Command \"{script.Replace("\"", "\\\"")}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            LogService.Warn($"Toast bildirimi gönderilemedi: {ex.Message}");
        }
    }

    private static string EscapeForPowerShell(string s) =>
        s.Replace("'", "''").Replace("`", "``");
}
