using WireGuardUI.Core.Services;
namespace WireGuardUI.Tests.Core;

public class IpAllocationServiceTests
{
    private readonly IpAllocationService _sut = new();

    [Fact]
    public void AllocateNextIp_EmptySubnet_ReturnsFirstHostIp()
    {
        var result = _sut.AllocateNextIp("10.252.1.0/24", []);
        Assert.Equal("10.252.1.1/32", result);
    }

    [Fact]
    public void AllocateNextIp_FirstTwoTaken_ReturnsThirdHostIp()
    {
        var result = _sut.AllocateNextIp("10.252.1.0/24", ["10.252.1.1/32", "10.252.1.2/32"]);
        Assert.Equal("10.252.1.3/32", result);
    }

    [Fact]
    public void AllocateNextIp_SkipsExistingIpsWithoutCidrSuffix()
    {
        var result = _sut.AllocateNextIp("10.252.1.0/24", ["10.252.1.1"]);
        Assert.Equal("10.252.1.2/32", result);
    }

    [Fact]
    public void AllocateNextIp_SubnetFull_ThrowsInvalidOperationException()
    {
        // /30 has 2 usable hosts: .1 and .2
        var existing = new[] { "10.0.0.1/32", "10.0.0.2/32" };
        Assert.Throws<InvalidOperationException>(() =>
            _sut.AllocateNextIp("10.0.0.0/30", existing));
    }
}
