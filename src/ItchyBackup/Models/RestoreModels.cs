using CommunityToolkit.Mvvm.ComponentModel;

namespace ItchyBackup.Models;

public partial class RestoreCategoryItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;
    public string Name { get; set; } = "";
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public string SizeText => Services.DiskSpaceChecker.FormatBytes(TotalSize);
    public string SubText => $"{FileCount} dosya • {SizeText}";
}

public partial class BackupListEntry : ObservableObject
{
    public string FolderName { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string DateText { get; set; } = "";
    public string ComputerInfo { get; set; } = "";
    public long TotalSize { get; set; }
    public bool HasZip { get; set; }
    public bool HasErrors { get; set; }
    public string SizeText => Services.DiskSpaceChecker.FormatBytes(TotalSize);
    public string DisplayInfo
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(ComputerInfo)) parts.Add(ComputerInfo);
            parts.Add(SizeText);
            if (HasZip) parts.Add("ZIP");
            return string.Join(" • ", parts);
        }
    }
}
