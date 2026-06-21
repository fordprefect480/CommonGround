using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Sends the leased-bed transactional emails. Failures are logged, never thrown -
/// the caller's action has already been persisted.
/// </summary>
public sealed class LeasedBedNotifications(
    IResend resend,
    IOptions<EmailOptions> emailOptions,
    IOptions<GardenOptions> gardenOptions,
    IOptions<LeasedBedsOptions> bedOptions,
    ILogger<LeasedBedNotifications> logger)
{
    /// <summary>Notifies the admin that a member applied (beds available) or joined the waiting list (full).</summary>
    public async Task SendApplicationReceivedAsync(string memberName, BedRequestStatus status, int remaining, int waitlistTotal, CancellationToken ct)
    {
        var recipient = bedOptions.Value.AdminNotificationEmail;
        if (!emailOptions.Value.IsConfigured || string.IsNullOrWhiteSpace(recipient))
        {
            return;
        }

        var (subject, html, text) = status == BedRequestStatus.Waitlisted
            ? (LeasedBedEmails.WaitlistSubject,
                LeasedBedEmails.BuildWaitlistedHtml(memberName, waitlistTotal),
                LeasedBedEmails.BuildWaitlistedText(memberName, waitlistTotal))
            : (LeasedBedEmails.AppliedSubject,
                LeasedBedEmails.BuildAppliedHtml(memberName, remaining),
                LeasedBedEmails.BuildAppliedText(memberName, remaining));

        await SendAsync(recipient, subject, html, text, ct);
    }

    /// <summary>Notifies the member they've been assigned a bed - payment-required or confirmed-free variant.</summary>
    public async Task SendAssignmentAsync(ApplicationUser member, string bedCode, DateOnly expiresOn, int amountCents, CancellationToken ct)
    {
        if (!emailOptions.Value.IsConfigured || string.IsNullOrWhiteSpace(member.Email))
        {
            return;
        }

        var name = member.DisplayName ?? "there";
        var fyLabel = $"{expiresOn.Year - 1}/{expiresOn.Year}";
        var profileUrl = string.IsNullOrWhiteSpace(gardenOptions.Value.PublicUrl)
            ? null
            : $"{gardenOptions.Value.PublicUrl.TrimEnd('/')}/profile";
        var amount = amountCents > 0 ? $"${amountCents / 100m:0.00}" : null;

        var html = LeasedBedEmails.BuildAssignmentHtml(name, bedCode, fyLabel, expiresOn, amount, profileUrl);
        var text = LeasedBedEmails.BuildAssignmentText(name, bedCode, fyLabel, expiresOn, amount, profileUrl);

        await SendAsync(member.Email, LeasedBedEmails.AssignedSubject, html, text, ct);
    }

    private async Task SendAsync(string to, string subject, string html, string text, CancellationToken ct)
    {
        var message = new EmailMessage
        {
            From = emailOptions.Value.From,
            Subject = subject,
            HtmlBody = html,
            TextBody = text,
        };
        message.To.Add(to);

        try
        {
            await resend.EmailSendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send leased-bed email to {Recipient}", to);
        }
    }
}
