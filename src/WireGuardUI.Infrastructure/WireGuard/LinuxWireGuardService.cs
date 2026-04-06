using System.Diagnostics;
using System.Text.RegularExpressions;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.WireGuard;

public class LinuxWireGuardService(
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

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, configContent);
        TrySetOwnerReadWriteOnly(tmpPath);
        File.Move(tmpPath, path, overwrite: true);
        TrySetOwnerReadWriteOnly(path);
    }

    public async Task<ApplyResult> SyncConfAsync()
    {
        var settings = await settingRepo.GetAsync();
        var configPath = string.IsNullOrWhiteSpace(settings.ConfigFilePath)
            ? "/etc/wireguard/wg0.conf"
            : settings.ConfigFilePath;
        var tunnelName = Path.GetFileNameWithoutExtension(configPath); // e.g. "wg0"

        // Try wg syncconf first
        var syncResult = await RunAsync("wg", $"syncconf {tunnelName} {configPath}");
        if (syncResult.ExitCode == 0)
            return ApplyResult.Ok(syncResult.Output);

        // Fallback: wg-quick down + up
        await RunAsync("wg-quick", $"down {tunnelName}");
        var upResult = await RunAsync("wg-quick", $"up {tunnelName}");
        return upResult.ExitCode == 0
            ? ApplyResult.Ok(upResult.Output)
            : ApplyResult.Fail($"wg syncconf and wg-quick both failed: {upResult.Output}", upResult.Output);
    }

    public async Task<Result<WireGuardStatus>> GetStatusAsync()
    {
        var result = await RunAsync("wg", "show all dump");
        if (result.ExitCode != 0)
            return Result<WireGuardStatus>.Failure(result.Output);

        return Result<WireGuardStatus>.Success(ParseDump(result.Output));
    }

    private static WireGuardStatus ParseDump(string dump)
    {
        var lines = dump.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var peers = new List<WireGuardPeer>();
        string interfaceName = "wg0", publicKey = "", listenPort = "0";

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 5 && parts[0] == "wg0")
            {
                if (parts[1] != "(none)" && parts[3] == "(none)")
                {
                    // Interface line: interface, private-key, public-key, listen-port, fwmark
                    interfaceName = parts[0];
                    publicKey = parts[2];
                    listenPort = parts[3];
                }
                else if (parts.Length >= 8)
                {
                    // Peer line: interface, public-key, preshared-key, endpoint, allowed-ips, latest-handshake, rx, tx
                    peers.Add(new WireGuardPeer(
                        PublicKey: parts[1],
                        Endpoint: parts[3] == "(none)" ? null : parts[3],
                        AllowedIPs: parts[4].Split(',').Select(s => s.Trim()).ToList(),
                        LastHandshake: parts[5] == "0" ? null : DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[5])).ToString("u"),
                        RxBytes: long.TryParse(parts[6], out var rx) ? rx : 0,
                        TxBytes: long.TryParse(parts[7], out var tx) ? tx : 0));
                }
            }
        }

        return new WireGuardStatus(interfaceName, publicKey, int.TryParse(listenPort, out var port) ? port : 0, peers);
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        try
        {
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return (process.ExitCode, string.IsNullOrEmpty(error) ? output : error);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return (-1, $"Failed to start '{fileName}': {ex.Message} (Is the tool installed?)");
        }
    }

    private static void TrySetOwnerReadWriteOnly(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Best effort only; do not fail config writes because chmod is unavailable.
        }
    }
}
