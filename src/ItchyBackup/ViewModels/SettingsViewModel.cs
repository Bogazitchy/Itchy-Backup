using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using Newtonsoft.Json;

namespace ItchyBackup.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ItchyBackup", "settings.json");

    [ObservableProperty] private bool _startWithWindows = false;
    [ObservableProperty] private bool _minimizeToTray = false;
    [ObservableProperty] private bool _autoChecksum = true;
    [ObservableProperty] private bool _soundNotification = true;
    [ObservableProperty] private bool _openFolderAfterBackup = false;
    [ObservableProperty] private string _defaultDestination = "";
    [ObservableProperty] private string _themeName = "Dark";
    [ObservableProperty] private string _accentColor = "#6C5CE7";
    [ObservableProperty] private bool _enableWebhookNotifications = false;
    [ObservableProperty] private string _webhookUrl = "";
    [ObservableProperty] private bool _enableEmailNotifications = false;
    [ObservableProperty] private string _smtpHost = "";
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private bool _smtpSsl = true;
    [ObservableProperty] private string _smtpUsername = "";
    [ObservableProperty] private string _smtpPassword = "";
    [ObservableProperty] private string _emailFrom = "";
    [ObservableProperty] private string _emailTo = "";

    public SettingsViewModel() => Load();

    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var s = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(SettingsPath));
            if (s == null) return;
            StartWithWindows      = s.StartWithWindows;
            MinimizeToTray        = s.MinimizeToTray;
            AutoChecksum          = s.AutoChecksum;
            SoundNotification     = s.SoundNotification;
            OpenFolderAfterBackup = s.OpenFolderAfterBackup;
            DefaultDestination    = s.DefaultDestination;
            ThemeName             = s.ThemeName;
            AccentColor           = s.AccentColor;
            EnableWebhookNotifications = s.EnableWebhookNotifications;
            WebhookUrl = s.WebhookUrl;
            EnableEmailNotifications = s.EnableEmailNotifications;
            SmtpHost = s.SmtpHost;
            SmtpPort = s.SmtpPort;
            SmtpSsl = s.SmtpSsl;
            SmtpUsername = s.SmtpUsername;
            SmtpPassword = s.SmtpPassword;
            EmailFrom = s.EmailFrom;
            EmailTo = s.EmailTo;
        }
        catch { }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(new SettingsData
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
            }, Formatting.Indented));
        }
        catch { }
    }

    private class SettingsData
    {
        public bool StartWithWindows { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool AutoChecksum { get; set; } = true;
        public bool SoundNotification { get; set; } = true;
        public bool OpenFolderAfterBackup { get; set; } = false;
        public string DefaultDestination { get; set; } = "";
        public string ThemeName { get; set; } = "Dark";
        public string AccentColor { get; set; } = "#6C5CE7";
        public bool EnableWebhookNotifications { get; set; }
        public string WebhookUrl { get; set; } = "";
        public bool EnableEmailNotifications { get; set; }
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool SmtpSsl { get; set; } = true;
        public string SmtpUsername { get; set; } = "";
        public string SmtpPassword { get; set; } = "";
        public string EmailFrom { get; set; } = "";
        public string EmailTo { get; set; } = "";
    }
}
