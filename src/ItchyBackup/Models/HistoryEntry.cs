namespace ItchyBackup.Models;

public class HistoryEntry
{
    public string FolderName { get; set; } = "";
    public string Date { get; set; } = "";
    public string Summary { get; set; } = "";
    public bool HasErrors { get; set; } = false;
    public string DestinationPath { get; set; } = "";

    public string DestinationRoot =>
        string.IsNullOrEmpty(DestinationPath)
            ? ""
            : System.IO.Path.GetDirectoryName(DestinationPath) ?? DestinationPath;
}
