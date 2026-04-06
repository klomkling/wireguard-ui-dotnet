namespace WireGuardUI.Core.Models;

public record ApplyResult(bool Success, string? ErrorMessage = null, string? RawOutput = null)
{
    public static ApplyResult Ok(string? output = null) => new(true, null, output);
    public static ApplyResult Fail(string error, string? output = null) => new(false, error, output);
}
