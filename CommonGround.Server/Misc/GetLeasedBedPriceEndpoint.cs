using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class GetLeasedBedPriceEndpoint(SiteSettingsService settings)
    : EndpointWithoutRequest<GetLeasedBedPriceEndpoint.Result>
{
    public sealed record Result(int PriceCents);

    public override void Configure()
    {
        Get("/leased-bed-price");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var cents = await settings.GetLeasedBedPriceCentsAsync(ct);
        await Send.OkAsync(new Result(cents), ct);
    }
}
