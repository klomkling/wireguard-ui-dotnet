namespace WireGuardUI.Core.Interfaces;

public interface IKeyPairService
{
    (string PrivateKey, string PublicKey) GenerateKeyPair();
    string GeneratePresharedKey();
    string GetPublicKeyFromPrivateKey(string privateKey);
}
