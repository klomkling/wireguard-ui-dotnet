namespace WireGuardUI.Core.Models;

public class GlobalSetting
{
    public int Id { get; set; } = 1;
    public string EndpointAddress { get; set; } = string.Empty;
    public List<string> DnsServers { get; set; } = ["1.1.1.1"];
    public int Mtu { get; set; } = 1450;
    public int PersistentKeepalive { get; set; } = 15;
    public string FirewallMark { get; set; } = "0xca6c";
    public string Table { get; set; } = string.Empty;
    public string ConfigFilePath { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
