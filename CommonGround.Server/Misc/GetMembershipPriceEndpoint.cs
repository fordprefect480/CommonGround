using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class GetMembershipPriceEndpoint(SiteSettingsService settings)
    : EndpointWithoutRequest<GetMembershipPriceEndpoint.Result>
{
    public sealed record Result(int PriceCents);

    public override void Configure()
    {
        Get("/membership-price");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var cents = await settings.GetMembershipPriceCentsAsync(ct);
        await Send.OkAsync(new Result(cents), ct);
    }
}
