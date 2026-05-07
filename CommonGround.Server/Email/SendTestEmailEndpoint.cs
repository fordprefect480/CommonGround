using CommonGround.Server.Auth;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

public sealed class SendTestEmailEndpoint(IResend resend, IOptions<EmailOptions> options)
    : Endpoint<SendTestEmailEndpoint.Request, SendTestEmailEndpoint.Result>
{
    public sealed record Request(string To);
    public sealed record Result(string Id);

    public override void Configure()
    {
        Post("/email/test");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (!options.Value.IsConfigured)
        {
            await Send.ResultAsync(Results.Problem(
                title: "Email not configured",
                detail: "Set Email:ApiToken and Email:FromAddress to enable sending.",
                statusCode: 503));
            return;
        }

        if (string.IsNullOrWhiteSpace(req.To))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "To address is required." }));
            return;
        }

        var message = new EmailMessage
        {
            From = options.Value.From,
            Subject = "Test email from Common Ground",
            HtmlBody = "<p>If you can read this, Resend is wired up correctly.</p>",
            TextBody = "If you can read this, Resend is wired up correctly.",
        };
        message.To.Add(req.To.Trim());

        var result = await resend.EmailSendAsync(message, ct);

        await Send.OkAsync(new Result(result.Content.ToString()), ct);
    }
}
