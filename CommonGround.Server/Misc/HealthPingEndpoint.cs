using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class HealthPingEndpoint
    : EndpointWithoutRequest<HealthPingEndpoint.PingResult>
{
    public sealed record PingResult(string Status);

    public override void Configure()
    {
        Get("/health/ping");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(new PingResult("ok"), ct);
}
