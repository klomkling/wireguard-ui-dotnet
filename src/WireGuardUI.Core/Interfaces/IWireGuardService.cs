using WireGuardUI.Core.Models;

namespace WireGuardUI.Core.Interfaces;

public interface IWireGuardService
{
    Task<string> GenerateConfigAsync();
    Task WriteConfigAsync(string configContent);
    Task<ApplyResult> SyncConfAsync();
    Task<Result<WireGuardStatus>> GetStatusAsync();
}
