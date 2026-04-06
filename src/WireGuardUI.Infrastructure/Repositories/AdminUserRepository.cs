using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class AdminUserRepository(AppDbContext db) : IAdminUserRepository
{
    public async Task<AdminUser> GetAsync() =>
        await db.AdminUsers.FirstOrDefaultAsync() ?? new AdminUser();

    public async Task SaveAsync(AdminUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var existing = await db.AdminUsers.FirstOrDefaultAsync();
        if (existing is null)
            db.AdminUsers.Add(user);
        else
            db.Entry(existing).CurrentValues.SetValues(user);
        await db.SaveChangesAsync();
    }
}
