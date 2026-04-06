using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IGlobalSettingRepository
{
    Task<GlobalSetting> GetAsync();
    Task SaveAsync(GlobalSetting setting);
}
