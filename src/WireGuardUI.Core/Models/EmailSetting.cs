namespace WireGuardUI.Core.Models;

public class EmailSetting
{
    public int Id { get; set; } = 1;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string Encryption { get; set; } = "STARTTLS"; // None | SSL | TLS | STARTTLS

    /// <summary>Password (default) or OAuth2 (Gmail XOAUTH2).</summary>
    public string AuthType { get; set; } = "Password";

    // OAuth2 fields — only used when AuthType == "OAuth2"
    public string OAuth2ClientId { get; set; } = string.Empty;
    public string OAuth2ClientSecret { get; set; } = string.Empty;
    public string OAuth2RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Microsoft OAuth2 tenant. Use 'consumers' for personal Hotmail/Outlook.com
    /// or 'organizations' (or a specific tenant GUID) for work/school.
    /// </summary>
    public string OAuth2Tenant { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
