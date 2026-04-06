using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;

namespace WireGuardUI.Infrastructure.WireGuard;

/// <summary>
/// A no-op WireGuard service used in development (e.g. macOS) where wg / wg-quick are not installed.
/// All operations succeed silently without touching the host system.
/// </summary>
public class NoOpWireGuardService(
    IClientRepository clientRepo,
    IServerConfigRepository serverRepo,
    IGlobalSettingRepository settingRepo) : IWireGuardService
{
    public async Task<string> GenerateConfigAsync()
    {
        var server = await serverRepo.GetAsync();
        var settings = await settingRepo.GetAsync();
        var clients = await clientRepo.GetAllAsync();
        return WireGuardConfigGenerator.GenerateServerConfig(server, settings, clients);
    }

    public async Task WriteConfigAsync(string configContent)
    {
        var settings = await settingRepo.GetAsync();
        var path = string.IsNullOrWhiteSpace(settings.ConfigFilePath)
            ? "/etc/wireguard/wg0.conf"
            : settings.ConfigFilePath;

        // Ensure the directory exists (important for dev paths)
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, configContent);
        File.Move(tmpPath, path, overwrite: true);
    }

    public Task<ApplyResult> SyncConfAsync()
    {
        // Skip wg/wg-quick — not available on macOS/dev
        return Task.FromResult(ApplyResult.Ok("[dev] WireGuard sync skipped — config file written but interface not reloaded"));
    }

    public Task<Result<WireGuardStatus>> GetStatusAsync()
    {
        // Return a placeholder status so the UI doesn't error out
        var status = new WireGuardStatus(
            InterfaceName: "wg0",
            PublicKey: "(dev — not running)",
            ListenPort: 51820,
            Peers: []);
        return Task.FromResult(Result<WireGuardStatus>.Success(status));
    }
}
