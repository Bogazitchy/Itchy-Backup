using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace ItchyBackup.Services;

public class RestoreOptions
{
    public string SourceBackupPath { get; set; } = "";
    public string TargetPath { get; set; } = "";
    public string? ZipPassword { get; set; }
    public bool Overwrite { get; set; } = false;
    public RestoreConflictPolicy ConflictPolicy { get; set; } = RestoreConflictPolicy.Skip;
    public List<string> SelectedRelativePaths { get; set; } = new();
}

public enum RestoreConflictPolicy
{
    Skip,
    Overwrite,
    Rename
}

public class RestoreProgress
{
    public string CurrentFile { get; set; } = "";
    public int CompletedFiles { get; set; }
    public int TotalFiles { get; set; }
    public long CopiedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double Percent => TotalBytes > 0 ? Math.Min(100, (double)CopiedBytes / TotalBytes * 100) : 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Skipped { get; set; } = new();
}

public class RestoreEngine
{
    private readonly RestoreOptions _options;
    private readonly IProgress<RestoreProgress>? _progress;
    private readonly CancellationToken _ct;
    private readonly RestoreProgress _state = new();

    public RestoreEngine(RestoreOptions options, IProgress<RestoreProgress>? progress, CancellationToken ct)
    {
        _options = options;
        _progress = progress;
        _ct = ct;
    }

    public async Task RunAsync()
    {
        var src = _options.SourceBackupPath;
        Directory.CreateDirectory(_options.TargetPath);

        if (File.Exists(src) && src.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            await RestoreFromZipAsync(src);
        else if (Directory.Exists(src))
            await RestoreFromFolderAsync(src);
        else
            throw new FileNotFoundException("Yedek konumu bulunamadı: " + src);

        await WriteRestoreReportAsync();
    }

    public static List<RestoreItem> ListBackupContents(string backupPath, string? zipPassword = null)
    {
        var items = new List<RestoreItem>();
        if (File.Exists(backupPath) && backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var fs = File.OpenRead(backupPath);
            using var zip = new ZipFile(fs);
            if (!string.IsNullOrEmpty(zipPassword)) zip.Password = zipPassword;
            foreach (ZipEntry entry in zip)
            {
                if (entry.IsDirectory) continue;
                items.Add(new RestoreItem
                {
                    RelativePath = entry.Name,
                    Size = entry.Size,
                    IsZipEntry = true
                });
            }
        }
        else if (Directory.Exists(backupPath))
        {
            foreach (var f in Directory.EnumerateFiles(backupPath, "*", SearchOption.AllDirectories))
            {
                if (IsMetadataFile(f)) continue;
                var rel = Path.GetRelativePath(backupPath, f);
                long size = 0;
                try { size = new FileInfo(f).Length; } catch { }
                items.Add(new RestoreItem
                {
                    RelativePath = rel,
                    Size = size,
                    IsZipEntry = false
                });
            }
        }
        return items;
    }

    private async Task RestoreFromFolderAsync(string sourceFolder)
    {
        var allFiles = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories)
            .Where(f => !IsMetadataFile(f))
            .ToList();

        if (_options.SelectedRelativePaths.Any())
        {
            var selectedSet = _options.SelectedRelativePaths
                .Select(p => p.Replace('/', '\\'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            allFiles = allFiles
                .Where(f => selectedSet.Contains(Path.GetRelativePath(sourceFolder, f).Replace('/', '\\')))
                .ToList();
        }

        _state.TotalFiles = allFiles.Count;
        _state.TotalBytes = allFiles.Sum(f => { try { return new FileInfo(f).Length; } catch { return 0L; } });
        if (_state.TotalBytes == 0) _state.TotalBytes = 1;
        _progress?.Report(Clone());

        foreach (var src in allFiles)
        {
            _ct.ThrowIfCancellationRequested();
            var rel = Path.GetRelativePath(sourceFolder, src);
            var dest = Path.Combine(_options.TargetPath, rel);
            dest = ResolveConflictDestination(dest, rel);
            if (string.IsNullOrEmpty(dest))
            {
                _state.Skipped.Add(rel);
                continue;
            }

            try
            {
                _state.CurrentFile = rel;
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                await CopyFileWithProgressAsync(src, dest);
                _state.CompletedFiles++;
                _progress?.Report(Clone());
            }
            catch (Exception ex)
            {
                _state.Errors.Add($"{rel}: {ex.Message}");
            }
        }
    }

    private async Task RestoreFromZipAsync(string zipPath)
    {
        await Task.Run(() =>
        {
            using var fs = File.OpenRead(zipPath);
            using var zip = new ZipFile(fs);
            if (!string.IsNullOrEmpty(_options.ZipPassword)) zip.Password = _options.ZipPassword;

            var entries = new List<ZipEntry>();
            foreach (ZipEntry e in zip)
                if (!e.IsDirectory && !IsMetadataPath(e.Name)) entries.Add(e);

            if (_options.SelectedRelativePaths.Any())
            {
                var selSet = _options.SelectedRelativePaths
                    .Select(p => p.Replace('\\', '/'))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                entries = entries.Where(e => selSet.Contains(e.Name)).ToList();
            }

            _state.TotalFiles = entries.Count;
            _state.TotalBytes = entries.Sum(e => e.Size);
            if (_state.TotalBytes == 0) _state.TotalBytes = 1;
            _progress?.Report(Clone());

            foreach (var entry in entries)
            {
                _ct.ThrowIfCancellationRequested();
                var dest = Path.Combine(_options.TargetPath, entry.Name);
                dest = ResolveConflictDestination(dest, entry.Name);
                if (string.IsNullOrEmpty(dest))
                {
                    _state.Skipped.Add(entry.Name);
                    continue;
                }

                try
                {
                    _state.CurrentFile = entry.Name;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    using var inp = zip.GetInputStream(entry);
                    using var outp = File.Create(dest);
                    var buf = new byte[81920];
                    int read;
                    while ((read = inp.Read(buf, 0, buf.Length)) > 0)
                    {
                        outp.Write(buf, 0, read);
                        _state.CopiedBytes += read;
                    }
                    _state.CompletedFiles++;
                    _progress?.Report(Clone());
                }
                catch (Exception ex)
                {
                    _state.Errors.Add($"{entry.Name}: {ex.Message}");
                }
            }
        }, _ct);
    }

    private async Task CopyFileWithProgressAsync(string src, string dest)
    {
        const int Buf = 81920;
        await using var fsIn = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, Buf, true);
        await using var fsOut = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, Buf, true);
        var buffer = new byte[Buf];
        int read;
        while ((read = await fsIn.ReadAsync(buffer, _ct)) > 0)
        {
            await fsOut.WriteAsync(buffer.AsMemory(0, read), _ct);
            _state.CopiedBytes += read;
            if (_state.CopiedBytes % (1024 * 1024) < Buf)
                _progress?.Report(Clone());
        }
    }

