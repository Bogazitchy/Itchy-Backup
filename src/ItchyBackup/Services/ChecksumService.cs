using System.IO;
using System.Security.Cryptography;
using System.Text;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class ChecksumService
{
    private const string ManifestFileName = "itchy_checksums.sha256";

    public static async Task<string> ComputeFileHashAsync(
        string filePath,
        string algorithm = "SHA256",
        CancellationToken ct = default)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81920, useAsync: true);
        using HashAlgorithm ha = algorithm.ToUpperInvariant() switch
        {
            "MD5"    => MD5.Create(),
            "SHA1"   => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            _        => SHA256.Create()
        };
        var hash = await Task.Run(() => ha.ComputeHash(stream), ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Yedek klasöründeki tüm dosyalar için SHA256 manifest dosyası oluşturur.
    /// </summary>
    public static async Task WriteManifestAsync(
        string backupRootDir,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var manifestPath = Path.Combine(backupRootDir, ManifestFileName);
        var sb = new StringBuilder();
        sb.AppendLine($"# Itchy Backup SHA256 Manifest");
        sb.AppendLine($"# Oluşturulma: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# Dizin: {backupRootDir}");
        sb.AppendLine();

        var files = Directory.GetFiles(backupRootDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(ManifestFileName))
            .ToList();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(backupRootDir, file);
            progress?.Report($"Checksum: {relative}");
            try
            {
                var hash = await ComputeFileHashAsync(file, "SHA256", ct);
                sb.AppendLine($"{hash}  {relative}");
                LogService.Info($"Checksum: {hash}  {relative}");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"# HATA: {relative} - {ex.Message}");
                LogService.Error($"Checksum hesaplanamadı: {relative}", ex);
            }
        }

        await File.WriteAllTextAsync(manifestPath, sb.ToString(), ct);
        LogService.Info($"Checksum manifest yazıldı: {manifestPath}");
    }

    /// <summary>
    /// Manifest dosyasını okuyarak tüm dosyaları doğrular.
    /// </summary>
    public static async Task<List<ChecksumResult>> VerifyManifestAsync(
        string backupRootDir,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var results = new List<ChecksumResult>();
        var manifestPath = Path.Combine(backupRootDir, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            LogService.Warn("Checksum manifest bulunamadı, doğrulama atlanıyor.");
            return results;
        }

        var lines = await File.ReadAllLinesAsync(manifestPath, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var parts = line.Split("  ", 2);
            if (parts.Length != 2) continue;

            var expected = parts[0].Trim();
            var relativePath = parts[1].Trim();
            var fullPath = Path.Combine(backupRootDir, relativePath);

            ct.ThrowIfCancellationRequested();
            progress?.Report($"Doğrulanıyor: {relativePath}");

            var result = new ChecksumResult
            {
                FilePath = fullPath,
                Algorithm = "SHA256",
                OriginalHash = expected
            };

            try
            {
                result.VerifiedHash = await ComputeFileHashAsync(fullPath, "SHA256", ct);
                if (!result.IsValid)
                    LogService.Error($"Checksum uyuşmazlığı: {relativePath}");
                else
                    LogService.Info($"Doğrulandı: {relativePath}");
            }
            catch (Exception ex)
            {
                result.VerifiedHash = "HATA";
                LogService.Error($"Doğrulama hatası: {relativePath}", ex);
            }

            results.Add(result);
        }

        return results;
    }
}
