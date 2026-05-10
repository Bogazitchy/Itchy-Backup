using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;

namespace ItchyBackup.Services;

public class RestoreOptions
{
    public string SourceBackupPath { get; set; } = "";  // Yedek klasörü veya .zip
    public string TargetPath { get; set; } = "";        // Geri yüklenecek konum
    public string? ZipPassword { get; set; }
    public bool Overwrite { get; set; } = false;
    public List<string> SelectedRelativePaths { get; set; } = new(); // Boşsa tümü
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
    }

    /// <summary>Yedek içindeki dosya/klasör yapısını listeler (kısmi geri yükleme için).</summary>
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
                if (f.EndsWith("checksums.sha256") || Path.GetFileName(f).StartsWith("backup_log_"))
                    continue;
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
            .Where(f => !f.EndsWith("checksums.sha256") && !Path.GetFileName(f).StartsWith("backup_log_"))
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

            if (File.Exists(dest) && !_options.Overwrite)
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
                if (!e.IsDirectory) entries.Add(e);

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

                if (File.Exists(dest) && !_options.Overwrite)
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

    public string FolderRelativePath { get; set; } = "";
    public string DisplayName => string.IsNullOrEmpty(FolderRelativePath) ? "(Kök dosyalar)" : FolderRelativePath;
    public int FileCount { get; set; }
    public long TotalSize { get; set; }
    public string SizeText => DiskSpaceChecker.FormatBytes(TotalSize);
    public string SubText => $"{FileCount} dosya • {SizeText}";
}
