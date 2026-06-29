using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class UpdateComingSoonEndpoint(
    SiteSettingsService settings,
    IActivityLogger activityLogger)
    : Endpoint<UpdateComingSoonEndpoint.Request, GetComingSoonEndpoint.Result>
{
    public sealed class Request
    {
        public bool ComingSoon { get; set; }
    }

    public override void Configure()
    {
        Put("/coming-soon");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        await settings.SetComingSoonAsync(req.ComingSoon, ct);

        await activityLogger.LogAsync(
            "settings.coming_soon.updated",
            req.ComingSoon
                ? "turned on the under-construction page (site visible to admins only)"
                : "turned off the under-construction page (site is now public)",
            targetType: "SiteSettings",
            targetId: "1",
            ct: ct);

        await Send.OkAsync(new GetComingSoonEndpoint.Result(req.ComingSoon), ct);
    }
}