    private string ResolveConflictDestination(string dest, string displayPath)
    {
        if (!File.Exists(dest)) return dest;
        var policy = _options.Overwrite ? RestoreConflictPolicy.Overwrite : _options.ConflictPolicy;
        if (policy == RestoreConflictPolicy.Overwrite) return dest;
        if (policy == RestoreConflictPolicy.Skip) return "";

        var dir = Path.GetDirectoryName(dest) ?? _options.TargetPath;
        var name = Path.GetFileNameWithoutExtension(dest);
        var ext = Path.GetExtension(dest);
        for (var i = 1; i < 1000; i++)
        {
            var candidate = Path.Combine(dir, $"{name}_geri_yuklenen_{i}{ext}");
            if (!File.Exists(candidate)) return candidate;
        }

        _state.Errors.Add($"{displayPath}: yeniden adlandırma hedefi üretilemedi");
        return "";
    }

    private async Task WriteRestoreReportAsync()
    {
        try
        {
            var reportPath = Path.Combine(_options.TargetPath, $"restore_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            var lines = new List<string>
            {
                "Itchy Backup Restore Raporu",
                $"Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Kaynak: {_options.SourceBackupPath}",
                $"Hedef: {_options.TargetPath}",
                $"Dosya: {_state.CompletedFiles}/{_state.TotalFiles}",
                $"Atlanan: {_state.Skipped.Count}",
                $"Hata: {_state.Errors.Count}",
                ""
            };
            if (_state.Skipped.Count > 0)
            {
                lines.Add("Atlanan dosyalar:");
                lines.AddRange(_state.Skipped.Take(300));
                lines.Add("");
            }
            if (_state.Errors.Count > 0)
            {
                lines.Add("Hatalar:");
                lines.AddRange(_state.Errors.Take(300));
            }
            await File.WriteAllLinesAsync(reportPath, lines, _ct);
        }
        catch { }
    }

    private RestoreProgress Clone() => new()
    {
        CurrentFile = _state.CurrentFile,
        CompletedFiles = _state.CompletedFiles,
        TotalFiles = _state.TotalFiles,
        CopiedBytes = _state.CopiedBytes,
        TotalBytes = _state.TotalBytes,
        Errors = _state.Errors.ToList(),
        Skipped = _state.Skipped.ToList()
    };

    private static bool IsMetadataFile(string path) => IsMetadataPath(Path.GetFileName(path));

    private static bool IsMetadataPath(string path)
    {
        var name = Path.GetFileName(path.Replace('/', '\\'));
        return name.Equals("checksums.sha256", StringComparison.OrdinalIgnoreCase)
            || name.Equals(BackupManifestService.ManifestFileName, StringComparison.OrdinalIgnoreCase)
            || name.Equals("backup_report.html", StringComparison.OrdinalIgnoreCase)
            || name.Equals("system_info.txt", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("backup_log_", StringComparison.OrdinalIgnoreCase);
    }
}

public partial class RestoreItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isSelected = true;

    public string RelativePath { get; set; } = "";
    public long Size { get; set; }
    public bool IsZipEntry { get; set; }
    public string Folder => Path.GetDirectoryName(RelativePath)?.Replace('\\', '/') ?? "";
    public string FileName => Path.GetFileName(RelativePath);
    public string SizeText => DiskSpaceChecker.FormatBytes(Size);
}

public partial class BackupFolderItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isSelected = true;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool _isExpanded = false;

    public string FolderRelativePath { get; set; } = "";

    public string DisplayName => string.IsNullOrEmpty(FolderRelativePath)
        ? "(Kök dosyalar)"
        : Path.GetFileName(FolderRelativePath.TrimEnd('\\')) is { Length: > 0 } n ? n : FolderRelativePath;

    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public string SizeText => DiskSpaceChecker.FormatBytes(TotalSize);
    public string SubText => $"{FileCount} dosya • {SizeText}";

    public System.Collections.ObjectModel.ObservableCollection<BackupFolderItem> Children { get; } = new();
    public bool HasChildren => Children.Count > 0;

    partial void OnIsSelectedChanged(bool value)
    {
        foreach (var child in Children) child.IsSelected = value;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}
