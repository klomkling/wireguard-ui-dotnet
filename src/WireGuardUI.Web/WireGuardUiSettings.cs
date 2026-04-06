using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WireGuardUI.Web;

public class WireGuardUiSettings
{
    public string DbPath { get; set; } = string.Empty;
    public int SessionExpiryHours { get; set; } = 8;
    public string DefaultUsername { get; set; } = string.Empty;
    public string DefaultPassword { get; set; } = string.Empty;
    public string WireGuardConfigPath { get; set; } = string.Empty;
    public string WireGuardExePath { get; set; } = string.Empty;

    public string ResolvedDbPath => string.IsNullOrWhiteSpace(DbPath)
        ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WireGuardUI", "wireguard-ui.db")
            : "/var/lib/wireguard-ui/wireguard-ui.db"
        : DbPath;

    public string ResolvedConfigPath => string.IsNullOrWhiteSpace(WireGuardConfigPath)
        ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WireGuardUI", "wireguard", "wg0.conf")
            : "/etc/wireguard/wg0.conf"
        : WireGuardConfigPath;

    public string ResolvedWgExePath => string.IsNullOrWhiteSpace(WireGuardExePath)
        ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Program Files\WireGuard\wg.exe"
            : "wg"
        : WireGuardExePath;
}
