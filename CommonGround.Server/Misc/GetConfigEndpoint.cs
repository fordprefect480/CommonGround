using CommonGround.Server.Configuration;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Misc;

public sealed class GetConfigEndpoint(IOptions<GardenOptions> garden, IConfiguration configuration)
    : EndpointWithoutRequest<GetConfigEndpoint.ConfigResult>
{
    public sealed record ConfigResult(string GardenName, string? ApplicationInsightsConnectionString);

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

        return Send.OkAsync(
            new ConfigResult(garden.Value.Name, appInsightsConnectionString),
            ct);
    }
}
