using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class EmailSettingRepository(AppDbContext db) : IEmailSettingRepository
{
    public async Task<EmailSetting> GetAsync() =>
        await db.EmailSettings.FirstOrDefaultAsync() ?? new EmailSetting();

    public async Task SaveAsync(EmailSetting setting)
    {
        setting.UpdatedAt = DateTime.UtcNow;
        var existing = await db.EmailSettings.FirstOrDefaultAsync();
        if (existing is null)
            db.EmailSettings.Add(setting);
        else
            db.Entry(existing).CurrentValues.SetValues(setting);
        await db.SaveChangesAsync();
    }
}
