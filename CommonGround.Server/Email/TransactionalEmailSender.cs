using CommonGround.Server.Data;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Email;

/// <summary>
/// Sends a single transactional email (password reset, welcome, bed assignment,
/// admin notification, …) and records it in the sent-email history so it shows up
/// alongside newsletters in the admin Email list and, when addressed to a known
/// member, on that member's detail page.
/// </summary>
/// <remarks>
/// Delivery failures are logged and reflected in the recorded recipient's status,
/// never thrown - the caller's own action has already been persisted. Callers stay
/// responsible for the <see cref="EmailOptions.IsConfigured"/> check: when email is
/// not configured nothing is sent and nothing is recorded.
/// </remarks>
public sealed class TransactionalEmailSender(
    IResend resend,
    AppDbContext db,
    IOptions<EmailOptions> emailOptions,
    ILogger<TransactionalEmailSender> logger)
{
    /// <summary>A single recipient, with the member id attached when known.</summary>
    public sealed record Recipient(string Email, string? UserId = null);

    /// <summary>
    /// Sends <paramref name="recipient"/> the email and records a <see cref="SentEmail"/>
    /// with one <see cref="SentEmailRecipient"/>. Returns true if delivery succeeded.
    /// </summary>
    public async Task<bool> SendAsync(
        string subject,
        string htmlBody,
        string textBody,
        Recipient recipient,
        string? replyTo = null,
        CancellationToken ct = default)
    {
        var message = new EmailMessage
        {
            From = emailOptions.Value.From,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
        };
        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            message.ReplyTo = replyTo;
        }
        message.To.Add(recipient.Email);

        var record = new SentEmailRecipient
        {
            UserId = recipient.UserId,
            Email = recipient.Email,
        };

        var sentEmail = new SentEmail
        {
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
            RecipientCount = 1,
        };

        var delivered = false;
        try
        {
            await resend.EmailSendAsync(message, ct);
            record.Status = SentEmailRecipientStatus.Sent;
            sentEmail.SentCount = 1;
            delivered = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send transactional email \"{Subject}\" to {Recipient}", subject, recipient.Email);
            record.Status = SentEmailRecipientStatus.Failed;
            record.ErrorMessage = Truncate(ex.Message, 1000);
            sentEmail.FailedCount = 1;
        }

        sentEmail.Recipients.Add(record);

        try
        {
            db.SentEmails.Add(sentEmail);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // The email itself may already have gone out; don't let a history-write
            // failure surface to the caller.
            logger.LogError(ex, "Failed to record transactional email \"{Subject}\" in the sent-email history", subject);
        }

        return delivered;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
