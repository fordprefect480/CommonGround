using CommonGround.Server.Auth;
using FastEndpoints;

namespace CommonGround.Server.Email;

public sealed class GetEmailQuotaEndpoint(EmailQuotaService quota)
    : EndpointWithoutRequest<EmailQuotaService.Snapshot>
{
    public override void Configure()
    {
        Get("/email/quota");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await quota.GetAsync(ct), ct);
}
