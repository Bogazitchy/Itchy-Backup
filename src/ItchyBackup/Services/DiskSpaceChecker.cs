using System.IO;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class DiskSpaceChecker
{
    /// <summary>Hedef sürücüde yeterli alan var mı kontrol eder. (gerekli + %10 tampon)</summary>
    public static SpaceCheckResult CheckSpace(string destinationPath, long requiredBytes)
    {
        var result = new SpaceCheckResult { RequiredBytes = requiredBytes };

        try
        {
            var fullPath = Path.GetFullPath(destinationPath);
            // UNC/network path ise farklı yaklaşım
            if (fullPath.StartsWith(@"\\"))
            {
                result.IsNetwork = true;
                result.DriveName = fullPath;
                result.AvailableBytes = -1; // Network paths için DriveInfo çalışmıyor
                result.IsSufficient = true; // Network için pre-check yapamıyoruz, devam ettir
                result.Message = "Ağ konumu - alan kontrolü yapılamadı";
                return result;
            }

            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
            {
                result.IsSufficient = false;
                result.Message = "Geçersiz hedef yol";
                return result;
            }

            var drive = new DriveInfo(root);
            result.DriveName = drive.Name;
            result.AvailableBytes = drive.AvailableFreeSpace;
            result.TotalBytes = drive.TotalSize;

            // %10 tampon
            var withBuffer = (long)(requiredBytes * 1.1);
            result.IsSufficient = drive.AvailableFreeSpace >= withBuffer;

            if (!result.IsSufficient)
            {
                var deficit = withBuffer - drive.AvailableFreeSpace;
                result.Message = $"Yetersiz alan! {FormatBytes(deficit)} daha gerekli.";
            }
            else
            {
                result.Message = $"Yeterli alan mevcut ({FormatBytes(drive.AvailableFreeSpace)} boş).";
            }
        }
        catch (Exception ex)
        {
            result.IsSufficient = false;
            result.Message = $"Disk kontrolü başarısız: {ex.Message}";
        }

        return result;
    }

    /// <summary>Seçili öğelerin toplam boyutunu hesaplar (paralel).</summary>
    public static async Task<long> EstimateBackupSize(IEnumerable<BackupItem> items, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            long total = 0;
            foreach (var item in items)
            {
                if (ct.IsCancellationRequested) break;
                var path = Environment.ExpandEnvironmentVariables(item.Path);
                if (Directory.Exists(path))
                    total += GetDirectorySize(path, ct);
                else if (File.Exists(path))
                {
                    try { total += new FileInfo(path).Length; } catch { }
                }
            }
            return total;
        }, ct);
    }

    private static long GetDirectorySize(string path, CancellationToken ct)
    {
        long size = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                if (ct.IsCancellationRequested) break;
                try { size += new FileInfo(f).Length; } catch { }
            }
        }
        catch { }
        return size;
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "?";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        if (bytes < 1024L * 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        return $"{bytes / (1024.0 * 1024 * 1024 * 1024):F2} TB";
    }
}

public class SpaceCheckResult
{
    public bool IsSufficient { get; set; }
    public bool IsNetwork { get; set; }
    public string DriveName { get; set; } = "";
    public long RequiredBytes { get; set; }
    public long AvailableBytes { get; set; }
    public long TotalBytes { get; set; }
    public string Message { get; set; } = "";

    public string RequiredText => DiskSpaceChecker.FormatBytes(RequiredBytes);
    public string AvailableText => AvailableBytes < 0 ? "?" : DiskSpaceChecker.FormatBytes(AvailableBytes);
}
