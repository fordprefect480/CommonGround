using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Sends the leased-bed transactional emails. Failures are logged, never thrown -
/// the caller's action has already been persisted.
/// </summary>
public sealed class LeasedBedNotifications(
    TransactionalEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    IOptions<GardenOptions> gardenOptions,
    IOptions<LeasedBedsOptions> bedOptions)
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
                LeasedBedEmails.BuildWaitlistedHtml(memberName, waitlistTotal, gardenOptions.Value.PublicUrl),
                LeasedBedEmails.BuildWaitlistedText(memberName, waitlistTotal, gardenOptions.Value.PublicUrl))
            : (LeasedBedEmails.AppliedSubject,
                LeasedBedEmails.BuildAppliedHtml(memberName, remaining, gardenOptions.Value.PublicUrl),
                LeasedBedEmails.BuildAppliedText(memberName, remaining, gardenOptions.Value.PublicUrl));

        // Sent to the admin notification address, so no member is attached.
        await emailSender.SendAsync(subject, html, text, new TransactionalEmailSender.Recipient(recipient), ct: ct);
    }

    /// <summary>Notifies the member they've been assigned a bed - payment-required or confirmed-free variant.</summary>
    public async Task SendAssignmentAsync(ApplicationUser member, string bedLabel, DateOnly expiresOn, int amountCents, CancellationToken ct)
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

        var html = LeasedBedEmails.BuildAssignmentHtml(name, bedLabel, fyLabel, expiresOn, amount, profileUrl);

        await emailSender.SendMembershipAsync(
            LeasedBedEmails.AssignedSubject, html,
            new TransactionalEmailSender.Recipient(member.Email, member.Id),
            ct: ct);
    }
}
