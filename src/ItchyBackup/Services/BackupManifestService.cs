using System.IO;
using Newtonsoft.Json;

namespace ItchyBackup.Services;

public class BackupManifest
{
    public string AppName { get; set; } = AppInfo.ProductName;
    public string AppVersion { get; set; } = AppInfo.Version;
    public string BackupId { get; set; } = Guid.NewGuid().ToString("N");
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    public string MachineName { get; set; } = Environment.MachineName;
    public string UserName { get; set; } = Environment.UserName;
    public string BackupType { get; set; } = "Tam";
    public string PackageType { get; set; } = "Klasör";
    public bool Encrypted { get; set; }
    public bool UsedVss { get; set; }
    public string IncrementalBase { get; set; } = "";
    public int SelectedItemCount { get; set; }
    public int FileCount { get; set; }
    public long TotalBytes { get; set; }
    public List<BackupManifestItem> SelectedItems { get; set; } = new();
    public List<BackupManifestFile> Files { get; set; } = new();
}

public class BackupManifestItem
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourcePath { get; set; } = "";
}

public class BackupManifestFile
{
    public string RelativePath { get; set; } = "";
    public long Size { get; set; }
    public string LastWriteTimeUtc { get; set; } = "";
}

public static class BackupManifestService
{
    public const string ManifestFileName = "backup_manifest.json";

    public static async Task<string> WriteAsync(
        string backupFolder,
        BackupOptions options,
        BackupResult result,
        string incrementalBase,
        CancellationToken ct)
    {
        var manifest = new BackupManifest
        {
            BackupType = options.IsIncremental ? "Artımlı" : "Tam",
            PackageType = options.UseZip ? "ZIP" : "Klasör",
            Encrypted = options.UseZip && options.UsePassword,
            UsedVss = options.UseVss,
            IncrementalBase = incrementalBase,
            SelectedItemCount = options.SelectedItems.Count,
            FileCount = result.FilesCopied,
            TotalBytes = result.TotalBytes,
            SelectedItems = options.SelectedItems.Select(i => new BackupManifestItem
            {
                Id = i.Id,
                Label = i.Label,
                Category = i.Parent?.Name ?? "",
                SourcePath = i.Path
            }).ToList()
        };

        foreach (var file in Directory.EnumerateFiles(backupFolder, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file);
            if (name.Equals(ManifestFileName, StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("checksums.sha256", StringComparison.OrdinalIgnoreCase)) continue;

            try
            {
                var info = new FileInfo(file);
                manifest.Files.Add(new BackupManifestFile
                {
                    RelativePath = Path.GetRelativePath(backupFolder, file),
                    Size = info.Length,
                    LastWriteTimeUtc = info.LastWriteTimeUtc.ToString("O")
                });
            }
            catch { }
        }

        manifest.FileCount = manifest.Files.Count;
        var path = Path.Combine(backupFolder, ManifestFileName);
        var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
        await File.WriteAllTextAsync(path, json, ct);
        return path;
    }

    public static BackupManifest? TryRead(string backupFolder)
    {
        try
        {
            var path = Path.Combine(backupFolder, ManifestFileName);
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<BackupManifest>(File.ReadAllText(path));
        }
        catch { return null; }
    }
}
