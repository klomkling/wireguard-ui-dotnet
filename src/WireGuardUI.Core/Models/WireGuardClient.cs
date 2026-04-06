namespace WireGuardUI.Core.Models;

public class WireGuardClient
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PresharedKey { get; set; } = string.Empty;
    public List<string> AllocatedIPs { get; set; } = [];
    public List<string> AllowedIPs { get; set; } = ["0.0.0.0/0"];
    public List<string> ExtraAllowedIPs { get; set; } = [];
    public string? Endpoint { get; set; }
    public bool UseServerDns { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
