namespace WireGuardUI.Core.Interfaces;

public interface IIpAllocationService
{
    string AllocateNextIp(string serverSubnet, IEnumerable<string> existingAllocatedIps);
}
