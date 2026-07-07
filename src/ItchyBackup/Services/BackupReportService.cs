using System.IO;
using System.Net;
using System.Text;

namespace ItchyBackup.Services;

public static class BackupReportService
{
    public static async Task<string> WriteHtmlReportAsync(BackupResult result, BackupOptions options, string folder)
    {
        var path = Path.Combine(folder, "backup_report.html");
        var manifest = BackupManifestService.TryRead(folder);
        var status = result.Errors.Count == 0
            ? (result.Warnings.Count == 0 ? "Başarılı" : "Başarılı, uyarılı")
            : "Hatalı";

        var healthScore = Math.Max(0, 100 - result.Errors.Count * 35 - result.Warnings.Count * 10);
        var rows = new[]
        {
            ("Durum", status),
            ("Sağlık puanı", $"{healthScore}/100"),
            ("Müşteri", string.IsNullOrWhiteSpace(options.CustomerName) ? "-" : options.CustomerName),
            ("Teknisyen", string.IsNullOrWhiteSpace(options.TechnicianName) ? "-" : options.TechnicianName),
            ("Yedek yolu", Mask(result.BackupPath, options.PrivacyMode)),
            ("Süre", result.Elapsed.ToString(@"mm\:ss")),
            ("Kategori", result.TotalCategories.ToString()),
            ("Kopyalanan dosya", result.FilesCopied.ToString()),
            ("Atlanan dosya", result.FilesSkipped.ToString()),
            ("Değişmeyen dosya", result.FilesUnchanged.ToString()),
            ("Toplam boyut", result.TotalBytesText),
            ("Yedek türü", result.IsIncremental ? "Artımlı" : "Tam"),
            ("Paketleme", options.UseZip ? "ZIP" : "Klasör"),
            ("Şifreleme", options.UsePassword ? "Açık" : "Kapalı"),
            ("Checksum", result.VerificationReport?.Summary ?? "Yok"),
            ("Manifest", manifest != null ? $"{manifest.FileCount} dosya kaydı" : "Yok"),
            ("Bilgisayar", options.PrivacyMode ? MaskMachine(Environment.MachineName) : Environment.MachineName),
            ("Kullanıcı", options.PrivacyMode ? "Windows kullanıcısı" : Environment.UserName),
        };

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><title>Itchy Backup Raporu</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;background:#edf7f2;color:#10251b;margin:32px}main{max-width:940px;margin:auto;background:white;border:1px solid #cfe5da;border-radius:10px;padding:28px}h1{margin:0 0 4px}.muted{color:#667085}.grid{display:grid;grid-template-columns:220px 1fr;border-top:1px solid #eef2f7;margin-top:20px}.grid div{padding:10px;border-bottom:1px solid #eef2f7}.key{font-weight:600;color:#214d3a}.ok{color:#067647}.bad{color:#b42318}.warn{color:#b54708}pre{white-space:pre-wrap;background:#07110d;color:#f2f4f7;padding:14px;border-radius:8px}table{width:100%;border-collapse:collapse;margin-top:14px}th,td{padding:8px;border-bottom:1px solid #eef2f7;text-align:left;font-size:13px}th{color:#214d3a}</style>");
        sb.AppendLine("</head><body><main>");
        sb.AppendLine($"<h1>Itchy Backup Raporu</h1><div class=\"muted\">{WebUtility.HtmlEncode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
        sb.AppendLine("<section class=\"grid\">");
        foreach (var (key, value) in rows)
            sb.AppendLine($"<div class=\"key\">{WebUtility.HtmlEncode(key)}</div><div>{WebUtility.HtmlEncode(value)}</div>");
        sb.AppendLine("</section>");

        if (result.Warnings.Count > 0)
            sb.AppendLine($"<h2>Uyarılar</h2><pre>{WebUtility.HtmlEncode(string.Join(Environment.NewLine, result.Warnings))}</pre>");
        if (result.Errors.Count > 0)
            sb.AppendLine($"<h2>Hatalar</h2><pre>{WebUtility.HtmlEncode(string.Join(Environment.NewLine, result.Errors))}</pre>");

        if (manifest?.SelectedItems.Count > 0)
        {
            sb.AppendLine("<h2>Seçilen Öğeler</h2><table><thead><tr><th>Kategori</th><th>Öğe</th><th>Kaynak</th></tr></thead><tbody>");
            foreach (var item in manifest.SelectedItems)
                sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(item.Category)}</td><td>{WebUtility.HtmlEncode(item.Label)}</td><td>{WebUtility.HtmlEncode(Mask(item.SourcePath, options.PrivacyMode))}</td></tr>");
            sb.AppendLine("</tbody></table>");
        }

        if (result.VerificationReport?.Entries.Any(e => e.Status != VerificationStatus.Valid) == true)
        {
            sb.AppendLine("<h2>Doğrulama Sorunları</h2><table><thead><tr><th>Durum</th><th>Dosya</th><th>Hata</th></tr></thead><tbody>");
            foreach (var entry in result.VerificationReport.Entries.Where(e => e.Status != VerificationStatus.Valid).Take(200))
                sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(entry.StatusText)}</td><td>{WebUtility.HtmlEncode(Mask(entry.FilePath, options.PrivacyMode))}</td><td>{WebUtility.HtmlEncode(entry.ErrorMessage)}</td></tr>");
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("</main></body></html>");

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string Mask(string value, bool enabled)
    {
        if (!enabled || string.IsNullOrWhiteSpace(value)) return value;
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
            value = value.Replace(userProfile, @"Kullanıcı", StringComparison.OrdinalIgnoreCase);
        value = value.Replace(Environment.UserName, "Kullanıcı", StringComparison.OrdinalIgnoreCase);
        return value;
    }

    private static string MaskMachine(string value)
        => string.IsNullOrWhiteSpace(value) ? "" : $"{value[..Math.Min(3, value.Length)]}***";
}
