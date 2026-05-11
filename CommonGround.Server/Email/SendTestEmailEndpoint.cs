using AngleSharp;
using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Blog;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

public sealed class SendTestEmailEndpoint(
    IResend resend,
    IOptions<EmailOptions> options,
    BlogHtmlSanitizer sanitizer,
    IActivityLogger activityLogger,
    ILogger<SendTestEmailEndpoint> logger)
    : Endpoint<SendTestEmailEndpoint.Request, SendTestEmailEndpoint.Result>
{
    public sealed record Request(string Subject, string HtmlBody, IReadOnlyList<string> Recipients);
    public sealed record Failure(string Email, string Error);
    public sealed record Result(int Sent, int Failed, IReadOnlyList<Failure> Failures);

    private const int MaxRecipients = 20;

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

        var subject = (req.Subject ?? "").Trim();
        if (subject.Length == 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Subject is required." }));
            return;
        }

        var html = sanitizer.Sanitize((req.HtmlBody ?? "").Trim());
        var textBody = await HtmlToTextAsync(html, ct);
        if (textBody.Length == 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Body is required." }));
            return;
        }

        var recipients = (req.Recipients ?? [])
            .Select(r => (r ?? "").Trim())
            .Where(r => r.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (recipients.Count == 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "At least one recipient is required." }));
            return;
        }

        if (recipients.Count > MaxRecipients)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"Too many recipients (max {MaxRecipients})." }));
            return;
        }

        var invalid = recipients.FirstOrDefault(r => !LooksLikeEmail(r));
        if (invalid is not null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"\"{invalid}\" is not a valid email address." }));
            return;
        }

        var sent = 0;
        var failures = new List<Failure>();

        foreach (var to in recipients)
        {
            ct.ThrowIfCancellationRequested();
            var message = new EmailMessage
            {
                From = options.Value.From,
                Subject = subject,
                HtmlBody = html,
                TextBody = textBody,
            };
            message.To.Add(to);

            try
            {
                await resend.EmailSendAsync(message, ct);
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send test email to {Recipient}", to);
                failures.Add(new Failure(to, Truncate(ex.Message, 500)));
            }
        }

        await activityLogger.LogAsync(
            "email.test_sent",
            $"Sent test email \"{subject}\" to {sent} of {recipients.Count} recipient(s)",
            details: new { Subject = subject, Sent = sent, Failed = failures.Count, Recipients = recipients },
            ct: ct);

        await Send.OkAsync(new Result(sent, failures.Count, failures), ct);
    }

    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1 && !value.Contains(' ');
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static async Task<string> HtmlToTextAsync(string html, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var doc = await context.OpenAsync(r => r.Content(html), ct);
        var text = doc.Body?.TextContent ?? "";
        return string.Join("\n", text
            .Split('\n', StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));
    }
}
