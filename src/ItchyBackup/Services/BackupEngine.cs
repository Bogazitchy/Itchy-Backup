using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public class BackupOptions
{
    public string DestinationRoot { get; set; } = "";
    public bool UseZip { get; set; } = false;
    public bool UsePassword { get; set; } = false;
    public string? Password { get; set; }
    public bool UseVss { get; set; } = true;
    public bool VerifyChecksum { get; set; } = true;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Normal;
    public List<BackupItem> SelectedItems { get; set; } = new();
}

public class BackupEngine
{
    private readonly BackupOptions _options;
    private readonly IProgress<BackupProgress>? _progress;
    private readonly CancellationToken _ct;
    private readonly BackupProgress _state = new();
    private readonly Stopwatch _sw = new();
    private long _totalBytesEstimate = 0;

    public BackupEngine(BackupOptions options, IProgress<BackupProgress>? progress, CancellationToken ct)
    {
        _options = options;
        _progress = progress;
        _ct = ct;
    }

    public async Task RunAsync()
    {
        _sw.Start();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupFolder = Path.Combine(_options.DestinationRoot, $"Yedek_{timestamp}");
        Directory.CreateDirectory(backupFolder);

        LogService.InitializeForBackup(backupFolder);
        LogService.Info("=== Itchy Backup başladı ===");
        LogService.Info($"Hedef: {backupFolder}");
        LogService.Info($"ZIP: {_options.UseZip}, Şifre: {_options.UsePassword}, VSS: {_options.UseVss}");

        _state.TotalItems = _options.SelectedItems.Count;

        // Önce boyut tahmini yap
        _totalBytesEstimate = await EstimateTotalSizeAsync();
        _state.TotalBytes = _totalBytesEstimate > 0 ? _totalBytesEstimate : 1;

        Report("Yedekleme başlıyor...", "");

        using var vss = _options.UseVss ? new VssService() : null;

        // ZIP modu: geçici klasöre kopyala, sonra zip'le
        string workFolder = _options.UseZip
            ? Path.Combine(Path.GetTempPath(), $"ItchyBackup_{timestamp}")
            : backupFolder;

        if (_options.UseZip)
            Directory.CreateDirectory(workFolder);

        for (int i = 0; i < _options.SelectedItems.Count; i++)
        {
            _ct.ThrowIfCancellationRequested();
            var item = _options.SelectedItems[i];
            _state.CompletedItems = i;
            _state.CurrentCategory = item.Parent?.Name ?? "";
            Report($"Kopyalanıyor: {item.Label}", item.Parent?.Name ?? "");

            try
            {
                await BackupItemAsync(item, workFolder, vss);
                LogService.Info($"OK: {item.Label}");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _state.Errors.Add($"{item.Label}: {ex.Message}");
                LogService.Error($"HATA: {item.Label}", ex);
            }
        }

        _state.CompletedItems = _state.TotalItems;

        // ZIP sıkıştırma
        if (_options.UseZip)
        {
            Report("ZIP oluşturuluyor...", "Sıkıştırma");
            var zipPath = Path.Combine(backupFolder,
                $"Yedek_{timestamp}{(_options.UsePassword ? "_sifireli" : "")}.zip");
            await CreateZipAsync(workFolder, zipPath);
            // Geçici klasörü temizle
            try { Directory.Delete(workFolder, true); } catch { }
            LogService.Info($"ZIP oluşturuldu: {zipPath}");
        }

        // Checksum
        if (_options.VerifyChecksum)
        {
            Report("Checksum hesaplanıyor...", "Doğrulama");
            var prog = new Progress<string>(s => Report(s, "Checksum"));
            await ChecksumService.WriteManifestAsync(backupFolder, prog, _ct);
            LogService.Info("Checksum manifest yazıldı.");
        }

        _sw.Stop();
        _state.Elapsed = _sw.Elapsed;
        _state.CopiedBytes = _state.TotalBytes;
        Report("Tamamlandı!", "");
        LogService.Info($"=== Tamamlandı. Süre: {_sw.Elapsed:mm\\:ss} Hata: {_state.Errors.Count} ===");

        NotificationService.ShowSuccess("Itchy Backup",
            $"Yedekleme tamamlandı!\n{_state.TotalItems} kategori • {_state.Errors.Count} hata • {_sw.Elapsed:mm\\:ss}");
    }

