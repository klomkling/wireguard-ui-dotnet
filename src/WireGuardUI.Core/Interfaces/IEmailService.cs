using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IEmailService
{
    Task<Result> SendClientConfigAsync(WireGuardClient client, ServerConfig server, GlobalSetting settings, string toEmail);
    Task<Result> TestConnectionAsync(EmailSetting emailSetting);
}
