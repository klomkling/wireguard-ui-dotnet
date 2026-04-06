using WireGuardUI.Core.Models;

namespace WireGuardUI.Infrastructure.WireGuard;

public sealed record ParsedWireGuardPeer(string? Name, Dictionary<string, string> Values);

public sealed record ParsedWireGuardConfig(
    Dictionary<string, string> InterfaceValues,
    List<ParsedWireGuardPeer> Peers);

public static class WireGuardConfigParser
{
    public static Result<ParsedWireGuardConfig> Parse(string content)
    {
        var interfaceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var peers = new List<ParsedWireGuardPeer>();

        Dictionary<string, string>? currentValues = null;
        string? currentSection = null;
        string? currentPeerName = null;

        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                if (currentSection == "Peer" && currentValues is not null)
                    peers.Add(new ParsedWireGuardPeer(currentPeerName, new Dictionary<string, string>(currentValues, StringComparer.OrdinalIgnoreCase)));

                currentPeerName = null;
                currentSection = line.Trim('[', ']');
                currentValues = currentSection.Equals("Interface", StringComparison.OrdinalIgnoreCase)
                    ? interfaceValues
                    : currentSection.Equals("Peer", StringComparison.OrdinalIgnoreCase)
                        ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        : null;
                continue;
            }

            if (currentValues is null)
                continue;

            if (currentSection == "Peer" && line.StartsWith("#", StringComparison.Ordinal))
            {
                var comment = line[1..].Trim();
                if (comment.StartsWith("Name", StringComparison.OrdinalIgnoreCase))
                {
                    var nameParts = comment.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (nameParts.Length == 2 && !string.IsNullOrWhiteSpace(nameParts[1]))
                        currentPeerName = nameParts[1];
                }
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal))
                continue;

            var kv = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
                continue;

            currentValues[kv[0]] = kv[1];
        }

        if (currentSection == "Peer" && currentValues is not null)
            peers.Add(new ParsedWireGuardPeer(currentPeerName, new Dictionary<string, string>(currentValues, StringComparer.OrdinalIgnoreCase)));

        if (interfaceValues.Count == 0)
            return Result<ParsedWireGuardConfig>.Failure("Invalid config: missing [Interface] section.");

        return Result<ParsedWireGuardConfig>.Success(new ParsedWireGuardConfig(interfaceValues, peers));
    }
}
