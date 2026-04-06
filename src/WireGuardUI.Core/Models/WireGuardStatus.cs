namespace WireGuardUI.Core.Models;

public record WireGuardStatus(
    string InterfaceName,
    string PublicKey,
    int ListenPort,
    List<WireGuardPeer> Peers);

public record WireGuardPeer(
    string PublicKey,
    string? Endpoint,
    List<string> AllowedIPs,
    string? LastHandshake,
    long RxBytes,
    long TxBytes);
