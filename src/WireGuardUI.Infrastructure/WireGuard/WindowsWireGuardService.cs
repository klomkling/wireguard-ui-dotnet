using System.Diagnostics;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.WireGuard;

public class WindowsWireGuardService(
    IClientRepository clientRepo,
    IServerConfigRepository serverRepo,
    IGlobalSettingRepository settingRepo,
    string wgExePath = @"C:\Program Files\WireGuard\wg.exe") : IWireGuardService
{
    private const string LegacyProgramFilesConfigPath = @"C:\Program Files\WireGuard\wg0.conf";

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
        var path = ResolveConfigPath(settings.ConfigFilePath);

        if (!string.Equals(settings.ConfigFilePath, path, StringComparison.OrdinalIgnoreCase))
        {
            settings.ConfigFilePath = path;
            await settingRepo.SaveAsync(settings);
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, configContent);
        File.Move(tmpPath, path, overwrite: true);
    }

    public async Task<ApplyResult> SyncConfAsync()
    {
        var settings = await settingRepo.GetAsync();
        var configPath = ResolveConfigPath(settings.ConfigFilePath);
        var tunnelName = Path.GetFileNameWithoutExtension(configPath); // e.g. "wg0"
        var serviceName = $"WireGuardTunnel${tunnelName}";

        // Try wg syncconf
        var syncResult = await RunAsync(wgExePath, $"syncconf {tunnelName} \"{configPath}\"");
        if (syncResult.ExitCode == 0)
            return ApplyResult.Ok(syncResult.Output);

        // Ensure tunnel service exists (first-run/bootstrap on Windows)
        var ensureServiceResult = await EnsureTunnelServiceAsync(serviceName, configPath);
        if (!ensureServiceResult.Success)
            return ApplyResult.Fail(
                $"wg syncconf failed and tunnel service is unavailable: {ensureServiceResult.Error}",
                syncResult.Output);

        // Fallback: restart Windows Service
        await RunAsync("net", $"stop \"{serviceName}\"");
        var startResult = await RunAsync("net", $"start \"{serviceName}\"");
        if (startResult.ExitCode != 0 &&
            (startResult.Output.Contains("NET HELPMSG 2185", StringComparison.OrdinalIgnoreCase) ||
             startResult.Output.Contains("service name is invalid", StringComparison.OrdinalIgnoreCase)))
        {
            var wireGuardExe = ResolveWireGuardExePath();
            var hint = $"Service '{serviceName}' was not found. Install tunnel service with: \"{wireGuardExe}\" /installtunnelservice \"{configPath}\"";
            return ApplyResult.Fail(hint, startResult.Output);
        }

        return startResult.ExitCode == 0
            ? ApplyResult.Ok(startResult.Output)
            : ApplyResult.Fail($"wg syncconf and service restart both failed: {startResult.Output}", startResult.Output);
    }

    public async Task<Result<WireGuardStatus>> GetStatusAsync()
    {
        var result = await RunAsync(wgExePath, "show all dump");
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
            if (parts.Length >= 5)
            {
                if (parts.Length == 5)
                {
                    interfaceName = parts[0];
                    publicKey = parts[2];
                    listenPort = parts[3];
                }
                else if (parts.Length >= 8)
                {
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
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, string.IsNullOrEmpty(error) ? output : error);
    }

    private static string ResolveConfigPath(string? configuredPath)
    {
        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "WireGuardUI",
            "wireguard",
            "wg0.conf");

        if (string.IsNullOrWhiteSpace(configuredPath))
            return fallback;

        // Backward compatibility: old default under Program Files is not writable for non-admin app processes.
        if (string.Equals(configuredPath, LegacyProgramFilesConfigPath, StringComparison.OrdinalIgnoreCase))
            return fallback;

        return configuredPath;
    }

    private async Task<(bool Success, string Error)> EnsureTunnelServiceAsync(string serviceName, string configPath)
    {
        var queryResult = await RunAsync("sc", $"query \"{serviceName}\"");
        if (queryResult.ExitCode == 0)
            return (true, string.Empty);

        var wireGuardExe = ResolveWireGuardExePath();
        if (!File.Exists(wireGuardExe))
            return (false, $"'{serviceName}' not found and WireGuard.exe is missing at '{wireGuardExe}'.");

        var installResult = await RunAsync(wireGuardExe, $"/installtunnelservice \"{configPath}\"");
        if (installResult.ExitCode != 0)
            return (false, $"'{serviceName}' not found. Auto-install failed: {installResult.Output}");

        return (true, string.Empty);
    }

    private string ResolveWireGuardExePath()
    {
        var dir = Path.GetDirectoryName(wgExePath);
        return string.IsNullOrWhiteSpace(dir)
            ? "wireguard.exe"
            : Path.Combine(dir, "wireguard.exe");
    }
}
