using CommonGround.Server.Activity;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Members;

/// <summary>
/// Confirms a pending membership payment exactly once. Invoked by both the Stripe
/// webhook and the browser return trip; the pending->paid transition is guarded by an
/// atomic conditional update so concurrent callers cannot double-activate or double-email.
/// </summary>
public sealed class MembershipActivationService(
    AppDbContext db,
    TransactionalEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    IOptions<GardenOptions> gardenOptions,
    IActivityLogger activityLogger,
    ILogger<MembershipActivationService> logger)
{
    /// <summary>
    /// Marks the payment for <paramref name="checkoutSessionId"/> as paid, sets the member's
    /// renewal date, and sends the welcome email. Returns the activated user's id, or null if
    /// no matching payment exists. Safe to call multiple times for the same session.
    /// </summary>
    public async Task<string?> ActivateAsync(string checkoutSessionId, string? paymentIntentId, CancellationToken ct)
    {
        var payment = await db.MembershipPayments
            .SingleOrDefaultAsync(p => p.StripeCheckoutSessionId == checkoutSessionId, ct);
        if (payment is null)
        {
            logger.LogWarning("No membership payment found for checkout session {SessionId}", checkoutSessionId);
            return null;
        }

        if (payment.Status == MembershipPaymentStatus.Paid)
        {
            return payment.UserId; // already activated
        }

        var now = DateTime.UtcNow;
        var periodEnd = MembershipPeriod.ComputePaidThrough(now);

        var rows = await db.MembershipPayments
            .Where(p => p.Id == payment.Id && p.Status == MembershipPaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, MembershipPaymentStatus.Paid)
                .SetProperty(p => p.PaidAtUtc, now)
                .SetProperty(p => p.StripePaymentIntentId, paymentIntentId)
                .SetProperty(p => p.PeriodStartUtc, now)
                .SetProperty(p => p.PeriodEndUtc, periodEnd), ct);

        if (rows == 0)
        {
            return payment.UserId; // another caller won the race
        }

        var user = await db.Users.SingleAsync(u => u.Id == payment.UserId, ct);
        user.MembershipPaidThroughUtc = periodEnd;
        await db.SaveChangesAsync(ct);

        await SendWelcomeEmailAsync(user, ct);

        await activityLogger.LogAsync(
            "member.joined",
            $"{user.Email} joined as a member",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        return user.Id;
    }

    /// <summary>
    /// Marks a still-pending payment for <paramref name="checkoutSessionId"/> as failed when its
    /// Stripe Checkout session expires. No-op if the payment is already paid or absent.
    /// </summary>
    public async Task ExpireAsync(string checkoutSessionId, CancellationToken ct)
    {
        await db.MembershipPayments
            .Where(p => p.StripeCheckoutSessionId == checkoutSessionId && p.Status == MembershipPaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, MembershipPaymentStatus.Failed), ct);
    }

    private async Task SendWelcomeEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        if (!emailOptions.Value.IsConfigured || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var signInUrl = string.IsNullOrWhiteSpace(gardenOptions.Value.PublicUrl)
            ? null
            : $"{gardenOptions.Value.PublicUrl.TrimEnd('/')}/login";

        await emailSender.SendMembershipAsync(
            MembershipWelcomeEmail.Subject,
            MembershipWelcomeEmail.BuildHtml(user.FirstName, signInUrl),
            new TransactionalEmailSender.Recipient(user.Email, user.Id),
            ct: ct);
    }
}
