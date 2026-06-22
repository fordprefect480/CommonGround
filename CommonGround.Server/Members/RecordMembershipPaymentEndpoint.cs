using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

/// <summary>Records an offline (cash/bank-transfer) membership payment and advances the renewal date.</summary>
public sealed class RecordMembershipPaymentEndpoint(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    SiteSettingsService settings,
    IActivityLogger activityLogger)
    : EndpointWithoutRequest<MemberDto>
{
    public override void Configure()
    {
        Post("/members/{id}/record-membership-payment");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id");
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTime.UtcNow;
        var periodEnd = MembershipPeriod.ComputePaidThrough(now);
        var amount = await settings.GetMembershipPriceCentsAsync(ct);

        db.MembershipPayments.Add(new MembershipPayment
        {
            UserId = user.Id,
            Method = PaymentMethod.Manual,
            StripeCheckoutSessionId = "",
            AmountCents = amount,
            Currency = "aud",
            Status = MembershipPaymentStatus.Paid,
            CreatedAtUtc = now,
            PaidAtUtc = now,
            PeriodStartUtc = now,
            PeriodEndUtc = periodEnd,
        });

        user.MembershipPaidThroughUtc = periodEnd;
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "member.payment_recorded",
            $"recorded an offline membership payment for {user.Email}",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        var roles = await userManager.GetRolesAsync(user);
        await Send.OkAsync(new MemberDto(
            user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
            user.PhoneNumber, user.JoinedAt, user.MembershipPaidThroughUtc, user.EmailConfirmed,
            user.IsSubscribedToMailingList, roles.ToArray()), ct);
    }
}
