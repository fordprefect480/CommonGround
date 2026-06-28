using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.Misc;

public sealed class GetComingSoonEndpoint(SiteSettingsService settings)
    : EndpointWithoutRequest<GetComingSoonEndpoint.Result>
{
    public sealed record Result(bool ComingSoon);

    public override void Configure()
    {
        Get("/coming-soon");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var comingSoon = await settings.GetComingSoonAsync(ct);
        await Send.OkAsync(new Result(comingSoon), ct);
    }
}
