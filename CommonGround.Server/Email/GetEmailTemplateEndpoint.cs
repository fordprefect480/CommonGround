using CommonGround.Server.Auth;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

public sealed class GetEmailTemplateEndpoint(
    IResend resend,
    IOptions<EmailOptions> options,
    ILogger<GetEmailTemplateEndpoint> logger)
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

        if (!Guid.TryParse(options.Value.TemplateId, out var templateId))
        {
            await Send.ResultAsync(Results.Problem(
                title: "Template not configured",
                detail: "Set Email:TemplateId to the Resend template ID to use as the email template.",
                statusCode: 503));
            return;
        }

        try
        {
            var response = await resend.TemplateRetrieveAsync(templateId, ct);
            await Send.OkAsync(new Result(response.Content.HtmlBody ?? ""), ct);
        }
        catch (ResendException ex)
        {
            logger.LogWarning(ex, "Failed to retrieve Resend template {TemplateId}", templateId);
            await Send.ResultAsync(Results.Problem(
                title: "Template unavailable",
                detail: $"Resend couldn't return template {templateId}: {ex.Message}. Check that Email:TemplateId points to a template that exists in the Resend account associated with the current API token.",
                statusCode: 502));
        }
    }
}
