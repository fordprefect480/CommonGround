using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Email;

public sealed class SendNewsletterEndpoint(
    BulkEmailDispatcher dispatcher,
    IOptions<EmailOptions> options,
    IOptions<GardenOptions> gardenOptions,
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IActivityLogger activityLogger)
    : Endpoint<SendNewsletterEndpoint.Request, SendNewsletterEndpoint.Result>
{
    public sealed record Request(
        string Subject,
        string HtmlBody,
        string? Mode,
        IReadOnlyList<string>? MemberIds,
        IReadOnlyList<string>? Emails,
        bool? IsNewsletter);

    public sealed record Result(long Id, int Sent, int Failed, int Queued);

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

        var sentEmail = new SentEmail
        {
            Subject = subject,
            HtmlBody = body,
            SenderUserId = senderUserId,
            SenderEmailSnapshot = senderEmail,
            IsNewsletter = isNewsletter,
            RecipientCount = recipients.Count,
            QueuedCount = recipients.Count,
            PublicBaseUrl = ResolvePublicBaseUrl(gardenOptions.Value.PublicUrl, http),
        };
        foreach (var r in recipients)
        {
            sentEmail.Recipients.Add(new SentEmailRecipient
            {
                UserId = r.UserId,
                Email = r.Email,
                Status = SentEmailRecipientStatus.Queued,
            });
        }

        db.SentEmails.Add(sentEmail);
        await db.SaveChangesAsync(ct);

        // Everything is queued first; this sends as much as today's allowance allows and the
        // queue worker delivers the rest as allowance frees up.
        await dispatcher.DispatchQueuedAsync(ct);

        var sent = sentEmail.SentCount;
        var failed = sentEmail.FailedCount;
        var queued = sentEmail.QueuedCount;

        var activitySummary = queued == 0
            ? $"sent the newsletter \"{subject}\" to {sent} of {recipients.Count} recipient(s)"
            : $"sent the newsletter \"{subject}\" to {sent} of {recipients.Count} recipient(s); {queued} will go out automatically as the daily email allowance frees up";

        await activityLogger.LogAsync(
            "email.newsletter_sent",
            activitySummary,
            targetType: "sentEmail",
            targetId: sentEmail.Id.ToString(),
            details: new { Subject = subject, Sent = sent, Failed = failed, Queued = queued, Recipients = recipients.Count, Mode = mode, IsNewsletter = isNewsletter },
            ct: ct);

        await Send.OkAsync(new Result(sentEmail.Id, sent, failed, queued), ct);
    }

    private static string ResolvePublicBaseUrl(string? configured, HttpContext? http)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }
        return http is not null ? $"{http.Request.Scheme}://{http.Request.Host}" : "";
    }
}
