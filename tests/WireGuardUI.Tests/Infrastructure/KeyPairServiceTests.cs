using WireGuardUI.Infrastructure.Crypto;
namespace WireGuardUI.Tests.Infrastructure;

public class KeyPairServiceTests
{
    private readonly BouncyCastleKeyPairService _sut = new();

    [Fact]
    public void GenerateKeyPair_ReturnsBase64EncodedKeys()
    {
        var (privateKey, publicKey) = _sut.GenerateKeyPair();

        Assert.NotEmpty(privateKey);
        Assert.NotEmpty(publicKey);
        // WireGuard keys are 32 bytes = 44 chars base64 (with padding)
        Assert.Equal(44, privateKey.Length);
        Assert.Equal(44, publicKey.Length);
        // Must be valid base64
        Assert.True(IsValidBase64(privateKey));
        Assert.True(IsValidBase64(publicKey));
    }

    [Fact]
    public void GenerateKeyPair_EachCallProducesDifferentKeys()
    {
        var (priv1, _) = _sut.GenerateKeyPair();
        var (priv2, _) = _sut.GenerateKeyPair();
        Assert.NotEqual(priv1, priv2);
    }

    [Fact]
    public void GeneratePresharedKey_Returns44CharBase64()
    {
        var key = _sut.GeneratePresharedKey();
        Assert.Equal(44, key.Length);
        Assert.True(IsValidBase64(key));
    }

    private static bool IsValidBase64(string s)
    {
        try { Convert.FromBase64String(s); return true; }
        catch { return false; }
    }
}
