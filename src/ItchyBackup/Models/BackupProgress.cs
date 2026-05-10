namespace ItchyBackup.Models;

public class BackupProgress
{
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public long TotalBytes { get; set; }
    public long CopiedBytes { get; set; }
    public int TotalFiles { get; set; }
    public int FilesCopied { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesUnchanged { get; set; }
    public string CurrentFile { get; set; } = "";
    public string CurrentCategory { get; set; } = "";
    public double SpeedMBps { get; set; }
    public TimeSpan Elapsed { get; set; }
    public TimeSpan Estimated { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public double PercentComplete =>
        TotalBytes > 0 ? Math.Min(100, (double)CopiedBytes / TotalBytes * 100) : 0;

    public string SpeedText =>
        SpeedMBps >= 1000 ? $"{SpeedMBps / 1024:F1} GB/s" :
        SpeedMBps >= 1   ? $"{SpeedMBps:F1} MB/s" :
                           $"{SpeedMBps * 1024:F0} KB/s";

    public string EstimatedText
    {
        get
        {
            if (Estimated.TotalSeconds < 1) return "--";
            if (Estimated.TotalSeconds < 60) return $"{(int)Estimated.TotalSeconds}s";
            if (Estimated.TotalMinutes < 60) return $"{(int)Estimated.TotalMinutes}dk {Estimated.Seconds}s";
            return $"{(int)Estimated.TotalHours}sa {Estimated.Minutes}dk";
        }
    }

    public string FileCountText => $"{FilesCopied}/{TotalFiles} dosya"
        + (FilesUnchanged > 0 ? $" • {FilesUnchanged} değişmedi" : "")
        + (FilesSkipped > 0 ? $" • {FilesSkipped} atlandı" : "");
}

public class ChecksumResult
{
    public string FilePath { get; set; } = "";
    public string Algorithm { get; set; } = "SHA256";
    public string OriginalHash { get; set; } = "";
    public string VerifiedHash { get; set; } = "";
    public bool IsValid => OriginalHash == VerifiedHash;
}
