using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IEmailSettingRepository
{
    Task<EmailSetting> GetAsync();
    Task SaveAsync(EmailSetting setting);
}
