using System.IO;
using System.Security.Cryptography;

namespace ItchyBackup.Services;

public static class BackupCompareService
{
    /// <summary>İki yedek klasörünü karşılaştırır.</summary>
    public static async Task<CompareReport> CompareAsync(
        string backupA, string backupB,
        IProgress<string>? progress, CancellationToken ct)
    {
        var report = new CompareReport
        {
            PathA = backupA,
            PathB = backupB
        };

        if (!Directory.Exists(backupA) || !Directory.Exists(backupB))
            throw new DirectoryNotFoundException("Karşılaştırılacak klasörler bulunamadı.");

        progress?.Report("Yedek A taranıyor...");
        var filesA = await Task.Run(() => GetFileMap(backupA, ct), ct);
        progress?.Report("Yedek B taranıyor...");
        var filesB = await Task.Run(() => GetFileMap(backupB, ct), ct);

        var allKeys = filesA.Keys.Union(filesB.Keys).OrderBy(k => k);
        int total = filesA.Count + filesB.Count;
        int idx = 0;

        foreach (var key in allKeys)
        {
            ct.ThrowIfCancellationRequested();
            idx++;
            progress?.Report($"Karşılaştırma: {key} ({idx})");

            var existsA = filesA.TryGetValue(key, out var infoA);
            var existsB = filesB.TryGetValue(key, out var infoB);

            if (existsA && !existsB)
            {
                report.OnlyInA.Add(new CompareEntry
                {
                    RelativePath = key,
                    SizeA = infoA!.Length,
                    Type = CompareType.OnlyInA
                });
            }
            else if (!existsA && existsB)
            {
                report.OnlyInB.Add(new CompareEntry
                {
                    RelativePath = key,
                    SizeB = infoB!.Length,
                    Type = CompareType.OnlyInB
                });
            }
            else if (existsA && existsB)
            {
                if (infoA!.Length != infoB!.Length)
                {
                    report.Modified.Add(new CompareEntry
                    {
                        RelativePath = key,
                        SizeA = infoA.Length,
                        SizeB = infoB.Length,
                        Type = CompareType.Modified
                    });
                }
                else
                {
                    report.Identical++;
                }
            }
        }

        return report;
    }

    private static Dictionary<string, FileInfo> GetFileMap(string root, CancellationToken ct)
    {
        var map = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (ct.IsCancellationRequested) break;
                if (f.EndsWith("checksums.sha256") || Path.GetFileName(f).StartsWith("backup_log_"))
                    continue;
                var rel = Path.GetRelativePath(root, f);
                try { map[rel] = new FileInfo(f); } catch { }
            }
        }
        catch { }
        return map;
    }
}

public class CompareReport
{
    public string PathA { get; set; } = "";
    public string PathB { get; set; } = "";
    public List<CompareEntry> OnlyInA { get; set; } = new();
    public List<CompareEntry> OnlyInB { get; set; } = new();
    public List<CompareEntry> Modified { get; set; } = new();
    public int Identical { get; set; }
    public int TotalChanges => OnlyInA.Count + OnlyInB.Count + Modified.Count;
    public string Summary => $"{Identical} aynı • {Modified.Count} değişmiş • {OnlyInA.Count} sadece A'da • {OnlyInB.Count} sadece B'de";
}

public class CompareEntry
{
    public string RelativePath { get; set; } = "";
    public long SizeA { get; set; }
    public long SizeB { get; set; }
    public CompareType Type { get; set; }
    public string TypeText => Type switch
    {
        CompareType.OnlyInA => "Sadece A'da",
        CompareType.OnlyInB => "Sadece B'de",
        CompareType.Modified => "Değişmiş",
        _ => "?"
    };
    public string SizeText => Type == CompareType.Modified
        ? $"{DiskSpaceChecker.FormatBytes(SizeA)} → {DiskSpaceChecker.FormatBytes(SizeB)}"
        : DiskSpaceChecker.FormatBytes(Type == CompareType.OnlyInA ? SizeA : SizeB);
}

public enum CompareType { OnlyInA, OnlyInB, Modified }
