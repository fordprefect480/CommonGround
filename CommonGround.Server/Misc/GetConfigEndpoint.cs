using CommonGround.Server.Configuration;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Misc;

public sealed class GetConfigEndpoint(
    IOptions<GardenOptions> garden,
    IOptions<ContactOptions> contact,
    IConfiguration configuration)
    : EndpointWithoutRequest<GetConfigEndpoint.ConfigResult>
{
    public sealed record ConfigResult(
        string GardenName,
        string? ApplicationInsightsConnectionString,
        string? TurnstileSiteKey,
        string? Version,
        string? CommitSha);

    public override void Configure()
    {
        Get("/config");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var appInsightsConnectionString =
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? configuration.GetConnectionString("appinsights");

        var turnstileSiteKey = string.IsNullOrWhiteSpace(contact.Value.TurnstileSiteKey)
            ? null
            : contact.Value.TurnstileSiteKey;

        var version = string.IsNullOrWhiteSpace(configuration["BuildInfo:Version"])
            ? null
            : configuration["BuildInfo:Version"];

        var commitSha = string.IsNullOrWhiteSpace(configuration["BuildInfo:CommitSha"])
            ? null
            : configuration["BuildInfo:CommitSha"];

        return Send.OkAsync(
            new ConfigResult(
                garden.Value.Name, appInsightsConnectionString, turnstileSiteKey, version, commitSha),
            ct);
    }
}
