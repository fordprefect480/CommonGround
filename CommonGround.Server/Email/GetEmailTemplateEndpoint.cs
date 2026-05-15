using CommonGround.Server.Auth;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

public sealed class GetEmailTemplateEndpoint(
    IResend resend,
    IOptions<EmailOptions> options)
    : EndpointWithoutRequest<GetEmailTemplateEndpoint.Result>
{
    public sealed record Result(string HtmlBody);

    public override void Configure()
    {
        Get("/email/template");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!options.Value.IsConfigured)
        {
            await Send.ResultAsync(Results.Problem(
                title: "Email not configured",
                detail: "Set Email:ApiToken and Email:FromAddress to enable template retrieval.",
                statusCode: 503));
            return;
        }

        if (!Guid.TryParse(options.Value.TemplateBroadcastId, out var broadcastId))
        {
            await Send.ResultAsync(Results.Problem(
                title: "Template not configured",
                detail: "Set Email:TemplateBroadcastId to the Resend broadcast ID to use as the email template.",
                statusCode: 503));
            return;
        }

        var response = await resend.BroadcastRetrieveAsync(broadcastId, ct);
        await Send.OkAsync(new Result(response.Content.HtmlBody ?? ""), ct);
    }
}
