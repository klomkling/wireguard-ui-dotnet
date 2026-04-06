using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IServerConfigRepository
{
    Task<ServerConfig> GetAsync();
    Task SaveAsync(ServerConfig config);
}
