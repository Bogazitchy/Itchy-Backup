namespace ItchyBackup.Models;

public class BackupProfile
{
    public string ProfileName { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string LastUsed { get; set; } = "";
    public string DefaultDestination { get; set; } = "";
    public bool UseZip { get; set; } = false;
    public bool UsePassword { get; set; } = false;
    public bool UseVss { get; set; } = true;
    public bool VerifyChecksum { get; set; } = true;
    public int KeepLastN { get; set; } = 3;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Normal;
    public List<string> SelectedItemIds { get; set; } = new();
    public bool IsIncremental { get; set; } = false;
    public bool UseNetworkCredentials { get; set; } = false;
    public string NetworkUsername { get; set; } = "";
    public string NetworkDomain { get; set; } = "";
    public string Icon { get; set; } = "person";

    public string IconPath => ProfileIcons.GetPath(Icon);
}

public enum CompressionLevel
{
    None = 0,
    Fast = 1,
    Normal = 5,
    Best = 9
}
