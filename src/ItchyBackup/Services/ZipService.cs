using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class ZipService
{
    /// <summary>
    /// Kaynak klasörü ZIP'e sıkıştırır. Şifre verilirse AES-256 ile şifreler.
    /// </summary>
    public static async Task CompressFolderAsync(
        string sourceFolder,
        string zipPath,
        string? password = null,
        CompressionLevel level = CompressionLevel.Normal,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            using var fsOut = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
            using var zipStream = new ZipOutputStream(fsOut);

            zipStream.SetLevel((int)level);

            if (!string.IsNullOrEmpty(password))
            {
                zipStream.Password = password;
                // SharpZipLib AES-256 için:
                // ZipEntry.AESKeySize = 256 (her entry için ayarlanır)
            }

            var sourceDir = new DirectoryInfo(sourceFolder);
            AddFolderToZip(zipStream, sourceFolder, sourceDir.Name, password, progress, ct);
        }, ct);

        LogService.Info($"ZIP oluşturuldu: {zipPath} ({(string.IsNullOrEmpty(password) ? "şifresiz" : "AES-256 şifreli")})");
    }

    private static void AddFolderToZip(
        ZipOutputStream zipStream,
        string baseFolder,
        string entryPrefix,
        string? password,
        IProgress<string>? progress,
        CancellationToken ct)
    {
        foreach (var file in Directory.GetFiles(baseFolder))
        {
            ct.ThrowIfCancellationRequested();
            var entryName = Path.Combine(entryPrefix, Path.GetFileName(file));
            entryName = ZipEntry.CleanName(entryName);

            var entry = new ZipEntry(entryName)
            {
                DateTime = File.GetLastWriteTime(file),
                Size = new FileInfo(file).Length
            };

            if (!string.IsNullOrEmpty(password))
                entry.AESKeySize = 256;

            progress?.Report($"Sıkıştırılıyor: {Path.GetFileName(file)}");
            zipStream.PutNextEntry(entry);

            var buffer = new byte[81920];
            using var fs = File.OpenRead(file);
            StreamUtils.Copy(fs, zipStream, buffer);
            zipStream.CloseEntry();
        }

        foreach (var dir in Directory.GetDirectories(baseFolder))
        {
            ct.ThrowIfCancellationRequested();
            var subPrefix = Path.Combine(entryPrefix, Path.GetFileName(dir));
            AddFolderToZip(zipStream, dir, subPrefix, password, progress, ct);
        }
    }

    /// <summary>
    /// ZIP dosyasını çıkarır.
    /// </summary>
    public static async Task ExtractZipAsync(
        string zipPath,
        string outputFolder,
        string? password = null,
        CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            using var fsIn = new FileStream(zipPath, FileMode.Open, FileAccess.Read);
            using var zipStream = new ZipInputStream(fsIn);
            if (!string.IsNullOrEmpty(password))
                zipStream.Password = password;

            ZipEntry? entry;
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                ct.ThrowIfCancellationRequested();
                var destPath = Path.Combine(outputFolder, entry.Name.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                if (entry.IsFile)
                {
                    using var fs = new FileStream(destPath, FileMode.Create);
                    var buffer = new byte[81920];
                    StreamUtils.Copy(zipStream, fs, buffer);
                }
            }
        }, ct);
    }
}
