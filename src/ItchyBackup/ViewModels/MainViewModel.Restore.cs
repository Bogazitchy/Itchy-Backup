using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItchyBackup.Services;

namespace ItchyBackup.ViewModels;

public class AvailableBackup
{
    public string Path { get; set; } = "";
    public string Name { get; set; } = "";
    public string Date { get; set; } = "";
    public string SizeText { get; set; } = "";
    public bool IsZip { get; set; }
}

public partial class MainViewModel
{
    public ObservableCollection<AvailableBackup> AvailableBackups { get; } = new();
    public ObservableCollection<RestoreItem> RestoreItems { get; } = new();
    public ObservableCollection<BackupFolderItem> RestoreFolders { get; } = new();

    [ObservableProperty] private AvailableBackup? _selectedBackupForRestore;
    [ObservableProperty] private string _restoreTargetPath = "";
    [ObservableProperty] private string _restoreZipPassword = "";
    [ObservableProperty] private bool _restoreOverwrite = false;
    [ObservableProperty] private bool _restorePartial = false;
    [ObservableProperty] private bool _isRestoring = false;
    [ObservableProperty] private double _restoreProgressPercent = 0;
    [ObservableProperty] private string _restoreProgressText = "Hazır";
    [ObservableProperty] private string _restoreCurrentFile = "";

    private CancellationTokenSource? _restoreCts;

    partial void OnSelectedBackupForRestoreChanged(AvailableBackup? value)
    {
        RestoreItems.Clear();
        RestoreFolders.Clear();
        if (value != null)
            LoadRestoreContents();
    }

