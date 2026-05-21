using System.Net;
using System.Text;
using System.IO;

namespace ItchyBackup.Services;

public static class BackupReportService
{
    public static async Task<string> WriteHtmlReportAsync(BackupResult result, BackupOptions options, string folder)
    {
        var path = Path.Combine(folder, "backup_report.html");
        var rows = new[]
        {
            ("Durum", result.Success ? "Başarılı" : "Hatalı"),
            ("Yedek yolu", result.BackupPath),
            ("Süre", result.Elapsed.ToString(@"mm\:ss")),
            ("Kategori", result.TotalCategories.ToString()),
            ("Kopyalanan dosya", result.FilesCopied.ToString()),
            ("Atlanan dosya", result.FilesSkipped.ToString()),
            ("Değişmeyen dosya", result.FilesUnchanged.ToString()),
            ("Toplam boyut", result.TotalBytesText),
            ("Yedek türü", result.IsIncremental ? "Artımlı" : "Tam"),
            ("Paketleme", options.UseZip ? "ZIP" : "Klasör"),
            ("Şifreleme", options.UsePassword ? "Açık" : "Kapalı"),
            ("Checksum", result.VerificationReport?.Summary ?? "Yok")
        };

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><title>Itchy Backup Raporu</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;background:#f5f7fb;color:#182033;margin:32px}main{max-width:860px;margin:auto;background:white;border:1px solid #dfe5f2;border-radius:10px;padding:28px}h1{margin:0 0 4px}.muted{color:#667085}.grid{display:grid;grid-template-columns:220px 1fr;border-top:1px solid #eef2f7;margin-top:20px}.grid div{padding:10px;border-bottom:1px solid #eef2f7}.key{font-weight:600;color:#344054}.ok{color:#067647}.bad{color:#b42318}pre{white-space:pre-wrap;background:#101828;color:#f2f4f7;padding:14px;border-radius:8px}</style>");
        sb.AppendLine("</head><body><main>");
        sb.AppendLine($"<h1>Itchy Backup Raporu</h1><div class=\"muted\">{WebUtility.HtmlEncode(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</div>");
        sb.AppendLine("<section class=\"grid\">");
        foreach (var (key, value) in rows)
        {
            sb.AppendLine($"<div class=\"key\">{WebUtility.HtmlEncode(key)}</div><div>{WebUtility.HtmlEncode(value)}</div>");
        }
        sb.AppendLine("</section>");
        if (result.Warnings.Count > 0)
            sb.AppendLine($"<h2>Uyarılar</h2><pre>{WebUtility.HtmlEncode(string.Join(Environment.NewLine, result.Warnings))}</pre>");
        if (result.Errors.Count > 0)
            sb.AppendLine($"<h2>Hatalar</h2><pre>{WebUtility.HtmlEncode(string.Join(Environment.NewLine, result.Errors))}</pre>");
        sb.AppendLine("</main></body></html>");

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }
}
