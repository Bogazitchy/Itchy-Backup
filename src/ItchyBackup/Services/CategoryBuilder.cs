using ItchyBackup.Models;
using System.IO;
using Microsoft.Win32;

namespace ItchyBackup.Services;

public static class CategoryBuilder
{
    public static List<BackupCategory> BuildAll()
    {
        var categories = new List<BackupCategory>
        {
            BuildUserFolders(),
            BuildBrowsers(),
            BuildOutlook(),
            BuildDatabases(),
            BuildVirtualMachines(),
            BuildCloudStorage(),
            BuildSystemTools(),
            BuildCustomFolders()
        };
        foreach (var cat in categories)
            foreach (var item in cat.Items)
            {
                item.Parent = cat;
                item.IsDetected = DetectItem(item);
            }
        return categories;
    }

    private static BackupCategory BuildSystemTools() => new()
    {
        Id = "system_tools", Name = "Sistem Araçları",
        Description = "Windows sürücüleri ve WiFi profilleri", AccentColor = "#A29BFE",
        Type = CategoryType.SystemTools,
        Items = new()
        {
            new() { Id = "winDrivers",   Label = "Windows Sürücüleri", Path = "pnputil_export" },
            new() { Id = "wifiProfiles", Label = "WiFi Profilleri",    Path = "netsh_wifi_export" },
        }
    };

    private static BackupCategory BuildCustomFolders() => new()
    {
        Id = "custom", Name = "Özel Klasörler",
        Description = "Manuel eklenen klasörler ve dosyalar",
        AccentColor = "#E17055", Type = CategoryType.CustomFolders,
        Items = new()
    };

