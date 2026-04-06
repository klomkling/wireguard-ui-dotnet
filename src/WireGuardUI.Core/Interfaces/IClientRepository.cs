using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IClientRepository
{
    Task<List<WireGuardClient>> GetAllAsync();
    Task<WireGuardClient?> GetByIdAsync(string id);
    Task AddAsync(WireGuardClient client);
    Task UpdateAsync(WireGuardClient client);
    Task DeleteAsync(string id);
}