    public void LoadAvailableBackups()
    {
        AvailableBackups.Clear();
        if (string.IsNullOrEmpty(DestinationPath) || !Directory.Exists(DestinationPath)) return;

        var dirs = Directory.GetDirectories(DestinationPath, "Yedek_*")
            .OrderByDescending(d => d).Take(50);

        foreach (var dir in dirs)
        {
            try
            {
                var name = Path.GetFileName(dir);
                long size = 0;
                try
                {
                    size = new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories)
                        .Sum(f => { try { return f.Length; } catch { return 0L; } });
                }
                catch { }

                // ZIP varsa onu göster
                var zipFile = Directory.GetFiles(dir, "*.zip").FirstOrDefault();

                AvailableBackups.Add(new AvailableBackup
                {
                    Path = zipFile ?? dir,
                    Name = name,
                    Date = name.Replace("Yedek_", "").Replace("_", " "),
                    SizeText = DiskSpaceChecker.FormatBytes(size),
                    IsZip = zipFile != null
                });
            }
            catch { }
        }
    }

    [RelayCommand]
    public void BrowseRestoreTarget()
    {
        using var d = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Geri yüklenecek hedef klasörü seçin",
            UseDescriptionForTitle = true
        };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            RestoreTargetPath = d.SelectedPath;
    }

    [RelayCommand]
    public void LoadRestoreContents()
    {
        if (SelectedBackupForRestore == null) return;
        RestoreItems.Clear();
        RestoreFolders.Clear();
        try
        {
            var items = RestoreEngine.ListBackupContents(
                SelectedBackupForRestore.Path,
                SelectedBackupForRestore.IsZip ? RestoreZipPassword : null);
            foreach (var item in items.OrderBy(i => i.RelativePath))
                RestoreItems.Add(item);

            var folderGroups = items
                .GroupBy(i => GetTopLevelFolder(i.RelativePath))
                .OrderBy(g => g.Key);

            foreach (var g in folderGroups)
            {
                var topItem = new BackupFolderItem
                {
                    FolderRelativePath = g.Key,
                    FileCount = g.Count(),
                    TotalSize = g.Sum(i => i.Size)
                };

                var subGroups = g
                    .GroupBy(i => GetSecondLevelPath(i.RelativePath, g.Key))
                    .Where(sg => !string.IsNullOrEmpty(sg.Key))
                    .OrderBy(sg => sg.Key);

                foreach (var sg in subGroups)
                {
                    topItem.Children.Add(new BackupFolderItem
                    {
                        FolderRelativePath = sg.Key,
                        FileCount = sg.Count(),
                        TotalSize = sg.Sum(i => i.Size)
                    });
                }

                RestoreFolders.Add(topItem);
            }

            RestoreProgressText = $"{RestoreItems.Count} dosya, {RestoreFolders.Count} klasör yüklendi";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"İçerik yüklenemedi:\n{ex.Message}",
                "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private static string GetTopLevelFolder(string relativePath)
    {
        var parts = relativePath.Replace('/', '\\').Split('\\');
        return parts.Length > 1 ? parts[0] : "";
    }

    private static string GetSecondLevelPath(string relativePath, string topLevelFolder)
    {
        var normalized = relativePath.Replace('/', '\\');
        var topLen = topLevelFolder.Length;
        if (normalized.Length <= topLen + 1) return "";
        var afterTop = normalized.Substring(topLen + 1);
        var sep = afterTop.IndexOf('\\');
        if (sep < 0) return "";
        return topLevelFolder + "\\" + afterTop.Substring(0, sep);
    }

    private List<string> GetSelectedFilePaths()
    {
        if (!RestoreFolders.Any())
            return RestoreItems.Where(i => i.IsSelected).Select(i => i.RelativePath).ToList();

        var selectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in RestoreFolders)
        {
            if (folder.Children.Count > 0)
            {
                foreach (var child in folder.Children.Where(c => c.IsSelected))
                    selectedPaths.Add(child.FolderRelativePath);
            }
            else if (folder.IsSelected)
            {
                selectedPaths.Add(folder.FolderRelativePath);
            }
        }

        return RestoreItems
            .Where(i => IsInSelectedPath(i.RelativePath, selectedPaths))
            .Select(i => i.RelativePath)
            .ToList();
    }

    private static bool IsInSelectedPath(string relativePath, HashSet<string> selectedPaths)
    {
        var normalized = relativePath.Replace('/', '\\');
        var dir = Path.GetDirectoryName(normalized) ?? "";
        foreach (var sp in selectedPaths)
        {
            if (string.IsNullOrEmpty(sp)) return true;
            if (dir.Equals(sp, StringComparison.OrdinalIgnoreCase) ||
                dir.StartsWith(sp + "\\", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    [RelayCommand]
    public async Task StartRestoreAsync()
    {
        if (SelectedBackupForRestore == null)
        {
            System.Windows.MessageBox.Show("Geri yüklenecek yedeği seçin.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(RestoreTargetPath))
        {
            System.Windows.MessageBox.Show("Hedef konumu seçin.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsRestoring = true;
        RestoreProgressPercent = 0;
        RestoreProgressText = "Başlıyor...";
        _restoreCts = new CancellationTokenSource();

        var options = new RestoreOptions
        {
            SourceBackupPath = SelectedBackupForRestore.Path,
            TargetPath = RestoreTargetPath,
            ZipPassword = RestoreZipPassword,
            Overwrite = RestoreOverwrite,
            SelectedRelativePaths = RestorePartial ? GetSelectedFilePaths() : new List<string>()
        };

        var progress = new Progress<RestoreProgress>(p =>
        {
            RestoreProgressPercent = p.Percent;
            RestoreCurrentFile = p.CurrentFile;
            RestoreProgressText = $"{p.CompletedFiles}/{p.TotalFiles} dosya"
                + (p.Skipped.Count > 0 ? $" • {p.Skipped.Count} atlandı" : "")
                + (p.Errors.Count > 0 ? $" • {p.Errors.Count} hata" : "");
        });

        try
        {
            var engine = new RestoreEngine(options, progress, _restoreCts.Token);
            await engine.RunAsync();
            RestoreProgressPercent = 100;
            RestoreProgressText = "Geri yükleme tamamlandı!";
            System.Windows.MessageBox.Show("Geri yükleme başarıyla tamamlandı.",
                "Itchy Backup", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            RestoreProgressText = "İptal edildi.";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Geri yükleme hatası:\n{ex.Message}",
                "Hata", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsRestoring = false;
            _restoreCts?.Dispose();
        }
    }

    [RelayCommand] public void CancelRestore() => _restoreCts?.Cancel();

    [RelayCommand]
    public void SelectAllRestoreItems()
    {
        foreach (var i in RestoreItems) i.IsSelected = true;
        foreach (var f in RestoreFolders)
        {
            f.IsSelected = true;
            foreach (var c in f.Children) c.IsSelected = true;
        }
    }

    [RelayCommand]
    public void ClearAllRestoreItems()
    {
        foreach (var i in RestoreItems) i.IsSelected = false;
        foreach (var f in RestoreFolders)
        {
            f.IsSelected = false;
            foreach (var c in f.Children) c.IsSelected = false;
        }
    }

    public string MachineName => Environment.MachineName;
    public string UserName => Environment.UserName;
}
