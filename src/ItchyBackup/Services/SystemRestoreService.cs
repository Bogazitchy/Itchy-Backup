using System.Diagnostics;
using System.Security.Principal;

namespace ItchyBackup.Services;

public class SystemRestorePointInfo
{
    public string SequenceNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreationTime { get; set; } = "";
    public string RestorePointType { get; set; } = "";
    public string EventType { get; set; } = "";

    public string Summary => string.IsNullOrWhiteSpace(Description)
        ? $"{SequenceNumber} - {CreationTime}"
        : $"{Description} - {CreationTime}";
}

public static class SystemRestoreService
{
    public static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    public static async Task<string> CreateRestorePointAsync(string description, CancellationToken ct = default)
    {
        if (!IsAdministrator())
            throw new InvalidOperationException("Sistem geri yükleme noktası oluşturmak için programı yönetici olarak çalıştırın.");

        description = string.IsNullOrWhiteSpace(description)
            ? $"Itchy Backup {DateTime.Now:yyyy-MM-dd HH:mm}"
            : description.Trim();

        var script =
            $"Checkpoint-Computer -Description {QuotePowerShell(description)} -RestorePointType MODIFY_SETTINGS";
        var output = await RunPowerShellAsync(script, ct);
        LogService.Info($"Sistem geri yükleme noktası oluşturuldu: {description}");
        return string.IsNullOrWhiteSpace(output)
            ? $"Geri yükleme noktası oluşturuldu: {description}"
            : output.Trim();
    }

    public static async Task<List<SystemRestorePointInfo>> ListRestorePointsAsync(CancellationToken ct = default)
    {
        var script = "Get-ComputerRestorePoint | Sort-Object SequenceNumber -Descending | Select-Object -First 20 SequenceNumber,Description,CreationTime,RestorePointType,EventType | ConvertTo-Json -Compress";
        var output = await RunPowerShellAsync(script, ct);
        if (string.IsNullOrWhiteSpace(output)) return new List<SystemRestorePointInfo>();

        try
        {
            var json = output.Trim();
            if (json.StartsWith("[", StringComparison.Ordinal))
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<SystemRestorePointInfo>>(json) ?? new();

            var single = Newtonsoft.Json.JsonConvert.DeserializeObject<SystemRestorePointInfo>(json);
            return single == null ? new() : new() { single };
        }
        catch (Exception ex)
        {
            LogService.Warn($"Geri yükleme noktaları okunamadı: {ex.Message}");
            return new List<SystemRestorePointInfo>();
        }
    }

    public static void OpenSystemRestore()
    {
        Process.Start(new ProcessStartInfo("rstrui.exe") { UseShellExecute = true });
    }

    public static void OpenSystemProtection()
    {
        Process.Start(new ProcessStartInfo("SystemPropertiesProtection.exe") { UseShellExecute = true });
    }

    private static async Task<string> RunPowerShellAsync(string command, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("powershell.exe",
            "-NoProfile -ExecutionPolicy Bypass -Command " + QuoteCommand(command))
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("PowerShell başlatılamadı.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? $"PowerShell çıkış kodu: {process.ExitCode}" : stderr.Trim());

        return stdout;
    }

    private static string QuotePowerShell(string value) => "'" + value.Replace("'", "''") + "'";
    private static string QuoteCommand(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}
