using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class GlobalSettingRepository(AppDbContext db) : IGlobalSettingRepository
{
    public async Task<GlobalSetting> GetAsync() =>
        await db.GlobalSettings.FirstOrDefaultAsync() ?? new GlobalSetting();

    public async Task SaveAsync(GlobalSetting setting)
    {
        setting.UpdatedAt = DateTime.UtcNow;
        var existing = await db.GlobalSettings.FirstOrDefaultAsync();
        if (existing is null)
            db.GlobalSettings.Add(setting);
        else
        {
            db.Entry(existing).CurrentValues.SetValues(setting);
            existing.DnsServers = setting.DnsServers;
        }
        await db.SaveChangesAsync();
    }
}
