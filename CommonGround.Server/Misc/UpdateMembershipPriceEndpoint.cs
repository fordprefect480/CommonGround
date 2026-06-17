using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Members;
using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class UpdateMembershipPriceEndpoint(
    SiteSettingsService settings,
    IActivityLogger activityLogger)
    : Endpoint<UpdateMembershipPriceEndpoint.Request, GetMembershipPriceEndpoint.Result>
{
    public sealed class Request
    {
        public int PriceCents { get; set; }
    }

    public override void Configure()
    {
        Put("/membership-price");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (!MembershipPrice.TryValidate(req.PriceCents, out var error))
        {
            await Send.ResultAsync(Results.BadRequest(new { error }));
            return;
        }

        await settings.SetMembershipPriceCentsAsync(req.PriceCents, ct);

        await activityLogger.LogAsync(
            "settings.membership_price.updated",
            $"set the membership price to ${req.PriceCents / 100m:0.00}",
            targetType: "SiteSettings",
            targetId: "1",
            ct: ct);

        await Send.OkAsync(new GetMembershipPriceEndpoint.Result(req.PriceCents), ct);
    }
}
