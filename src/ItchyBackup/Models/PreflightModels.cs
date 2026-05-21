namespace ItchyBackup.Models;

public enum PreflightStatus { Ok, Warning, Error }

public class PreflightCheckItem
{
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public PreflightStatus Status { get; set; }

    public string StatusText => Status switch
    {
        PreflightStatus.Ok => "OK",
        PreflightStatus.Warning => "Uyarı",
        PreflightStatus.Error => "Hata",
        _ => "?"
    };

    public string Icon => Status switch
    {
        PreflightStatus.Ok => "✓",
        PreflightStatus.Warning => "!",
        PreflightStatus.Error => "✕",
        _ => "?"
    };
}

public class BackupPreset
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<string> ItemIds { get; set; } = new();
}
