using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.LeasedBeds.Admin;

public sealed class GetBedRequestsEndpoint(LeasedBedService beds)
    : EndpointWithoutRequest<AdminBedRequests>
{
    public override void Configure()
    {
        Get("/requests");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await beds.GetRequestsAsync(ct), ct);
    }
}
