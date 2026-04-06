namespace WireGuardUI.Core.Models;

public class ServerConfig
{
    public int Id { get; set; } = 1;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public List<string> Addresses { get; set; } = ["10.252.1.0/24"];
    public int ListenPort { get; set; } = 51820;
    public string? PostUp { get; set; }
    public string? PreDown { get; set; }
    public string? PostDown { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
