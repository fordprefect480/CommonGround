using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

public sealed class SendNewsletterEndpoint(
    IResend resend,
    IOptions<EmailOptions> options,
    IOptions<GardenOptions> gardenOptions,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IActivityLogger activityLogger,
    UnsubscribeTokenService unsubscribeTokens,
    ILogger<SendNewsletterEndpoint> logger)
    : Endpoint<SendNewsletterEndpoint.Request, SendNewsletterEndpoint.Result>
{
    public sealed record Request(
        string Subject,
        string HtmlBody,
        string? Mode,
        IReadOnlyList<string>? MemberIds,
        IReadOnlyList<string>? Emails,
        bool? IsNewsletter);

    public sealed record Result(long Id, int Sent, int Failed);

    private const string ModeAllSubscribers = "all_subscribers";
    private const string ModeSpecificMembers = "specific_members";
    private const string ModeCustomEmails = "custom_emails";

    private sealed record ResolvedRecipient(string? UserId, string Email);

    public override void Configure()
    {
        Post("/email/newsletter");
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
        var body = (req.HtmlBody ?? "").Trim();

        if (subject.Length == 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Subject is required." }));
            return;
        }

        if (body.Length == 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Body is required." }));
            return;
        }

        var isNewsletter = req.IsNewsletter ?? true;
        var templateId = options.Value.TemplateIdFor(isNewsletter);

        var mode = string.IsNullOrWhiteSpace(req.Mode) ? ModeAllSubscribers : req.Mode!.Trim();
        List<ResolvedRecipient> recipients;
        switch (mode)
        {
            case ModeAllSubscribers:
                recipients = await db.Users
                    .Where(u => u.IsSubscribedToMailingList && u.Email != null && u.Email != "")
                    .Select(u => new ResolvedRecipient(u.Id, u.Email!))
                    .ToListAsync(ct);
                if (recipients.Count == 0)
                {
                    await Send.ResultAsync(Results.BadRequest(new { error = "There are no subscribed members." }));
                    return;
                }
                break;

            case ModeSpecificMembers:
                var ids = (req.MemberIds ?? [])
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToArray();
                if (ids.Length == 0)
                {
                    await Send.ResultAsync(Results.BadRequest(new { error = "Select at least one member." }));
                    return;
                }
                recipients = await db.Users
                    .Where(u => ids.Contains(u.Id) && u.Email != null && u.Email != "")
                    .Select(u => new ResolvedRecipient(u.Id, u.Email!))
                    .ToListAsync(ct);
                if (recipients.Count == 0)
                {
                    await Send.ResultAsync(Results.BadRequest(new { error = "None of the selected members have a valid email address." }));
                    return;
                }
                break;

            case ModeCustomEmails:
                var emails = (req.Emails ?? [])
                    .Select(e => (e ?? "").Trim())
                    .Where(e => e.Length > 0)
                    .GroupBy(e => e, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
                if (emails.Count == 0)
                {
                    await Send.ResultAsync(Results.BadRequest(new { error = "Enter at least one email address." }));
                    return;
                }
                recipients = emails.Select(e => new ResolvedRecipient(null, e)).ToList();
                break;

            default:
                await Send.ResultAsync(Results.BadRequest(new { error = $"Unknown recipient mode \"{mode}\"." }));
                return;
        }

        string? senderUserId = null;
        string? senderEmail = null;
        var http = httpContextAccessor.HttpContext;
        if (http?.User is { Identity.IsAuthenticated: true })
        {
            var user = await userManager.GetUserAsync(http.User);
            if (user is not null)
            {
                senderUserId = user.Id;
                senderEmail = user.Email;
            }
        }

        var publicBaseUrl = ResolvePublicBaseUrl(gardenOptions.Value.PublicUrl, http);

        string BuildUnsubscribeUrl(ResolvedRecipient r)
        {
            if (r.UserId is null)
            {
                return $"mailto:{options.Value.FromAddress}?subject=Unsubscribe";
            }
            var token = unsubscribeTokens.CreateToken(r.UserId);
            return $"{publicBaseUrl}/unsubscribe?token={Uri.EscapeDataString(token)}";
        }

        var sentEmail = new SentEmail
        {
            Subject = subject,
            HtmlBody = body,
            SenderUserId = senderUserId,
            SenderEmailSnapshot = senderEmail,
            IsNewsletter = isNewsletter,
            RecipientCount = recipients.Count,
        };

        var sent = 0;
        var failed = 0;

        foreach (var r in recipients)
        {
            ct.ThrowIfCancellationRequested();

            // Newsletters carry a per-recipient unsubscribe link and the List-Unsubscribe
            // headers (anti-spam law); membership emails don't.
            var unsubscribeUrl = isNewsletter ? BuildUnsubscribeUrl(r) : null;

            var variables = new Dictionary<string, object> { ["BODY"] = body };
            if (unsubscribeUrl is not null)
            {
                variables["RESEND_UNSUBSCRIBE_URL"] = unsubscribeUrl;
            }

            var message = new EmailMessage
            {
                From = options.Value.From,
                Subject = subject,
                Template = new EmailMessageTemplate { TemplateId = templateId, Variables = variables },
            };
            message.To.Add(r.Email);

            if (unsubscribeUrl is not null)
            {
                message.Headers = new Dictionary<string, string>
                {
                    ["List-Unsubscribe"] = $"<{unsubscribeUrl}>",
                };
                if (unsubscribeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    message.Headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";
                }
            }

            var record = new SentEmailRecipient
            {
                UserId = r.UserId,
                Email = r.Email,
            };

            try
            {
                await resend.EmailSendAsync(message, ct);
                record.Status = SentEmailRecipientStatus.Sent;
                sent++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send newsletter to {Recipient}", r.Email);
                record.Status = SentEmailRecipientStatus.Failed;
                record.ErrorMessage = Truncate(ex.Message, 1000);
                failed++;
            }

            sentEmail.Recipients.Add(record);
        }

        sentEmail.SentCount = sent;
        sentEmail.FailedCount = failed;

        db.SentEmails.Add(sentEmail);
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "email.newsletter_sent",
            $"sent the newsletter \"{subject}\" to {sent} of {recipients.Count} recipient(s)",
            targetType: "sentEmail",
            targetId: sentEmail.Id.ToString(),
            details: new { Subject = subject, Sent = sent, Failed = failed, Recipients = recipients.Count, Mode = mode, IsNewsletter = isNewsletter },
            ct: ct);

        await Send.OkAsync(new Result(sentEmail.Id, sent, failed), ct);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string ResolvePublicBaseUrl(string? configured, HttpContext? http)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }
        return http is not null ? $"{http.Request.Scheme}://{http.Request.Host}" : "";
    }
}
