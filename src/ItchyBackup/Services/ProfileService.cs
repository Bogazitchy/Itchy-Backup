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

    public static string GetProfileDir() => ProfileDir;

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
