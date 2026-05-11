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
    }
}
