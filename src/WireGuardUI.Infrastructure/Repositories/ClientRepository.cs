using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class ClientRepository(AppDbContext db) : IClientRepository
{
    public Task<List<WireGuardClient>> GetAllAsync() =>
        db.Clients.AsNoTracking().OrderBy(c => c.CreatedAt).ToListAsync();

    public Task<WireGuardClient?> GetByIdAsync(string id) =>
        db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(WireGuardClient client)
    {
        db.Clients.Add(client);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(WireGuardClient client)
    {
        var tracked = db.Clients.Local.FirstOrDefault(c => c.Id == client.Id);
        var target = tracked ?? await db.Clients.FirstOrDefaultAsync(c => c.Id == client.Id);

        if (target is null)
        {
            client.UpdatedAt = DateTime.UtcNow;
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return;
        }

        target.Name = client.Name;
        target.Email = client.Email;
        target.PrivateKey = client.PrivateKey;
        target.PublicKey = client.PublicKey;
        target.PresharedKey = client.PresharedKey;
        target.AllocatedIPs = [..client.AllocatedIPs];
        target.AllowedIPs = [..client.AllowedIPs];
        target.ExtraAllowedIPs = [..client.ExtraAllowedIPs];
        target.Endpoint = client.Endpoint;
        target.UseServerDns = client.UseServerDns;
        target.Enabled = client.Enabled;
        target.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is not null)
        {
            db.Clients.Remove(client);
            await db.SaveChangesAsync();
        }
    }
}
