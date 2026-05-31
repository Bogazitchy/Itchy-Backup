using System.IO;
using Newtonsoft.Json;
using ItchyBackup.Models;

namespace ItchyBackup.Services;

public static class ProfileService
{
    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ItchyBackup", "Profiles");

    static ProfileService() => Directory.CreateDirectory(ProfileDir);

    public static void Save(BackupProfile profile)
    {
        profile.LastUsed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var path = Path.Combine(ProfileDir, SanitizeFileName(profile.ProfileName) + ".json");
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
        File.WriteAllText(path, json);
        LogService.Info($"Profil kaydedildi: {profile.ProfileName}");
    }

    public static BackupProfile? Load(string profileName)
    {
        var path = Path.Combine(ProfileDir, SanitizeFileName(profileName) + ".json");
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<BackupProfile>(json);
    }

    public static List<BackupProfile> LoadAll()
    {
        var result = new List<BackupProfile>();
        foreach (var file in Directory.GetFiles(ProfileDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonConvert.DeserializeObject<BackupProfile>(json);
                if (profile != null) result.Add(profile);
            }
            catch (Exception ex) { LogService.Error($"Profil okunamadı: {file}", ex); }
        }
        return result.OrderByDescending(p => p.LastUsed).ToList();
    }

    public static void Delete(string profileName)
    {
        var path = Path.Combine(ProfileDir, SanitizeFileName(profileName) + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    public static void Export(BackupProfile profile, string targetPath)
    {
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
        File.WriteAllText(targetPath, json);
        LogService.Info($"Profil dışa aktarıldı: {profile.ProfileName}");
    }

    public static BackupProfile Import(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var profile = JsonConvert.DeserializeObject<BackupProfile>(json)
            ?? throw new InvalidDataException("Profil dosyası okunamadı.");
        if (string.IsNullOrWhiteSpace(profile.ProfileName))
            throw new InvalidDataException("Profil adı boş olamaz.");

        Save(profile);
        LogService.Info($"Profil içe aktarıldı: {profile.ProfileName}");
        return profile;
    }

    public static string GetProfileDir() => ProfileDir;

    public static void EnsureDefaultProfiles()
    {
        var createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        EnsureProfile(new BackupProfile
        {
            ProfileName = "Hızlı Format",
            CreatedAt = createdAt,
            Icon = "flash",
            UseVss = true,
            VerifyChecksum = true,
            SelectedItemIds = new List<string>
            {
                "desktop", "documents", "downloads", "pictures", "videos", "music",
                "chrome", "firefox", "edge", "opera", "brave", "vivaldi",
                "wifiProfiles", "winDrivers",
                "pst", "ost", "sig", "templ", "nk2",
            }
        });

        EnsureProfile(new BackupProfile
        {
            ProfileName = "Standart Servis",
            CreatedAt = createdAt,
            Icon = "tool",
            UseVss = true,
            VerifyChecksum = true,
            SelectedItemIds = new List<string>
            {
                "desktop", "documents", "downloads", "pictures",
                "chrome", "firefox", "edge", "wifiProfiles"
            }
        });

        EnsureProfile(new BackupProfile
        {
            ProfileName = "Muhasebe PC",
            CreatedAt = createdAt,
            Icon = "briefcase",
            UseVss = true,
            VerifyChecksum = true,
            SelectedItemIds = new List<string>
            {
                "desktop", "documents", "pst", "ost", "firebird", "sqlite", "sqlserver", "access"
            }
        });

        EnsureProfile(new BackupProfile
        {
            ProfileName = "Tarayıcı Kurtarma",
            CreatedAt = createdAt,
            Icon = "globe",
            UseVss = false,
            VerifyChecksum = true,
            SelectedItemIds = new List<string>
            {
                "chrome", "firefox", "edge", "opera", "brave", "vivaldi",
                "onedrive", "googledrive", "dropbox"
            }
        });

        EnsureProfile(new BackupProfile
        {
            ProfileName = "Tam Kullanıcı",
            CreatedAt = createdAt,
            Icon = "person",
            UseVss = true,
            VerifyChecksum = true,
            SelectedItemIds = new List<string>
            {
                "desktop", "documents", "downloads", "pictures", "videos", "music", "appdata",
                "chrome", "firefox", "edge", "onedrive", "googledrive", "dropbox"
            }
        });
    }

    private static void EnsureProfile(BackupProfile profile)
    {
        var path = Path.Combine(ProfileDir, SanitizeFileName(profile.ProfileName) + ".json");
        if (File.Exists(path)) return;
        var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
        File.WriteAllText(path, json);
        LogService.Info($"Varsayılan profil oluşturuldu: {profile.ProfileName}");
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
