using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using CommonGround.Server.Configuration;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Events;

public sealed class EventbriteClient(
    HttpClient http,
    IOptionsMonitor<EventbriteOptions> options,
    ILogger<EventbriteClient> logger)
{
    private const string BaseUrl = "https://www.eventbriteapi.com/v3/";

    public async Task<IReadOnlyList<EventbriteEvent>> ListUpcomingAsync(int take, CancellationToken ct)
    {
        var opts = options.CurrentValue;
        if (!opts.IsConfigured) return [];

        // TEMPORARY (testing): fetch the 3 most recent PAST events instead of upcoming.
        // Revert to status=live&order_by=start_asc&time_filter=current_future&page_size={Math.Clamp(take, 1, 50)}.
        var url = $"{BaseUrl}organizations/{Uri.EscapeDataString(opts.OrganizationId!)}/events/?order_by=start_desc&time_filter=past&expand=logo,venue&page_size=3";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.PrivateToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Eventbrite list returned HTTP {Status}: {Body}", (int)response.StatusCode, body.Length > 400 ? body[..400] : body);
                return [];
            }

            var payload = await response.Content.ReadFromJsonAsync<EventbriteListResponse>(ct);
            return payload?.Events ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Eventbrite list request failed");
            return [];
        }
    }

    private sealed record EventbriteListResponse(
        [property: JsonPropertyName("events")] List<EventbriteEvent>? Events);
}

public sealed record EventbriteEvent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] EventbriteText? Name,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("description")] EventbriteText? Description,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("start")] EventbriteDateTime? Start,
    [property: JsonPropertyName("end")] EventbriteDateTime? End,
    [property: JsonPropertyName("logo")] EventbriteLogo? Logo);

public sealed record EventbriteText([property: JsonPropertyName("text")] string? Text);

public sealed record EventbriteDateTime([property: JsonPropertyName("utc")] DateTime? Utc);

public sealed record EventbriteLogo(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("original")] EventbriteLogoOriginal? Original);

public sealed record EventbriteLogoOriginal([property: JsonPropertyName("url")] string? Url);
