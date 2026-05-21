using System.IO;
using System.Security.Principal;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class BackupPreflightService
{
    public static async Task<List<PreflightCheckItem>> RunAsync(
        string destinationPath,
        IEnumerable<BackupItem> selectedItems,
        bool useVss,
        bool useZip,
        bool usePassword,
        bool isIncremental,
        CancellationToken ct = default)
    {
        var selected = selectedItems.ToList();
        var checks = new List<PreflightCheckItem>();

        checks.Add(selected.Count > 0
            ? Ok("Seçim", $"{selected.Count} öğe seçili.")
            : Error("Seçim", "Yedeklenecek en az bir öğe seçilmeli."));

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            checks.Add(Error("Hedef", "Yedek hedef klasörü seçilmedi."));
        }
        else
        {
            checks.Add(CheckDestination(destinationPath));
        }

        if (usePassword && !useZip)
            checks.Add(Warning("Şifreleme", "Şifreleme yalnızca ZIP yedeklerde uygulanır."));
        else if (useZip && usePassword)
            checks.Add(Ok("Şifreleme", "ZIP AES-256 ile korunacak."));
        else if (useZip)
            checks.Add(Warning("Şifreleme", "ZIP oluşturulacak ancak parola kullanılmayacak."));
        else
            checks.Add(Ok("Paketleme", "Yedek klasör olarak üretilecek."));

        var requiresAdmin = selected.Any(i => i.Id == "winDrivers" || i.Id == "wifiProfiles" || i.RequiresVss);
        if (requiresAdmin || useVss)
        {
            checks.Add(IsAdministrator()
                ? Ok("Yönetici yetkisi", "VSS ve sistem araçları için yetki uygun.")
                : Warning("Yönetici yetkisi", "VSS/sürücü/WiFi işlemleri için yönetici olarak çalıştırmak gerekebilir."));
        }

        if (selected.Any(i => i.Path.Contains("OneDrive", StringComparison.OrdinalIgnoreCase)))
            checks.Add(Warning("OneDrive", "Çevrimiçi-only dosyalar yerel diskte yoksa kopyalanamayabilir."));

        if (selected.Any(i => i.RequiresVss) && !useVss)
            checks.Add(Warning("Açık dosyalar", "Outlook/veritabanı gibi açık dosyalar için VSS kapalı."));

        if (isIncremental)
            checks.Add(Ok("Artımlı yedek", "Değişmeyen dosyalar boyut ve tarih bilgisine göre atlanacak."));

        if (!string.IsNullOrWhiteSpace(destinationPath) && selected.Count > 0)
        {
            try
            {
                var estimated = await DiskSpaceChecker.EstimateBackupSize(selected, ct);
                var space = DiskSpaceChecker.CheckSpace(destinationPath, estimated);
                checks.Add(space.IsSufficient
                    ? Ok("Disk alanı", $"{space.RequiredText} tahmini veri için hedef uygun. Boş alan: {space.AvailableText}.")
                    : Error("Disk alanı", $"{space.RequiredText} gerekli, kullanılabilir alan: {space.AvailableText}."));

                var format = GetDriveFormat(destinationPath);
                if (format.Equals("FAT32", StringComparison.OrdinalIgnoreCase) && estimated > 4L * 1024 * 1024 * 1024)
                    checks.Add(Warning("Dosya sistemi", "FAT32 hedefte 4 GB üzeri ZIP/dosya sorun çıkarabilir."));
            }
            catch (Exception ex)
            {
                checks.Add(Warning("Disk alanı", $"Tahmin yapılamadı: {ex.Message}"));
            }
        }

        return checks;
    }

    private static PreflightCheckItem CheckDestination(string path)
    {
        try
        {
            if (NetworkShareHelper.IsUncPath(path))
                return Ok("Hedef", "UNC/ağ hedefi algılandı; yazma testi yedekleme sırasında yapılacak.");

            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return Error("Hedef", "Hedef sürücü bulunamadı.");

            return Ok("Hedef", path);
        }
        catch (Exception ex)
        {
            return Error("Hedef", ex.Message);
        }
    }

    private static string GetDriveFormat(string path)
    {
        try
        {
            if (NetworkShareHelper.IsUncPath(path)) return "";
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return string.IsNullOrEmpty(root) ? "" : new DriveInfo(root).DriveFormat;
        }
        catch { return ""; }
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private static PreflightCheckItem Ok(string title, string detail) => new() { Title = title, Detail = detail, Status = PreflightStatus.Ok };
    private static PreflightCheckItem Warning(string title, string detail) => new() { Title = title, Detail = detail, Status = PreflightStatus.Warning };
    private static PreflightCheckItem Error(string title, string detail) => new() { Title = title, Detail = detail, Status = PreflightStatus.Error };
}
