using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

/// <summary>
/// Delivers queued bulk-email recipients (newsletters and multi-recipient membership mail)
/// within Resend's daily allowance. Both the send endpoint (for the first batch, straight
/// away) and <see cref="BulkEmailQueueWorker"/> (for later batches) go through here, so the
/// database's <see cref="SentEmailRecipientStatus.Queued"/> rows are the one source of truth
/// for what is still to go out.
/// </summary>
/// <remarks>
/// Queued recipients are delivered oldest email first, so a new send waits behind one that
/// is already part-way out. Each recipient's outcome is saved as soon as Resend answers, so
/// a restart mid-batch never re-sends to someone who already has the email.
/// </remarks>
public sealed class BulkEmailDispatcher(
    IResend resend,
    AppDbContext db,
    EmailQuotaService quota,
    IOptions<EmailOptions> options,
    UnsubscribeTokenService unsubscribeTokens,
    TimeProvider time,
    ILogger<BulkEmailDispatcher> logger)
{
    // The app runs as a single replica, so an in-process gate is enough to stop the endpoint
    // and the worker delivering the same queued recipient twice.
    private static readonly SemaphoreSlim DispatchGate = new(1, 1);

    /// <summary>Progress made on one email during a dispatch pass.</summary>
    public sealed record EmailProgress(long SentEmailId, string Subject, int SentThisPass, int SentTotal, int RecipientCount, int QueuedRemaining);

    /// <summary>
    /// Sends as many queued recipients as the remaining daily allowance permits. Stops early,
    /// leaving recipients queued, if Resend reports its quota or rate limit is exhausted or
    /// <paramref name="ct"/> is cancelled.
    /// </summary>
    public async Task<IReadOnlyList<EmailProgress>> DispatchQueuedAsync(CancellationToken ct)
    {
        await DispatchGate.WaitAsync(ct);
        try
        {
            return await DispatchCoreAsync(ct);
        }
        finally
        {
            DispatchGate.Release();
        }
    }

    private async Task<IReadOnlyList<EmailProgress>> DispatchCoreAsync(CancellationToken ct)
    {
        var snapshot = await quota.GetAsync(ct);
        var allowance = snapshot.BulkRemaining;
        var sentThisPass = new Dictionary<long, int>();
        var touched = new Dictionary<long, SentEmail>();

        while (allowance > 0 && !ct.IsCancellationRequested)
        {
            var batch = await db.SentEmailRecipients
                .Include(r => r.SentEmail)
                .Include(r => r.User)
                .Where(r => r.Status == SentEmailRecipientStatus.Queued)
                .OrderBy(r => r.SentEmailId)
                .ThenBy(r => r.Id)
                .Take(allowance)
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var recipient in batch)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var email = recipient.SentEmail;
                touched[email.Id] = email;

                if (SkipReason(email, recipient) is { } skipReason)
                {
                    recipient.Status = SentEmailRecipientStatus.Skipped;
                    recipient.ErrorMessage = skipReason;
                    email.QueuedCount--;
                    await db.SaveChangesAsync(CancellationToken.None);
                    continue;
                }

                var outcome = await SendAsync(email, recipient);
                if (outcome == Outcome.AllowanceExhausted)
                {
                    return Summarise(touched, sentThisPass);
                }

                email.QueuedCount--;
                if (outcome == Outcome.Sent)
                {
                    email.SentCount++;
                    sentThisPass[email.Id] = sentThisPass.GetValueOrDefault(email.Id) + 1;
                    allowance--;
                }
                else
                {
                    email.FailedCount++;
                }

                // Deliberately not cancellable: the email has already gone, so the record must be kept.
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }

        return Summarise(touched, sentThisPass);
    }

    private enum Outcome { Sent, Failed, AllowanceExhausted }

    private static string? SkipReason(SentEmail email, SentEmailRecipient recipient)
    {
        // Archived members are hidden by the Users query filter, so their navigation loads as null.
        if (recipient.UserId is not null && recipient.User is null)
        {
            return "Member was deleted before this email reached them.";
        }
        if (email.IsNewsletter && recipient.User is { IsSubscribedToMailingList: false })
        {
            return "Unsubscribed before this email reached them.";
        }
        return null;
    }

    private async Task<Outcome> SendAsync(SentEmail email, SentEmailRecipient recipient)
    {
        try
        {
            // Not cancellable: abandoning a request mid-flight would leave us unsure whether it was delivered.
            await resend.EmailSendAsync(BuildMessage(email, recipient), CancellationToken.None);
            recipient.Status = SentEmailRecipientStatus.Sent;
            recipient.SentAtUtc = time.GetUtcNow().UtcDateTime;
            recipient.ErrorMessage = null;
            return Outcome.Sent;
        }
        catch (ResendException ex) when (ex.ErrorType is ErrorType.DailyQuotaExceeded or ErrorType.MonthlyQuotaExceeded or ErrorType.RateLimitExceeded)
        {
            logger.LogWarning(ex, "Resend refused \"{Subject}\" ({ErrorType}); leaving the remaining recipients queued", email.Subject, ex.ErrorType);
            return Outcome.AllowanceExhausted;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send \"{Subject}\" to {Recipient}", email.Subject, recipient.Email);
            recipient.Status = SentEmailRecipientStatus.Failed;
            recipient.ErrorMessage = Truncate(ex.Message, 1000);
            return Outcome.Failed;
        }
    }

    private EmailMessage BuildMessage(SentEmail email, SentEmailRecipient recipient)
    {
        // Newsletters carry a per-recipient unsubscribe link and the List-Unsubscribe
        // headers (anti-spam law); membership emails don't.
        var unsubscribeUrl = email.IsNewsletter ? BuildUnsubscribeUrl(email, recipient) : null;

        var variables = new Dictionary<string, object> { ["BODY"] = email.HtmlBody };
        if (unsubscribeUrl is not null)
        {
            variables["RESEND_UNSUBSCRIBE_URL"] = unsubscribeUrl;
        }

        var message = new EmailMessage
        {
            From = options.Value.From,
            Subject = email.Subject,
            Template = new EmailMessageTemplate { TemplateId = options.Value.TemplateIdFor(email.IsNewsletter), Variables = variables },
        };
        message.To.Add(recipient.Email);

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

        return message;
    }

    private string BuildUnsubscribeUrl(SentEmail email, SentEmailRecipient recipient)
    {
        if (recipient.UserId is null)
        {
            return $"mailto:{options.Value.FromAddress}?subject=Unsubscribe";
        }
        var token = unsubscribeTokens.CreateToken(recipient.UserId);
        return $"{email.PublicBaseUrl}/unsubscribe?token={Uri.EscapeDataString(token)}";
    }

    private static IReadOnlyList<EmailProgress> Summarise(Dictionary<long, SentEmail> touched, Dictionary<long, int> sentThisPass) =>
        touched.Values
            .OrderBy(e => e.Id)
            .Select(e => new EmailProgress(e.Id, e.Subject, sentThisPass.GetValueOrDefault(e.Id), e.SentCount, e.RecipientCount, e.QueuedCount))
            .ToList();

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
