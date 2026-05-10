using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ItchyBackup.Services;

public static class NetworkShareHelper
{
    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetAddConnection2(ref NETRESOURCE netResource, string? password, string? username, int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NETRESOURCE
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public string? lpLocalName;
        public string lpRemoteName;
        public string? lpComment;
        public string? lpProvider;
    }

    private const int RESOURCETYPE_DISK = 1;

    /// <summary>Kimlik bilgileriyle UNC paylaşımına bağlanır.</summary>
    public static void Connect(string uncPath, string? username, string? password, string? domain)
    {
        if (!IsUncPath(uncPath) || string.IsNullOrEmpty(username)) return;

        var server = GetUncServer(uncPath);
        var netResource = new NETRESOURCE
        {
            dwType = RESOURCETYPE_DISK,
            lpRemoteName = server
        };

        var fullUser = string.IsNullOrEmpty(domain) ? username : $"{domain}\\{username}";
        var result = WNetAddConnection2(ref netResource,
            string.IsNullOrEmpty(password) ? null : password,
            fullUser, 0);

        // 1219 = ERROR_SESSION_CREDENTIAL_CONFLICT — zaten bağlı
        if (result != 0 && result != 1219)
            throw new Win32Exception(result, $"Ağ bağlantısı kurulamadı: {server} (Win32 hata: {result})");
    }

    /// <summary>UNC sunucusuyla bağlantıyı keser.</summary>
    public static void Disconnect(string uncPath)
    {
        if (!IsUncPath(uncPath)) return;
        try { WNetCancelConnection2(GetUncServer(uncPath), 0, false); } catch { }
    }

    public static bool IsUncPath(string? path) =>
        !string.IsNullOrWhiteSpace(path) && path.TrimStart().StartsWith(@"\\");

    private static string GetUncServer(string uncPath)
    {
        var trimmed = uncPath.TrimStart('\\');
        var slash = trimmed.IndexOf('\\');
        var server = slash >= 0 ? trimmed[..slash] : trimmed;
        return @"\\" + server;
    }
}
