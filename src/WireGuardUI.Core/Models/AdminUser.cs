namespace WireGuardUI.Core.Models;

public class AdminUser
{
    public int Id { get; set; } = 1;
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
