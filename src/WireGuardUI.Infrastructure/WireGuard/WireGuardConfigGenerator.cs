using System.Text;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.WireGuard;

public static class WireGuardConfigGenerator
{
    public static string GenerateServerConfig(
        ServerConfig server,
        GlobalSetting settings,
        IEnumerable<WireGuardClient> clients)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Interface]");
        sb.AppendLine($"Address = {string.Join(", ", server.Addresses)}");
        sb.AppendLine($"ListenPort = {server.ListenPort}");
        sb.AppendLine($"PrivateKey = {server.PrivateKey}");
        sb.AppendLine($"MTU = {settings.Mtu}");

        if (settings.DnsServers.Count > 0)
            sb.AppendLine($"DNS = {string.Join(", ", settings.DnsServers)}");
        if (!string.IsNullOrWhiteSpace(settings.FirewallMark) && settings.FirewallMark != "0")
            sb.AppendLine($"FwMark = {settings.FirewallMark}");
        if (!string.IsNullOrWhiteSpace(settings.Table))
            sb.AppendLine($"Table = {settings.Table}");
        if (!string.IsNullOrWhiteSpace(server.PostUp))
            sb.AppendLine($"PostUp = {server.PostUp}");
        if (!string.IsNullOrWhiteSpace(server.PreDown))
            sb.AppendLine($"PreDown = {server.PreDown}");
        if (!string.IsNullOrWhiteSpace(server.PostDown))
            sb.AppendLine($"PostDown = {server.PostDown}");

        foreach (var client in clients.Where(c => c.Enabled))
        {
            sb.AppendLine();
            sb.AppendLine("[Peer]");
            sb.AppendLine($"# Name = {client.Name}");
            sb.AppendLine($"PublicKey = {client.PublicKey}");
            if (!string.IsNullOrWhiteSpace(client.PresharedKey))
                sb.AppendLine($"PresharedKey = {client.PresharedKey}");
            var allowedIps = client.AllocatedIPs.Concat(client.ExtraAllowedIPs);
            sb.AppendLine($"AllowedIPs = {string.Join(", ", allowedIps)}");
            if (!string.IsNullOrWhiteSpace(client.Endpoint))
                sb.AppendLine($"Endpoint = {client.Endpoint}");
        }

        return sb.ToString();
    }

    public static string GenerateClientConfig(
        WireGuardClient client,
        ServerConfig server,
        GlobalSetting settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Interface]");
        sb.AppendLine($"PrivateKey = {client.PrivateKey}");
        sb.AppendLine($"Address = {string.Join(", ", client.AllocatedIPs)}");
        if (client.UseServerDns && settings.DnsServers.Count > 0)
            sb.AppendLine($"DNS = {string.Join(", ", settings.DnsServers)}");
        sb.AppendLine($"MTU = {settings.Mtu}");
        sb.AppendLine();
        sb.AppendLine("[Peer]");
        sb.AppendLine($"PublicKey = {server.PublicKey}");
        if (!string.IsNullOrWhiteSpace(client.PresharedKey))
            sb.AppendLine($"PresharedKey = {client.PresharedKey}");
        sb.AppendLine($"Endpoint = {settings.EndpointAddress}:{server.ListenPort}");
        var allowedIps = client.AllowedIPs.Concat(client.ExtraAllowedIPs);
        sb.AppendLine($"AllowedIPs = {string.Join(", ", allowedIps)}");
        sb.AppendLine($"PersistentKeepalive = {settings.PersistentKeepalive}");

        return sb.ToString();
    }
}
