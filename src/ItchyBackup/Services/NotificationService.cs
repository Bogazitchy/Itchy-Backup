using System.Diagnostics;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using Newtonsoft.Json;

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

    public static async Task SendExternalAsync(NotificationOptions options, string title, string message)
    {
        if (options.EnableWebhook && !string.IsNullOrWhiteSpace(options.WebhookUrl))
            await SendWebhookAsync(options.WebhookUrl, title, message);

        if (options.EnableEmail && !string.IsNullOrWhiteSpace(options.EmailTo))
            await SendEmailAsync(options, title, message);
    }

    private static async Task SendWebhookAsync(string url, string title, string message)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var payload = JsonConvert.SerializeObject(new
            {
                content = $"{title}\n{message}",
                text = $"{title}\n{message}"
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            await client.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            LogService.Warn($"Webhook bildirimi gönderilemedi: {ex.Message}");
        }
    }

    private static async Task SendEmailAsync(NotificationOptions options, string title, string message)
    {
        try
        {
            using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
            {
                EnableSsl = options.SmtpSsl
            };
            if (!string.IsNullOrWhiteSpace(options.SmtpUsername))
                client.Credentials = new System.Net.NetworkCredential(options.SmtpUsername, options.SmtpPassword);

            using var mail = new MailMessage(
                string.IsNullOrWhiteSpace(options.EmailFrom) ? options.SmtpUsername : options.EmailFrom,
                options.EmailTo,
                title,
                message);
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            LogService.Warn($"E-posta bildirimi gönderilemedi: {ex.Message}");
        }
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

public class NotificationOptions
{
    public bool EnableWebhook { get; set; }
    public string WebhookUrl { get; set; } = "";
    public bool EnableEmail { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool SmtpSsl { get; set; } = true;
    public string SmtpUsername { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string EmailFrom { get; set; } = "";
    public string EmailTo { get; set; } = "";
}
