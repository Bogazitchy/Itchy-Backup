using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class BackupValidationService
{
    public static async Task<BackupValidationResult> ValidateAsync(string backupPath, string? zipPassword, CancellationToken ct)
    {
        var result = new BackupValidationResult();
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            Add(result, "Hata", "Yedek seçilmedi.");
            return Finish(result);
        }

        if (File.Exists(backupPath) && backupPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            await ValidateZipAsync(result, backupPath, zipPassword, ct);
        else if (Directory.Exists(backupPath))
            await ValidateFolderAsync(result, backupPath, ct);
        else
            Add(result, "Hata", "Yedek dosyası veya klasörü bulunamadı.");

        return Finish(result);
    }

    private static async Task ValidateFolderAsync(BackupValidationResult result, string folder, CancellationToken ct)
    {
        var manifest = BackupManifestService.TryRead(folder);
        if (manifest == null) Add(result, "Uyarı", "backup_manifest.json bulunamadı.");
        else Add(result, "Bilgi", $"{manifest.FileCount} dosyalı manifest okundu.");

        if (!File.Exists(Path.Combine(folder, "backup_report.html")))
            Add(result, "Uyarı", "HTML müşteri/servis raporu bulunamadı.");

        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("backup_log_", StringComparison.OrdinalIgnoreCase))
            .Take(1)
            .Any();
        if (!files) Add(result, "Hata", "Yedek içinde dosya bulunamadı.");

        if (File.Exists(Path.Combine(folder, "checksums.sha256")))
        {
            var verification = await ChecksumService.VerifyManifestAsync(folder, null, ct);
            if (verification.IsAllValid)
                Add(result, "Bilgi", verification.Summary);
            else
                Add(result, "Hata", $"{verification.Summary}; eksik/değişmiş/hatalı kayıt var.");
        }
        else
        {
            Add(result, "Uyarı", "checksums.sha256 bulunamadı; bütünlük doğrulaması yapılamadı.");
        }
    }

    private static Task ValidateZipAsync(BackupValidationResult result, string zipPath, string? zipPassword, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var fs = File.OpenRead(zipPath);
            using var zip = new ZipFile(fs);
            if (!string.IsNullOrEmpty(zipPassword)) zip.Password = zipPassword;
            var count = 0;
            foreach (ZipEntry entry in zip)
            {
                ct.ThrowIfCancellationRequested();
                if (entry.IsDirectory) continue;
                count++;
                if (count > 10) break;
                using var stream = zip.GetInputStream(entry);
                var buffer = new byte[Math.Min(8192, Math.Max(1, (int)Math.Min(entry.Size, 8192)))];
                _ = stream.Read(buffer, 0, buffer.Length);
            }
            if (count == 0) Add(result, "Hata", "ZIP içinde dosya bulunamadı.");
            else Add(result, "Bilgi", $"ZIP açma testi başarılı; ilk {count} dosya okunabildi.");
        }, ct);
    }

    private static BackupValidationResult Finish(BackupValidationResult result)
    {
        var errors = result.Issues.Count(i => i.Severity == "Hata");
        var warnings = result.Issues.Count(i => i.Severity == "Uyarı");
        result.Score = Math.Max(0, 100 - errors * 45 - warnings * 15);
        result.Status = errors > 0 ? "Riskli" : warnings > 0 ? "Uyarılı" : "Sağlıklı";
        result.Summary = $"{result.Status} • Puan {result.Score}/100 • {errors} hata • {warnings} uyarı";
        return result;
    }

    private static void Add(BackupValidationResult result, string severity, string message)
        => result.Issues.Add(new BackupValidationIssue { Severity = severity, Message = message });
}
