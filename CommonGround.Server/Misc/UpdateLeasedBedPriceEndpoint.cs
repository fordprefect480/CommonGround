using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.LeasedBeds;
using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class UpdateLeasedBedPriceEndpoint(
    SiteSettingsService settings,
    IActivityLogger activityLogger)
    : Endpoint<UpdateLeasedBedPriceEndpoint.Request, GetLeasedBedPriceEndpoint.Result>
{
    public sealed class Request
    {
        public int PriceCents { get; set; }
    }

    public override void Configure()
    {
        Put("/leased-bed-price");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (!LeasedBedPrice.TryValidate(req.PriceCents, out var error))
        {
            await Send.ResultAsync(Results.BadRequest(new { error }));
            return;
        }

        await settings.SetLeasedBedPriceCentsAsync(req.PriceCents, ct);

        await activityLogger.LogAsync(
            "settings.leased_bed_price.updated",
            $"set the leased bed price to ${req.PriceCents / 100m:0.00}",
            targetType: "SiteSettings",
            targetId: "1",
            ct: ct);

        await Send.OkAsync(new GetLeasedBedPriceEndpoint.Result(req.PriceCents), ct);
    }
}