    private static BackupCategory BuildUserFolders()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new BackupCategory
        {
            Id = "user_folders", Name = "Kullanıcı Klasörleri",
            Description = "%USERPROFILE% dizini", AccentColor = "#6C5CE7",
            Type = CategoryType.UserFolders,
            Items = new()
            {
                new() { Id="desktop",   Label="Masaüstü",        Path=Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                new() { Id="documents", Label="Belgelerim",       Path=Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                new() { Id="downloads", Label="İndirilenler",     Path=Path.Combine(profile,"Downloads") },
                new() { Id="pictures",  Label="Resimler",         Path=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                new() { Id="videos",    Label="Videolar",         Path=Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                new() { Id="music",     Label="Müzik",            Path=Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                new() { Id="appdata",   Label="AppData\\Roaming", Path=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) },
            }
        };
    }

    private static BackupCategory BuildBrowsers()
    {
        var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new BackupCategory
        {
            Id = "browsers", Name = "Tarayıcı Verileri",
            Description = "Şifreler, yer imleri, çerezler", AccentColor = "#00CEC9",
            Type = CategoryType.Browsers,
            Items = new()
            {
                new() { Id="chrome",  Label="Google Chrome",    Path=Path.Combine(local,  @"Google\Chrome\User Data") },
                new() { Id="firefox", Label="Mozilla Firefox",  Path=Path.Combine(roaming,@"Mozilla\Firefox\Profiles") },
                new() { Id="edge",    Label="Microsoft Edge",   Path=Path.Combine(local,  @"Microsoft\Edge\User Data") },
                new() { Id="opera",   Label="Opera / Opera GX", Path=Path.Combine(roaming,@"Opera Software") },
                new() { Id="brave",   Label="Brave Browser",    Path=Path.Combine(local,  @"BraveSoftware\Brave-Browser\User Data") },
                new() { Id="vivaldi", Label="Vivaldi",          Path=Path.Combine(local,  @"Vivaldi\User Data") },
            }
        };
    }

    private static BackupCategory BuildOutlook()
    {
        var docs    = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var local   = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new BackupCategory
        {
            Id = "outlook", Name = "Outlook / Mail Verileri",
            Description = "PST, OST, profil ayarları", AccentColor = "#0078D4",
            Type = CategoryType.Outlook,
            Items = new()
            {
                new() { Id="pst",   Label="PST Dosyaları",      Path=Path.Combine(docs,   @"Outlook Files"),        RequiresVss=true },
                new() { Id="ost",   Label="OST Dosyaları",      Path=Path.Combine(local,  @"Microsoft\Outlook"),    RequiresVss=true },
                new() { Id="sig",   Label="İmzalar",            Path=Path.Combine(roaming,@"Microsoft\Signatures") },
                new() { Id="templ", Label="Şablonlar",          Path=Path.Combine(roaming,@"Microsoft\Templates") },
                new() { Id="nk2",   Label="Otomatik Tamamlama", Path=Path.Combine(roaming,@"Microsoft\Outlook") },
            }
        };
    }

    private static BackupCategory BuildDatabases() => new()
    {
        Id = "databases", Name = "Veritabanları",
        Description = "Firebird, SQLite, SQL Server, Access", AccentColor = "#FDCB6E",
        Type = CategoryType.Databases,
        Items = new()
        {
            new() { Id="firebird",  Label="Firebird (*.fdb, *.gdb)", Path=DetectFirebirdPath(),  RequiresVss=true },
            new() { Id="sqlite",    Label="SQLite (*.db, *.sqlite)",  Path="Sistem genelinde" },
            new() { Id="sqlserver", Label="SQL Server MDF/LDF",       Path=DetectSqlServerPath(), RequiresVss=true },
            new() { Id="access",    Label="Access (*.mdb, *.accdb)",  Path="Sistem genelinde" },
        }
    };

    private static BackupCategory BuildVirtualMachines()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var docs    = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return new BackupCategory
        {
            Id = "vms", Name = "Sanal Makineler",
            Description = ".vmdk, .vdi, .vmx, .vbox dosyaları", AccentColor = "#FF7675",
            Type = CategoryType.VirtualMachines,
            Items = new()
            {
                new() { Id="vmware",    Label="VMware (.vmdk, .vmx)", Path=Path.Combine(docs,   "Virtual Machines") },
                new() { Id="vbox",      Label="VirtualBox (.vdi)",     Path=Path.Combine(profile,"VirtualBox VMs") },
                new() { Id="hyperv",    Label="Hyper-V (.vhdx)",       Path=@"C:\ProgramData\Microsoft\Windows\Hyper-V" },
                new() { Id="parallels", Label="Parallels (.pvm)",      Path=Path.Combine(profile,"Parallels") },
            }
        };
    }

    private static BackupCategory BuildCloudStorage()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return new BackupCategory
        {
            Id = "cloud", Name = "Bulut Depolama (Yerel)",
            Description = "Sync olmayan dosyalar dahil", AccentColor = "#55EFC4",
            Type = CategoryType.CloudStorage,
            Items = new()
            {
                new() { Id="onedrive",    Label="OneDrive",     Path=DetectOneDrivePath() },
                new() { Id="googledrive", Label="Google Drive",  Path=DetectGoogleDrivePath() },
                new() { Id="mega",        Label="MEGA Sync",     Path=Path.Combine(profile,"MEGA") },
                new() { Id="dropbox",     Label="Dropbox",       Path=Path.Combine(profile,"Dropbox") },
            }
        };
    }

    public static BackupItem CreateCustomFolderItem(string path)
    {
        var name = Path.GetFileName(path.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(name)) name = path;
        return new BackupItem
        {
            Id = $"custom_{Guid.NewGuid():N}",
            Label = name,
            Path = path,
            IsDetected = Directory.Exists(path) || File.Exists(path)
        };
    }

    private static bool DetectItem(BackupItem item)
    {
        if (item.Parent?.Type is CategoryType.CustomFolders or CategoryType.SystemTools) return true;
        if (item.Path is "Sistem genelinde" or "Bulunamadı") return true;
        if (item.Parent?.Type is CategoryType.UserFolders or CategoryType.Browsers)
            return Directory.Exists(item.Path);
        return Directory.Exists(item.Path) || File.Exists(item.Path);
    }

    private static string DetectFirebirdPath()
    {
        string[] c = { @"C:\Program Files\Firebird\Firebird_3_0", @"C:\Program Files\Firebird" };
        return c.FirstOrDefault(Directory.Exists) ?? "Bulunamadı";
    }

    private static string DetectSqlServerPath()
    {
        try
        {
            var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server");
            if (k?.GetValue("InstalledInstances") is string[] i && i.Length > 0)
                return @"C:\Program Files\Microsoft SQL Server";
        }
        catch { }
        return "Bulunamadı";
    }

    private static string DetectOneDrivePath()
    {
        try
        {
            var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive");
            if (k?.GetValue("UserFolder") is string p && Directory.Exists(p)) return p;
        }
        catch { }
        var fb = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive");
        return Directory.Exists(fb) ? fb : "Bulunamadı";
    }

    private static string DetectGoogleDrivePath()
    {
        foreach (var c in new[] { "G:\\My Drive", "H:\\My Drive" })
            if (Directory.Exists(c)) return c;
        return "Bulunamadı";
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
