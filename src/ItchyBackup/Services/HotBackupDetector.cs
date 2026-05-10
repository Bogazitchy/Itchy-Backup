using System.Diagnostics;
using System.ServiceProcess;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class HotBackupDetector
{
    /// <summary>Yedeklenecek öğelere göre çalışan servis ve uygulamaları tespit eder.</summary>
    public static List<HotBackupWarning> DetectRunningServices(IEnumerable<string> selectedItemIds)
    {
        var warnings = new List<HotBackupWarning>();
        var ids = selectedItemIds.ToHashSet();

        // ── Tarayıcı kontrolleri ──
        if (ids.Contains("chrome") && IsProcessRunning("chrome"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Google Chrome",
                Message = "Chrome çalışıyor — profil dosyaları kilitli olabilir. Kapatmanız önerilir.",
                Severity = HotWarningSeverity.High
            });

        if (ids.Contains("firefox") && IsProcessRunning("firefox"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Mozilla Firefox",
                Message = "Firefox çalışıyor — places.sqlite ve diğer profil dosyaları kilitli. Kapatmanız önerilir.",
                Severity = HotWarningSeverity.High
            });

        if (ids.Contains("edge") && IsProcessRunning("msedge"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Microsoft Edge",
                Message = "Edge çalışıyor — profil verileri kilitli olabilir. Kapatmanız önerilir.",
                Severity = HotWarningSeverity.High
            });

        if (ids.Contains("opera") && (IsProcessRunning("opera") || IsProcessRunning("opera_gx")))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Opera",
                Message = "Opera çalışıyor — profil dosyaları kilitli olabilir.",
                Severity = HotWarningSeverity.High
            });

        if (ids.Contains("brave") && IsProcessRunning("brave"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Brave",
                Message = "Brave çalışıyor — profil verileri kilitli olabilir.",
                Severity = HotWarningSeverity.High
            });

        if (ids.Contains("vivaldi") && IsProcessRunning("vivaldi"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Vivaldi",
                Message = "Vivaldi çalışıyor — profil verileri kilitli olabilir.",
                Severity = HotWarningSeverity.High
            });

        // ── Outlook ──
        if ((ids.Contains("pst") || ids.Contains("ost")) && IsProcessRunning("outlook"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Microsoft Outlook",
                Message = "Outlook çalışıyor. PST/OST dosyaları kilitli — VSS ile kopyalanacak veya hata verebilir.",
                Severity = HotWarningSeverity.High
            });

        // ── Veritabanları ──
        if (ids.Contains("sqlserver") && IsServiceRunning("MSSQLSERVER"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "SQL Server",
                Message = "SQL Server servisi çalışıyor. MDF dosyaları kilitli — VSS gerekli.",
                Severity = HotWarningSeverity.Critical
            });

        if (ids.Contains("firebird") &&
            (IsServiceRunning("FirebirdServerDefaultInstance") || IsProcessRunning("firebird")))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Firebird",
                Message = "Firebird servisi çalışıyor. FDB/GDB dosyaları kilitli — VSS gerekli.",
                Severity = HotWarningSeverity.Critical
            });

        // ── Sanal makineler ──
        if (ids.Contains("vmware") && IsProcessRunning("vmware-vmx"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "VMware",
                Message = "VMware sanal makinesi çalışıyor. VMDK dosyaları kilitli — kapatmanız gerekir.",
                Severity = HotWarningSeverity.Critical
            });

        if (ids.Contains("vbox") && IsProcessRunning("VirtualBox"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "VirtualBox",
                Message = "VirtualBox VM çalışıyor. VDI dosyaları kilitli — kapatmanız gerekir.",
                Severity = HotWarningSeverity.Critical
            });

        if (ids.Contains("hyperv") && IsServiceRunning("vmms"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Hyper-V",
                Message = "Hyper-V servisi çalışıyor. VHDX dosyaları kilitli olabilir.",
                Severity = HotWarningSeverity.High
            });

        // ── Bulut ──
        if (ids.Contains("onedrive") && IsProcessRunning("OneDrive"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "OneDrive",
                Message = "OneDrive çalışıyor — sync sırasında dosya kilitleri olabilir.",
                Severity = HotWarningSeverity.Low
            });

        if (ids.Contains("dropbox") && IsProcessRunning("Dropbox"))
            warnings.Add(new HotBackupWarning
            {
                ServiceName = "Dropbox",
                Message = "Dropbox çalışıyor — sync sırasında dosya kilitleri olabilir.",
                Severity = HotWarningSeverity.Low
            });

        return warnings;
    }

    private static bool IsProcessRunning(string processName)
    {
        try { return Process.GetProcessesByName(processName).Any(); }
        catch { return false; }
    }

    private static bool IsServiceRunning(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            return sc.Status == ServiceControllerStatus.Running;
        }
        catch { return false; }
    }
}

public enum HotWarningSeverity { Low, High, Critical }

public class HotBackupWarning
{
    public string ServiceName { get; set; } = "";
    public string Message { get; set; } = "";
    public HotWarningSeverity Severity { get; set; }
}
