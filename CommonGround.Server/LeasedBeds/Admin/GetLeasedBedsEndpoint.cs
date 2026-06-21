using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.LeasedBeds.Admin;

public sealed class GetLeasedBedsEndpoint(LeasedBedService beds)
    : EndpointWithoutRequest<LeasedBedsOverview>
{
    public override void Configure()
    {
        Get("");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
