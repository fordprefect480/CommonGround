using System.Text.Json.Serialization;

namespace CommonGround.Server.Misc;

public sealed class TurnstileVerifier(HttpClient http, ILogger<TurnstileVerifier> logger)
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    public async Task<bool> VerifyAsync(string secretKey, string token, string? remoteIp, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var form = new Dictionary<string, string>
        {
            ["secret"] = secretKey,
            ["response"] = token,
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            form["remoteip"] = remoteIp;
        }

        try
        {
            using var response = await http.PostAsync(VerifyUrl, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile verify returned HTTP {Status}", (int)response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(ct);
            if (result is null) return false;

            if (!result.Success)
            {
                logger.LogInformation("Turnstile token rejected: {Errors}",
                    result.ErrorCodes is null ? "(no codes)" : string.Join(",", result.ErrorCodes));
            }
            return result.Success;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Turnstile verification failed");
            return false;
        }
    }

    private sealed record SiteVerifyResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("error-codes")] IReadOnlyList<string>? ErrorCodes);
}
