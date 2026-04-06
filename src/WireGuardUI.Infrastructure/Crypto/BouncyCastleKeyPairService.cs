using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using WireGuardUI.Core.Interfaces;
namespace WireGuardUI.Infrastructure.Crypto;

public class BouncyCastleKeyPairService : IKeyPairService
{
    public (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        var generator = new X25519KeyPairGenerator();
        generator.Init(new X25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = generator.GenerateKeyPair();

        var privateKey = (X25519PrivateKeyParameters)keyPair.Private;
        var publicKey = (X25519PublicKeyParameters)keyPair.Public;

        return (
            Convert.ToBase64String(privateKey.GetEncoded()),
            Convert.ToBase64String(publicKey.GetEncoded())
        );
    }

    public string GeneratePresharedKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }

    public string GetPublicKeyFromPrivateKey(string privateKey)
    {
        var privateKeyBytes = Convert.FromBase64String(privateKey);
        var privateKeyParams = new X25519PrivateKeyParameters(privateKeyBytes, 0);
        return Convert.ToBase64String(privateKeyParams.GeneratePublicKey().GetEncoded());
    }
}
