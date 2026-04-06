using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.WireGuard;
namespace WireGuardUI.Infrastructure.Email;

public class SmtpEmailService(IEmailSettingRepository emailSettingRepo) : IEmailService
{
    private static readonly HttpClient TokenHttpClient = new();

    public async Task<Result> SendClientConfigAsync(
        WireGuardClient client,
        ServerConfig server,
        GlobalSetting settings,
        string toEmail)
    {
        try
        {
            var emailSetting = await emailSettingRepo.GetAsync();
            var configContent = WireGuardConfigGenerator.GenerateClientConfig(client, server, settings);
            var fileName = $"{client.Name.Replace(" ", "_")}.conf";

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailSetting.FromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"WireGuard Config — {client.Name}";

            var builder = new BodyBuilder
            {
                TextBody = $"Please find your WireGuard configuration for '{client.Name}' attached."
            };
            builder.Attachments.Add(fileName, System.Text.Encoding.UTF8.GetBytes(configContent), ContentType.Parse("text/plain"));
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await ConnectAndAuthenticateAsync(smtp, emailSetting);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> TestConnectionAsync(EmailSetting emailSetting)
    {
        try
        {
            using var smtp = new SmtpClient();
            await ConnectAndAuthenticateAsync(smtp, emailSetting);
            await smtp.DisconnectAsync(true);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static async Task ConnectAndAuthenticateAsync(SmtpClient smtp, EmailSetting setting)
    {
        var smtpHost = NormalizeSmtpHost(setting.SmtpHost);
        var secureSocketOptions = setting.Encryption switch
        {
            "SSL" => SecureSocketOptions.SslOnConnect,
            "TLS" => SecureSocketOptions.SslOnConnect,
            "STARTTLS" => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None
        };

        await smtp.ConnectAsync(smtpHost, setting.SmtpPort, secureSocketOptions);

        if (setting.AuthType == "OAuth2")
        {
            if (string.IsNullOrWhiteSpace(setting.OAuth2ClientId) ||
                string.IsNullOrWhiteSpace(setting.OAuth2RefreshToken))
                throw new InvalidOperationException("OAuth2 ClientId and RefreshToken are required.");

            var accessToken = await GetOAuth2AccessTokenAsync(setting, smtpHost);

            var userName = string.IsNullOrWhiteSpace(setting.SmtpUsername)
                ? setting.FromAddress
                : setting.SmtpUsername;

            await smtp.AuthenticateAsync(new SaslMechanismOAuth2(userName, accessToken));
        }
        else if (!string.IsNullOrWhiteSpace(setting.SmtpUsername))
        {
            await smtp.AuthenticateAsync(setting.SmtpUsername, setting.SmtpPassword);
        }
    }

    private static async Task<string> GetOAuth2AccessTokenAsync(EmailSetting setting, string smtpHost)
    {
        var payload = new Dictionary<string, string>
        {
            ["client_id"] = setting.OAuth2ClientId,
            ["refresh_token"] = setting.OAuth2RefreshToken,
            ["grant_type"] = "refresh_token"
        };

        if (!string.IsNullOrWhiteSpace(setting.OAuth2ClientSecret))
            payload["client_secret"] = setting.OAuth2ClientSecret;

        if (IsMicrosoftHost(smtpHost))
            return await GetMicrosoftAccessTokenAsync(setting, payload);

        return await RequestAccessTokenAsync(
            providerName: "Google",
            tokenEndpoint: "https://oauth2.googleapis.com/token",
            payload: payload);
    }

    private static async Task<string> GetMicrosoftAccessTokenAsync(
        EmailSetting setting,
        Dictionary<string, string> payload)
    {
        payload["scope"] = "https://outlook.office.com/SMTP.Send offline_access";

        var tenant = NormalizeMicrosoftTenant(setting.OAuth2Tenant);
        var tokenEndpoint = $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token";
        return await RequestAccessTokenAsync("Microsoft", tokenEndpoint, payload);
    }

    private static string NormalizeMicrosoftTenant(string? tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant))
        {
            throw new InvalidOperationException(
                "Microsoft Account Type is required. Choose Personal (consumers) or Work/School (organizations).");
        }

        var normalized = tenant.Trim();
        if (normalized.Equals("common", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Microsoft Account Type 'common' is not supported. Choose Personal (consumers) or Work/School (organizations).");
        }

        return normalized;
    }

    private static async Task<string> RequestAccessTokenAsync(
        string providerName,
        string tokenEndpoint,
        Dictionary<string, string> payload)
    {
        var localPayload = new Dictionary<string, string>(payload, StringComparer.Ordinal);
        var response = await TokenHttpClient.PostAsync(
            tokenEndpoint,
            new FormUrlEncodedContent(localPayload));

        OAuth2TokenResponse? token = null;
        try
        {
            token = await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>();
        }
        catch
        {
            // ignore JSON parse errors, handled below
        }

        if (!response.IsSuccessStatusCode)
        {
            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            var detail = string.IsNullOrWhiteSpace(token?.ErrorDescription) ? status : token.ErrorDescription;

            // Microsoft public-client apps must not send client_secret.
            if (providerName.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                localPayload.ContainsKey("client_secret") &&
                (detail.Contains("AADSTS700025", StringComparison.OrdinalIgnoreCase) ||
                 detail.Contains("AADSTS90023", StringComparison.OrdinalIgnoreCase) ||
                 detail.Contains("Client is public", StringComparison.OrdinalIgnoreCase)))
            {
                localPayload.Remove("client_secret");
                response = await TokenHttpClient.PostAsync(
                    tokenEndpoint,
                    new FormUrlEncodedContent(localPayload));

                token = null;
                try
                {
                    token = await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>();
                }
                catch
                {
                    // ignore JSON parse errors, handled below
                }

                if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(token?.AccessToken))
                    return token.AccessToken!;

                status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                detail = string.IsNullOrWhiteSpace(token?.ErrorDescription) ? status : token.ErrorDescription;
            }

            if (providerName.Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
                detail = BuildFriendlyMicrosoftTokenError(detail);

            throw new InvalidOperationException($"{providerName} token refresh failed: {token?.Error ?? status} - {detail}");
        }

        token ??= await response.Content.ReadFromJsonAsync<OAuth2TokenResponse>()
            ?? throw new InvalidOperationException($"Empty response from {providerName} token endpoint.");

        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException($"{providerName} token refresh failed: {token.Error} - {token.ErrorDescription}");

        return token.AccessToken;
    }

    private static string BuildFriendlyMicrosoftTokenError(string detail)
    {
        if (detail.Contains("AADSTS90023", StringComparison.OrdinalIgnoreCase))
        {
            return "AADSTS90023: This Azure app is a public client and must not send client_secret. " +
                   "Clear OAuth2 Client Secret in Email Settings and try again. " +
                   detail;
        }

        if (detail.Contains("AADSTS40008", StringComparison.OrdinalIgnoreCase))
        {
            return "AADSTS40008: Microsoft Entra reported an unexpected error from an external identity provider. " +
                   "This is usually a tenant federation/IdP issue (not SMTP scope configuration). " +
                   "Ask your Microsoft 365 admin to verify domain federation/IdP health and SMTP AUTH policy. " +
                   detail;
        }

        return detail;
    }

    private static bool IsMicrosoftHost(string host) =>
        host.Contains("office365", StringComparison.OrdinalIgnoreCase) ||
        host.Contains("outlook", StringComparison.OrdinalIgnoreCase) ||
        host.Contains("hotmail", StringComparison.OrdinalIgnoreCase) ||
        host.Contains("live.com", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeSmtpHost(string? rawHost)
    {
        if (string.IsNullOrWhiteSpace(rawHost))
            throw new InvalidOperationException("SMTP Host is required.");

        var host = rawHost.Trim();

        // Allow users to paste full URI accidentally; keep only hostname.
        if (Uri.TryCreate(host, UriKind.Absolute, out var absoluteUri) &&
            !string.IsNullOrWhiteSpace(absoluteUri.Host))
        {
            host = absoluteUri.Host;
        }

        if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            throw new InvalidOperationException($"Invalid SMTP Host: '{rawHost}'. Use hostname only, e.g. smtp-mail.outlook.com");

        return host;
    }

    private sealed class OAuth2TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }
        [JsonPropertyName("error")]         public string? Error { get; init; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; init; }
    }
}
