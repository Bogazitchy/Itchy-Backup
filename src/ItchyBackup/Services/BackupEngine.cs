using System.IO;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public class BackupOptions
{
    public string DestinationRoot { get; set; } = "";
    public List<string> AdditionalDestinations { get; set; } = new();
    public bool UseZip { get; set; } = false;
    public bool UsePassword { get; set; } = false;
    public string? Password { get; set; }
    public bool UseVss { get; set; } = true;
    public bool VerifyChecksum { get; set; } = true;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Normal;
    public List<BackupItem> SelectedItems { get; set; } = new();
    public bool IncludeMachineInfo { get; set; } = true;
    public bool IsIncremental { get; set; } = false;
    public string IncrementalBaseFolder { get; set; } = "";
    public bool UseNetworkCredentials { get; set; } = false;
    public string NetworkUsername { get; set; } = "";
    public string NetworkPassword { get; set; } = "";
    public string NetworkDomain { get; set; } = "";
    public RotationPolicy RotationPolicy { get; set; } = RotationPolicy.None;
    public int RotationKeepLastN { get; set; } = 5;
    public int RotationDeleteOlderThanDays { get; set; } = 30;
    public int ParallelCopyThreads { get; set; } = 4;
}

public class BackupResult
{
    public bool Success { get; set; }
    public string BackupPath { get; set; } = "";
    public TimeSpan Elapsed { get; set; }
    public int TotalCategories { get; set; }
    public int FilesCopied { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesUnchanged { get; set; }
    public long TotalBytes { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public VerificationReport? VerificationReport { get; set; }
    public bool IsIncremental { get; set; }
    public string IncrementalBase { get; set; } = "";
}

public class BackupEngine
{
    private readonly BackupOptions _options;
    private readonly IProgress<BackupProgress>? _progress;
    private readonly CancellationToken _ct;
    private readonly BackupProgress _state = new();
    private readonly Stopwatch _sw = new();
    private readonly object _stateLock = new();
    private string? _incrementalBase;
    private string _workFolder = "";

    public BackupResult Result { get; } = new();

    public BackupEngine(BackupOptions options, IProgress<BackupProgress>? progress, CancellationToken ct)
    {
        _options = options;
        _progress = progress;
        _ct = ct;
    }

    public async Task<BackupResult> RunAsync()
    {
        _sw.Start();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Ağ paylaşımına bağlan (kimlik bilgileri varsa)
        if (_options.UseNetworkCredentials && NetworkShareHelper.IsUncPath(_options.DestinationRoot))
        {
            try
            {
                LogService.Info($"Ağ paylaşımına bağlanılıyor: {_options.DestinationRoot}");
                NetworkShareHelper.Connect(_options.DestinationRoot,
                    _options.NetworkUsername, _options.NetworkPassword, _options.NetworkDomain);
                LogService.Info("Ağ bağlantısı başarılı.");
            }
            catch (Exception ex)
            {
                LogService.Error("Ağ bağlantısı kurulamadı", ex);
                throw new InvalidOperationException($"SMB bağlantısı başarısız: {ex.Message}", ex);
            }
        }

        // Klasör adı: bilgisayar/kullanıcı bilgisi ile
        string folderName;
        if (_options.IncludeMachineInfo)
        {
            var machine = SanitizeName(Environment.MachineName);
            var user = SanitizeName(Environment.UserName);
            folderName = $"Yedek_{machine}_{user}_{timestamp}";
        }
        else
        {
            folderName = $"Yedek_{timestamp}";
        }

        var backupFolder = Path.Combine(_options.DestinationRoot, folderName);
        Directory.CreateDirectory(backupFolder);
        Result.BackupPath = backupFolder;

        LogService.InitializeForBackup(backupFolder);
        LogService.Info("=== Itchy Backup başladı ===");
        LogService.Info($"Hedef: {backupFolder}");
        LogService.Info($"Bilgisayar: {Environment.MachineName} | Kullanıcı: {Environment.UserName}");
        LogService.Info($"ZIP: {_options.UseZip}, Şifre: {_options.UsePassword}, VSS: {_options.UseVss}");

        _state.TotalItems = _options.SelectedItems.Count;
        _state.TotalBytes = await EstimateTotalSizeAsync();
        if (_state.TotalBytes <= 0) _state.TotalBytes = 1;
        Result.TotalBytes = _state.TotalBytes;

        // Toplam dosya sayısını da hesapla
        _state.TotalFiles = await CountTotalFilesAsync();
        if (_state.TotalFiles <= 0) _state.TotalFiles = 1;

        Report("Yedekleme başlıyor...", "");

        using var vss = _options.UseVss ? new VssService() : null;

        string workFolder = _options.UseZip
            ? Path.Combine(Path.GetTempPath(), $"ItchyBackup_{timestamp}")
            : backupFolder;

        if (_options.UseZip)
            Directory.CreateDirectory(workFolder);

        _workFolder = workFolder;

        // Artımlı yedekleme için önceki yedek klasörünü bul
        if (_options.IsIncremental)
        {
            if (!string.IsNullOrEmpty(_options.IncrementalBaseFolder) && Directory.Exists(_options.IncrementalBaseFolder))
            {
                _incrementalBase = _options.IncrementalBaseFolder;
                LogService.Info($"Artımlı baz (manuel): {_incrementalBase}");
            }
            else
            {
                _incrementalBase = FindLastBackupFolder(backupFolder);
                if (_incrementalBase != null)
                    LogService.Info($"Artımlı baz (otomatik): {_incrementalBase}");
                else
                    LogService.Info("Artımlı: önceki yedek bulunamadı, tam yedek yapılıyor.");
            }
        }

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

        if (_options.UseZip)
        {
            Report("ZIP oluşturuluyor...", "Sıkıştırma");
            var zipPath = Path.Combine(backupFolder,
                $"{folderName}{(_options.UsePassword ? "_sifireli" : "")}.zip");
            await CreateZipAsync(workFolder, zipPath);
            try { Directory.Delete(workFolder, true); } catch { }
            LogService.Info($"ZIP oluşturuldu: {zipPath}");
        }

        // Bilgisayar bilgi dosyası
        await WriteSystemInfoAsync(backupFolder);

        // Checksum
        if (_options.VerifyChecksum)
        {
            Report("Checksum hesaplanıyor...", "Doğrulama");
            var prog = new Progress<string>(s => Report(s, "Checksum"));
            await ChecksumService.WriteManifestAsync(backupFolder, prog, _ct);
            LogService.Info("Checksum manifest yazıldı.");

            // Otomatik doğrulama
            Report("Yedek doğrulanıyor...", "Doğrulama");
            Result.VerificationReport = await ChecksumService.VerifyManifestAsync(backupFolder, prog, _ct);
            LogService.Info($"Doğrulama: {Result.VerificationReport.Summary}");
        }

        // Rotasyonu ana hedefte uygula
        ApplyRotation(_options.DestinationRoot);

        // Ek hedeflere kopyala
        foreach (var extraDest in _options.AdditionalDestinations.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            Report("Ek hedefe kopyalanıyor...", "Çoklu Hedef");
            try
            {
                LogService.Info($"Ek hedef kopyalanıyor: {extraDest}");
                Directory.CreateDirectory(extraDest);
                var extraFolder = Path.Combine(extraDest, folderName);
                await CopyFolderToExtraDestAsync(backupFolder, extraFolder);
                ApplyRotation(extraDest);
                LogService.Info($"Ek hedef tamamlandı: {extraFolder}");
            }
            catch (Exception ex)
            {
                Result.Warnings.Add($"Ek hedef başarısız ({extraDest}): {ex.Message}");
                LogService.Error($"Ek hedef hatası: {extraDest}", ex);
            }
        }

        _sw.Stop();
        _state.Elapsed = _sw.Elapsed;
        _state.CopiedBytes = _state.TotalBytes;
        Report("Tamamlandı!", "");

        // Ağ bağlantısını kes
        if (_options.UseNetworkCredentials && NetworkShareHelper.IsUncPath(_options.DestinationRoot))
            NetworkShareHelper.Disconnect(_options.DestinationRoot);

        Result.Success = _state.Errors.Count == 0;
        Result.Elapsed = _sw.Elapsed;
        Result.TotalCategories = _state.TotalItems;
        Result.FilesCopied = _state.FilesCopied;
        Result.FilesSkipped = _state.FilesSkipped;
        Result.FilesUnchanged = _state.FilesUnchanged;
        Result.Errors = _state.Errors.ToList();
        Result.Warnings = _state.Warnings.ToList();
        Result.IsIncremental = _options.IsIncremental;
        Result.IncrementalBase = _incrementalBase != null ? Path.GetFileName(_incrementalBase) : "";

        LogService.Info($"=== Tamamlandı. Süre: {_sw.Elapsed:mm\\:ss} Dosya: {_state.FilesCopied} Değişmedi: {_state.FilesUnchanged} Hata: {_state.Errors.Count} ===");

        NotificationService.ShowSuccess("Itchy Backup",
            $"Yedekleme tamamlandı!\n{_state.TotalItems} kategori • {_state.FilesCopied} dosya • {_state.Errors.Count} hata • {_sw.Elapsed:mm\\:ss}");

        return Result;
    }

    private async Task WriteSystemInfoAsync(string folder)
    {
        var backupType = _options.IsIncremental
            ? $"Artımlı{(_incrementalBase != null ? $" (baz: {Path.GetFileName(_incrementalBase)})" : " (baz yok → tam)") }"
            : "Tam";
        var info = $@"# Itchy Backup - Sistem Bilgisi
Tarih: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Bilgisayar Adı: {Environment.MachineName}
Kullanıcı Adı: {Environment.UserName}
Domain: {Environment.UserDomainName}
İşletim Sistemi: {Environment.OSVersion}
.NET: {Environment.Version}
İşlemci: {Environment.ProcessorCount} çekirdek
Yedek Türü: {backupType}
Kapsayıcı: {(_options.UseZip ? "ZIP" : "Klasör")}
Şifreli: {(_options.UsePassword ? "Evet" : "Hayır")}
VSS Kullanıldı: {(_options.UseVss ? "Evet" : "Hayır")}
Ağ Hedefi: {(NetworkShareHelper.IsUncPath(_options.DestinationRoot) ? "Evet" : "Hayır")}
Kategori Sayısı: {_options.SelectedItems.Count}
";
        await File.WriteAllTextAsync(Path.Combine(folder, "system_info.txt"), info);
    }

    private async Task BackupItemAsync(BackupItem item, string destRoot, VssService? vss)
    {
        var safeLabel = SanitizeName(item.Label);
        var safeCat = SanitizeName(item.Parent?.Name ?? "Diger");
        var destDir = Path.Combine(destRoot, safeCat, safeLabel);
        Directory.CreateDirectory(destDir);

        // Özel sistem araçları
        if (item.Id == "winDrivers")
        {
            await ExportDriversAsync(destDir);
            return;
        }
        if (item.Id == "wifiProfiles")
        {
            await ExportWifiProfilesAsync(destDir);
            return;
        }

        var sourcePath = Environment.ExpandEnvironmentVariables(item.Path);
        if (vss != null && item.RequiresVss)
            sourcePath = vss.GetVssPath(sourcePath);

        if (sourcePath == "Sistem genelinde")
        {
            await ScanSystemAsync(item.Id, destDir);
        }
        else if (Directory.Exists(sourcePath))
        {
            await CopyDirectoryAsync(sourcePath, destDir, item.Parent?.Name ?? "");
        }
        else if (File.Exists(sourcePath))
        {
            // Tek dosya (özel klasörler için)
            var dest = Path.Combine(destDir, Path.GetFileName(sourcePath));
            await CopyFileAsync(sourcePath, dest, item.Parent?.Name ?? "");
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

        foreach (var dir in di.GetDirectories("*", SearchOption.AllDirectories))
        {
            _ct.ThrowIfCancellationRequested();
            var target = dir.FullName.Replace(source, dest);
            try { Directory.CreateDirectory(target); } catch { }
        }

        var files = di.GetFiles("*", SearchOption.AllDirectories);
        var sem = new SemaphoreSlim(_options.ParallelCopyThreads, _options.ParallelCopyThreads);

        var tasks = files.Select(async file =>
        {
            _ct.ThrowIfCancellationRequested();
            await sem.WaitAsync(_ct);
            try
            {
                var destFile = file.FullName.Replace(source, dest);

                // Artımlı: değişmeyen dosyaları atla
                if (_options.IsIncremental && _incrementalBase != null)
                {
                    var rel = Path.GetRelativePath(_workFolder, destFile);
                    var baseCopy = Path.Combine(_incrementalBase, rel);
                    if (File.Exists(baseCopy) && IsUnchanged(file.FullName, baseCopy))
                    {
                        lock (_stateLock) { _state.FilesUnchanged++; _state.CopiedBytes += file.Length; }
                        return;
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                Report($"{file.Name}", category);
                await CopyFileAsync(file.FullName, destFile, category);
            }
            catch (Exception ex)
            {
                lock (_stateLock) { _state.FilesSkipped++; }
                LogService.Warn($"Kopyalanamadı: {file.Name} - {ex.Message}");
            }
            finally { sem.Release(); }
        }).ToArray();

        await Task.WhenAll(tasks);
    }

    private static bool IsUnchanged(string srcPath, string lastCopyPath)
    {
        try
        {
            var src = new FileInfo(srcPath);
            var last = new FileInfo(lastCopyPath);
            return src.Length == last.Length
                && Math.Abs((src.LastWriteTimeUtc - last.LastWriteTimeUtc).TotalSeconds) < 2;
        }
        catch { return false; }
    }

    private string? FindLastBackupFolder(string currentBackupFolder)
    {
        try
        {
            var machine = SanitizeName(Environment.MachineName);
            var user = SanitizeName(Environment.UserName);
            return Directory.GetDirectories(_options.DestinationRoot, $"Yedek_{machine}_{user}_*")
                .Where(d => d != currentBackupFolder)
                .OrderByDescending(d => d)
                .FirstOrDefault();
        }
        catch { return null; }
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
                lock (_stateLock) { _state.CopiedBytes += read; }

                if ((DateTime.Now - lastReport).TotalMilliseconds > 200)
                {
                    UpdateSpeed();
                    _progress?.Report(CloneState());
                    lastReport = DateTime.Now;
                }
            }
            lock (_stateLock) { _state.FilesCopied++; }
            UpdateSpeed();
        }
        catch (IOException ex) when (ex.Message.Contains("used by another"))
        {
            lock (_stateLock)
            {
                _state.FilesSkipped++;
                _state.Warnings.Add($"Kilitli: {Path.GetFileName(src)}");
            }
            LogService.Warn($"Dosya kilitli, atlanıyor: {Path.GetFileName(src)}");
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

    private async Task ExportDriversAsync(string destDir)
    {
        try
        {
            Report("Windows sürücüleri dışa aktarılıyor...", "Sistem Araçları");
            var psi = new ProcessStartInfo("pnputil", $"/export-drivers * \"{destDir}\"")
            {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                await p.WaitForExitAsync(_ct);
                LogService.Info($"pnputil /export-drivers tamamlandı, çıkış kodu: {p.ExitCode}");
                if (p.ExitCode != 0)
                    _state.Warnings.Add($"Sürücü dışa aktarma uyarısı (kod: {p.ExitCode}) — yönetici yetkisi gerekebilir.");
            }
            _state.FilesCopied++;
        }
        catch (Exception ex)
        {
            LogService.Error("Sürücü dışa aktarma hatası", ex);
            _state.Warnings.Add($"Sürücü dışa aktarılamadı: {ex.Message}");
        }
    }

    private async Task ExportWifiProfilesAsync(string destDir)
    {
        try
        {
            Report("WiFi profilleri dışa aktarılıyor...", "Sistem Araçları");
            var psi = new ProcessStartInfo("netsh", $"wlan export profile key=clear folder=\"{destDir}\"")
            {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                await p.WaitForExitAsync(_ct);
                LogService.Info($"netsh wlan export tamamlandı, çıkış kodu: {p.ExitCode}");
            }
            _state.FilesCopied++;
        }
        catch (Exception ex)
        {
            LogService.Error("WiFi profili dışa aktarma hatası", ex);
            _state.Warnings.Add($"WiFi profilleri dışa aktarılamadı: {ex.Message}");
        }
    }

    private async Task CopyFolderToExtraDestAsync(string sourceFolder, string destFolder)
    {
        Directory.CreateDirectory(destFolder);
        foreach (var dir in Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories))
        {
            var target = dir.Replace(sourceFolder, destFolder);
            Directory.CreateDirectory(target);
        }
        var files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
        var sem = new SemaphoreSlim(_options.ParallelCopyThreads);
        var tasks = files.Select(async file =>
        {
            await sem.WaitAsync(_ct);
            try
            {
                var destFile = file.Replace(sourceFolder, destFolder);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                using var fsIn  = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, true);
                using var fsOut = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                await fsIn.CopyToAsync(fsOut, _ct);
            }
            finally { sem.Release(); }
        }).ToArray();
        await Task.WhenAll(tasks);
    }

    private void ApplyRotation(string destRoot)
    {
        if (_options.RotationPolicy == RotationPolicy.None) return;
        try
        {
            var machine = SanitizeName(Environment.MachineName);
            var user = SanitizeName(Environment.UserName);
            var dirs = Directory.GetDirectories(destRoot, $"Yedek_{machine}_{user}_*")
                .OrderByDescending(d => d).ToList();

            if (_options.RotationPolicy == RotationPolicy.KeepLastN)
            {
                foreach (var old in dirs.Skip(_options.RotationKeepLastN))
                {
                    LogService.Info($"Rotasyon: siliniyor {Path.GetFileName(old)}");
                    Directory.Delete(old, true);
                    Result.Warnings.Add($"Eski yedek silindi: {Path.GetFileName(old)}");
                }
            }
            else if (_options.RotationPolicy == RotationPolicy.DeleteOlderThanDays)
            {
                var cutoff = DateTime.Now.AddDays(-_options.RotationDeleteOlderThanDays);
                foreach (var old in dirs.Where(d => Directory.GetCreationTime(d) < cutoff))
                {
                    LogService.Info($"Rotasyon: siliniyor {Path.GetFileName(old)}");
                    Directory.Delete(old, true);
                    Result.Warnings.Add($"Eski yedek silindi: {Path.GetFileName(old)}");
                }
            }
        }
        catch (Exception ex) { LogService.Error("Rotasyon hatası", ex); }
    }

    private async Task<long> EstimateTotalSizeAsync()
    {
        return await DiskSpaceChecker.EstimateBackupSize(_options.SelectedItems, _ct);
    }

    private async Task<int> CountTotalFilesAsync()
    {
        return await Task.Run(() =>
        {
            int count = 0;
            foreach (var item in _options.SelectedItems)
            {
                var path = Environment.ExpandEnvironmentVariables(item.Path);
                if (Directory.Exists(path))
                {
                    try { count += Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count(); }
                    catch { }
                }
                else if (File.Exists(path)) count++;
            }
            return count;
        });
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
        TotalFiles      = _state.TotalFiles,
        FilesCopied     = _state.FilesCopied,
        FilesSkipped    = _state.FilesSkipped,
        FilesUnchanged  = _state.FilesUnchanged,
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