    private async Task BackupItemAsync(BackupItem item, string destRoot, VssService? vss)
    {
        var sourcePath = Environment.ExpandEnvironmentVariables(item.Path);
        if (vss != null && item.RequiresVss)
            sourcePath = vss.GetVssPath(sourcePath);

        var safeLabel = SanitizeName(item.Label);
        var safeCat = SanitizeName(item.Parent?.Name ?? "Diger");
        var destDir = Path.Combine(destRoot, safeCat, safeLabel);
        Directory.CreateDirectory(destDir);

        if (sourcePath == "Sistem genelinde")
        {
            // SQLite / Access tarama
            await ScanSystemAsync(item.Id, destDir);
        }
        else if (Directory.Exists(sourcePath))
        {
            await CopyDirectoryAsync(sourcePath, destDir, item.Parent?.Name ?? "");
        }
        else if (sourcePath.Contains('*'))
        {
            var dir = Path.GetDirectoryName(sourcePath) ?? "";
            var pat = Path.GetFileName(sourcePath);
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir, pat, SearchOption.AllDirectories))
                {
                    _ct.ThrowIfCancellationRequested();
                    await CopyFileAsync(f, Path.Combine(destDir, Path.GetFileName(f)), item.Parent?.Name ?? "");
                }
        }
        else
        {
            _state.Warnings.Add($"{item.Label}: kaynak bulunamadı ({sourcePath})");
            LogService.Warn($"Kaynak yok: {sourcePath}");
        }
    }

    private async Task CopyDirectoryAsync(string source, string dest, string category)
    {
        var di = new DirectoryInfo(source);
        if (!di.Exists) return;

        // Alt dizinleri oluştur
        foreach (var dir in di.GetDirectories("*", SearchOption.AllDirectories))
        {
            _ct.ThrowIfCancellationRequested();
            var target = dir.FullName.Replace(source, dest);
            try { Directory.CreateDirectory(target); } catch { }
        }

        // Dosyaları kopyala
        var files = di.GetFiles("*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            _ct.ThrowIfCancellationRequested();
            var destFile = file.FullName.Replace(source, dest);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                Report($"{file.Name}", category);
                await CopyFileAsync(file.FullName, destFile, category);
            }
            catch (Exception ex)
            {
                LogService.Warn($"Kopyalanamadı: {file.Name} - {ex.Message}");
            }
        }
    }

    private async Task CopyFileAsync(string src, string dest, string category)
    {
        const int BufSize = 81920;
        try
        {
            using var fsIn  = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufSize, true);
            using var fsOut = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, BufSize, true);

            var buffer = new byte[BufSize];
            int read;
            var lastReport = DateTime.Now;

            while ((read = await fsIn.ReadAsync(buffer, _ct)) > 0)
            {
                await fsOut.WriteAsync(buffer.AsMemory(0, read), _ct);
                _state.CopiedBytes += read;

                // Her 200ms'de bir progress raporla
                if ((DateTime.Now - lastReport).TotalMilliseconds > 200)
                {
                    UpdateSpeed();
                    _progress?.Report(CloneState());
                    lastReport = DateTime.Now;
                }
            }
            UpdateSpeed();
        }
        catch (IOException ex) when (ex.Message.Contains("used by another"))
        {
            LogService.Warn($"Dosya kilitli, atlanıyor: {Path.GetFileName(src)}");
            _state.Warnings.Add($"Kilitli: {Path.GetFileName(src)}");
        }
    }

    private async Task CreateZipAsync(string sourceFolder, string zipPath)
    {
        await Task.Run(() =>
        {
            using var fsOut = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using var zip = new ZipOutputStream(fsOut);
            zip.SetLevel(6);
            if (_options.UsePassword && !string.IsNullOrEmpty(_options.Password))
                zip.Password = _options.Password;

            AddFolderToZip(zip, sourceFolder, "", _options.UsePassword ? _options.Password : null);
        }, _ct);
    }

    private void AddFolderToZip(ZipOutputStream zip, string folder, string prefix, string? password)
    {
        foreach (var file in Directory.GetFiles(folder))
        {
            _ct.ThrowIfCancellationRequested();
            var entryName = ZipEntry.CleanName(
                string.IsNullOrEmpty(prefix) ? Path.GetFileName(file) : $"{prefix}/{Path.GetFileName(file)}");

            var entry = new ZipEntry(entryName)
            {
                DateTime = File.GetLastWriteTime(file),
                Size = new FileInfo(file).Length
            };
            if (!string.IsNullOrEmpty(password))
                entry.AESKeySize = 256;

            zip.PutNextEntry(entry);
            using var fs = File.OpenRead(file);
            var buf = new byte[81920];
            StreamUtils.Copy(fs, zip, buf);
            zip.CloseEntry();
            Report($"ZIP: {Path.GetFileName(file)}", "Sıkıştırma");
        }

        foreach (var dir in Directory.GetDirectories(folder))
        {
            _ct.ThrowIfCancellationRequested();
            var subPrefix = string.IsNullOrEmpty(prefix)
                ? Path.GetFileName(dir)
                : $"{prefix}/{Path.GetFileName(dir)}";
            AddFolderToZip(zip, dir, subPrefix, password);
        }
    }

    private async Task ScanSystemAsync(string itemId, string destDir)
    {
        var patterns = itemId == "sqlite"
            ? new[] { "*.db", "*.sqlite", "*.sqlite3" }
            : new[] { "*.mdb", "*.accdb" };

        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (var pat in patterns)
            {
                _ct.ThrowIfCancellationRequested();
                IEnumerable<string> files;
                try { files = Directory.GetFiles(drive.RootDirectory.FullName, pat, SearchOption.AllDirectories); }
                catch { continue; }

                foreach (var f in files)
                {
                    if (f.Contains(@"\Windows\") || f.Contains(@"\$Recycle")) continue;
                    _ct.ThrowIfCancellationRequested();
                    var dest = Path.Combine(destDir, Path.GetFileName(f));
                    if (File.Exists(dest))
                        dest = Path.Combine(destDir,
                            $"{Path.GetFileNameWithoutExtension(f)}_{Guid.NewGuid():N}{Path.GetExtension(f)}");
                    await CopyFileAsync(f, dest, "Veritabanı");
                }
            }
        }
    }

    private async Task<long> EstimateTotalSizeAsync()
    {
        return await Task.Run(() =>
        {
            long total = 0;
            foreach (var item in _options.SelectedItems)
            {
                var path = Environment.ExpandEnvironmentVariables(item.Path);
                if (Directory.Exists(path))
                    total += GetDirSize(path);
            }
            return total;
        });
    }

    private static long GetDirSize(string path)
    {
        try
        {
            return new DirectoryInfo(path)
                .GetFiles("*", SearchOption.AllDirectories)
                .Sum(f => { try { return f.Length; } catch { return 0L; } });
        }
        catch { return 0; }
    }

    private void UpdateSpeed()
    {
        var elapsed = _sw.Elapsed.TotalSeconds;
        if (elapsed > 0 && _state.CopiedBytes > 0)
        {
            _state.SpeedMBps = _state.CopiedBytes / (1024.0 * 1024) / elapsed;
            var remaining = _state.TotalBytes - _state.CopiedBytes;
            if (_state.SpeedMBps > 0)
                _state.Estimated = TimeSpan.FromSeconds(remaining / (1024.0 * 1024) / _state.SpeedMBps);
        }
        _state.Elapsed = _sw.Elapsed;
    }

    private void Report(string file, string category)
    {
        _state.CurrentFile = file;
        _state.CurrentCategory = category;
        _progress?.Report(CloneState());
    }

    private BackupProgress CloneState() => new()
    {
        TotalItems      = _state.TotalItems,
        CompletedItems  = _state.CompletedItems,
        TotalBytes      = _state.TotalBytes,
        CopiedBytes     = _state.CopiedBytes,
        CurrentFile     = _state.CurrentFile,
        CurrentCategory = _state.CurrentCategory,
        SpeedMBps       = _state.SpeedMBps,
        Elapsed         = _state.Elapsed,
        Estimated       = _state.Estimated,
        Errors          = _state.Errors.ToList(),
        Warnings        = _state.Warnings.ToList(),
    };

    private static string SanitizeName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
