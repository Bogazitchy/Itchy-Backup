using System.IO;
using System.Security.Cryptography;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class ChecksumService
{
    /// <summary>Yedek klasöründe SHA-256 manifest dosyası oluşturur.</summary>
    public static async Task<string> WriteManifestAsync(
        string folder,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        var manifestPath = Path.Combine(folder, "checksums.sha256");
        await using var writer = new StreamWriter(manifestPath, false);
        await writer.WriteLineAsync($"# SHA-256 manifest - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        await writer.WriteLineAsync($"# Folder: {folder}");
        await writer.WriteLineAsync();

        var allFiles = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("checksums.sha256") && !f.EndsWith(".log"))
            .ToList();

        int i = 0;
        foreach (var file in allFiles)
        {
            ct.ThrowIfCancellationRequested();
            i++;
            var rel = Path.GetRelativePath(folder, file);
            progress?.Report($"Hash: {rel} ({i}/{allFiles.Count})");
            try
            {
                var hash = await ComputeSha256Async(file, ct);
                await writer.WriteLineAsync($"{hash}  {rel}");
            }
            catch (Exception ex)
            {
                await writer.WriteLineAsync($"# HATA: {rel} - {ex.Message}");
            }
        }
        return manifestPath;
    }

    /// <summary>Yedek klasöründe checksum doğrulaması yapar ve raporu döner.</summary>
    public static async Task<VerificationReport> VerifyManifestAsync(
        string folder,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        var report = new VerificationReport { FolderPath = folder };
        var manifestPath = Path.Combine(folder, "checksums.sha256");
        if (!File.Exists(manifestPath))
        {
            report.HasManifest = false;
            return report;
        }
        report.HasManifest = true;

        var lines = await File.ReadAllLinesAsync(manifestPath, ct);
        var entries = lines
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
            .ToList();

        int idx = 0;
        foreach (var line in entries)
        {
            ct.ThrowIfCancellationRequested();
            idx++;
            var parts = line.Split(new[] { "  " }, 2, StringSplitOptions.None);
            if (parts.Length != 2) continue;
            var expected = parts[0].Trim();
            var rel = parts[1].Trim();
            var fullPath = Path.Combine(folder, rel);

            progress?.Report($"Doğrulama: {rel} ({idx}/{entries.Count})");
            var entry = new VerificationEntry { FilePath = rel, ExpectedHash = expected };

            if (!File.Exists(fullPath))
            {
                entry.Status = VerificationStatus.Missing;
                report.Missing++;
            }
            else
            {
                try
                {
                    entry.ActualHash = await ComputeSha256Async(fullPath, ct);
                    if (entry.ActualHash.Equals(expected, StringComparison.OrdinalIgnoreCase))
                    {
                        entry.Status = VerificationStatus.Valid;
                        report.Valid++;
                    }
                    else
                    {
                        entry.Status = VerificationStatus.Modified;
                        report.Modified++;
                    }
                }
                catch (Exception ex)
                {
                    entry.Status = VerificationStatus.Error;
                    entry.ErrorMessage = ex.Message;
                    report.Errors++;
                }
            }
            report.Entries.Add(entry);
        }

        report.Total = entries.Count;
        return report;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        using var sha = SHA256.Create();
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        var hash = await sha.ComputeHashAsync(fs, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public class VerificationReport
{
    public string FolderPath { get; set; } = "";
    public bool HasManifest { get; set; }
    public int Total { get; set; }
    public int Valid { get; set; }
    public int Modified { get; set; }
    public int Missing { get; set; }
    public int Errors { get; set; }
    public List<VerificationEntry> Entries { get; set; } = new();

    public bool IsAllValid => HasManifest && Modified == 0 && Missing == 0 && Errors == 0;
    public string Summary => HasManifest
        ? $"{Valid}/{Total} dosya doğrulandı"
        : "Manifest bulunamadı";
}

public class VerificationEntry
{
    public string FilePath { get; set; } = "";
    public string ExpectedHash { get; set; } = "";
    public string ActualHash { get; set; } = "";
    public VerificationStatus Status { get; set; }
    public string ErrorMessage { get; set; } = "";

    public string StatusText => Status switch
    {
        VerificationStatus.Valid => "✓ Geçerli",
        VerificationStatus.Modified => "⚠ Değişmiş",
        VerificationStatus.Missing => "✕ Eksik",
        VerificationStatus.Error => "⚠ Hata",
        _ => "?"
    };
}

public enum VerificationStatus { Valid, Modified, Missing, Error }
