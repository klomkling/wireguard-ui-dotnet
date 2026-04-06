using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser> GetAsync();
    Task SaveAsync(AdminUser user);
}
