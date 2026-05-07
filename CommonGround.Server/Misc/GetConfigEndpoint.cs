using CommonGround.Server.Configuration;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Misc;

public sealed class GetConfigEndpoint(IOptions<GardenOptions> garden)
    : EndpointWithoutRequest<GetConfigEndpoint.ConfigResult>
{
    public sealed record ConfigResult(string GardenName);

    public override void Configure()
    {
        Get("/config");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(new ConfigResult(garden.Value.Name), ct);
}
