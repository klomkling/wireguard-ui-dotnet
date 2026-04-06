using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.WireGuard;
namespace WireGuardUI.Tests.Core;

public class WireGuardConfigGeneratorTests
{
    private static ServerConfig MakeServer() => new()
    {
        PrivateKey = "serverPrivKey==",
        PublicKey = "serverPubKey==",
        Addresses = ["10.252.1.0/24"],
        ListenPort = 51820
    };

    private static GlobalSetting MakeSettings() => new()
    {
        EndpointAddress = "vpn.example.com",
        DnsServers = ["1.1.1.1"],
        Mtu = 1450,
        PersistentKeepalive = 15
    };

    [Fact]
    public void GenerateServerConfig_ContainsInterfaceSection()
    {
        var server = MakeServer();
        var clients = new List<WireGuardClient>();

        var result = WireGuardConfigGenerator.GenerateServerConfig(server, MakeSettings(), clients);

        Assert.Contains("[Interface]", result);
        Assert.Contains("ListenPort = 51820", result);
        Assert.Contains("PrivateKey = serverPrivKey==", result);
        Assert.Contains("Address = 10.252.1.0/24", result);
        Assert.Contains("MTU = 1450", result);
    }

    [Fact]
    public void GenerateServerConfig_OnlyIncludesEnabledClients()
    {
        var server = MakeServer();
        var clients = new List<WireGuardClient>
        {
            new() { Name = "Alice", PublicKey = "alicePub==", AllocatedIPs = ["10.252.1.2/32"], Enabled = true },
            new() { Name = "Bob",   PublicKey = "bobPub==",   AllocatedIPs = ["10.252.1.3/32"], Enabled = false }
        };

        var result = WireGuardConfigGenerator.GenerateServerConfig(server, MakeSettings(), clients);

        Assert.Contains("alicePub==", result);
        Assert.DoesNotContain("bobPub==", result);
    }

    [Fact]
    public void GenerateClientConfig_ContainsBothSections()
    {
        var client = new WireGuardClient
        {
            PrivateKey = "clientPrivKey==",
            PublicKey = "clientPubKey==",
            PresharedKey = "psk==",
            AllocatedIPs = ["10.252.1.2/32"],
            AllowedIPs = ["0.0.0.0/0"],
            UseServerDns = true
        };

        var result = WireGuardConfigGenerator.GenerateClientConfig(client, MakeServer(), MakeSettings());

        Assert.Contains("[Interface]", result);
        Assert.Contains("PrivateKey = clientPrivKey==", result);
        Assert.Contains("Address = 10.252.1.2/32", result);
        Assert.Contains("[Peer]", result);
        Assert.Contains("PublicKey = serverPubKey==", result);
        Assert.Contains("Endpoint = vpn.example.com:51820", result);
        Assert.Contains("PresharedKey = psk==", result);
        Assert.Contains("PersistentKeepalive = 15", result);
        Assert.Contains("DNS = 1.1.1.1", result);
    }
}
