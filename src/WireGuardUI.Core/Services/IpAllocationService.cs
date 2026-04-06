using System.Net;
using WireGuardUI.Core.Interfaces;
namespace WireGuardUI.Core.Services;

public class IpAllocationService : IIpAllocationService
{
    public string AllocateNextIp(string serverSubnet, IEnumerable<string> existingAllocatedIps)
    {
        var parts = serverSubnet.Split('/');
        var networkAddress = IPAddress.Parse(parts[0]);
        var prefixLength = int.Parse(parts[1]);

        var bytes = networkAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        var networkUint = BitConverter.ToUInt32(bytes, 0);
        var hostCount = (uint)(1 << (32 - prefixLength));

        var existingSet = existingAllocatedIps
            .Select(ip => ip.Split('/')[0])
            .ToHashSet();

        // Skip network (.0) and broadcast (last) addresses
        for (uint i = 1; i < hostCount - 1; i++)
        {
            var candidateUint = networkUint + i;
            var candidateBytes = BitConverter.GetBytes(candidateUint);
            if (BitConverter.IsLittleEndian) Array.Reverse(candidateBytes);
            var candidate = new IPAddress(candidateBytes).ToString();

            if (!existingSet.Contains(candidate))
                return $"{candidate}/32";
        }

        throw new InvalidOperationException($"No available IPs in subnet {serverSubnet}.");
    }
}
