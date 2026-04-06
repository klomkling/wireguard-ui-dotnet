using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class ServerConfigRepository(AppDbContext db) : IServerConfigRepository
{
    public async Task<ServerConfig> GetAsync() =>
        await db.ServerConfigs.FirstOrDefaultAsync() ?? new ServerConfig();

    public async Task SaveAsync(ServerConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var existing = await db.ServerConfigs.FirstOrDefaultAsync();
        if (existing is null)
            db.ServerConfigs.Add(config);
        else
        {
            db.Entry(existing).CurrentValues.SetValues(config);
            existing.Addresses = config.Addresses;
        }
        await db.SaveChangesAsync();
    }
}
