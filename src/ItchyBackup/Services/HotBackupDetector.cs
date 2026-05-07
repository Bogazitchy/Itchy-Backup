using System.ServiceProcess;
using System.Diagnostics;

namespace ItchyBackup.Services;

public class HotBackupWarning
{
    public string ServiceName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string RelatedItemId { get; set; } = "";
    public string Message { get; set; } = "";
    public bool RequiresVss { get; set; } = true;
}

public static class HotBackupDetector
{
    private static readonly Dictionary<string, (string display, string itemId)> WatchedServices = new()
    {
        ["MSSQLSERVER"]         = ("SQL Server (Default)", "sqlserver"),
        ["MSSQL$SQLEXPRESS"]    = ("SQL Server Express", "sqlserver"),
        ["MSSQL$MSSQLSERVER"]   = ("SQL Server", "sqlserver"),
        ["MySQL80"]             = ("MySQL 8.0", "mysql_data"),
        ["MySQL57"]             = ("MySQL 5.7", "mysql_data"),
        ["MariaDB"]             = ("MariaDB", "mysql_data"),
        ["FirebirdServerDefaultInstance"] = ("Firebird Server", "firebird"),
        ["FirebirdGuardianDefaultInstance"] = ("Firebird Guardian", "firebird"),
    };

    public static List<HotBackupWarning> DetectRunningServices(IEnumerable<string> selectedItemIds)
    {
        var warnings = new List<HotBackupWarning>();
        var selectedSet = new HashSet<string>(selectedItemIds);

        foreach (var (svcName, (displayName, itemId)) in WatchedServices)
        {
            if (!selectedSet.Contains(itemId)) continue;

            try
            {
                using var svc = new ServiceController(svcName);
                if (svc.Status == ServiceControllerStatus.Running)
                {
                    warnings.Add(new HotBackupWarning
                    {
                        ServiceName = svcName,
                        DisplayName = displayName,
                        RelatedItemId = itemId,
                        Message = $"{displayName} servisi çalışıyor. Açık veritabanı dosyaları doğrudan kopyalanamaz. VSS ile yedekleme yapılacak.",
                        RequiresVss = true
                    });
                    LogService.Warn($"Hot backup tespit edildi: {displayName} ({svcName}) çalışıyor.");
                }
            }
            catch (InvalidOperationException) { /* Servis yok */ }
            catch (Exception ex) { LogService.Error($"Servis kontrol hatası: {svcName}", ex); }
        }

        // Outlook açık mı?
        if (selectedSet.Contains("pst") || selectedSet.Contains("ost"))
        {
            var outlookProcs = Process.GetProcessesByName("OUTLOOK");
            if (outlookProcs.Length > 0)
            {
                warnings.Add(new HotBackupWarning
                {
                    ServiceName = "OUTLOOK",
                    DisplayName = "Microsoft Outlook",
                    RelatedItemId = "pst",
                    Message = "Outlook açık çalışıyor. PST/OST dosyaları kilitli olabilir. VSS ile yedekleme yapılacak.",
                    RequiresVss = true
                });
                LogService.Warn("Outlook çalışıyor - VSS kullanılacak.");
            }
            foreach (var p in outlookProcs) p.Dispose();
        }

        return warnings;
    }
}
