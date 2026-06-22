using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

/// <summary>Records a manual (cash/bank-transfer) membership payment of the given amount and advances the renewal date.</summary>
public sealed class RecordMembershipPaymentEndpoint(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IActivityLogger activityLogger)
    : Endpoint<RecordMembershipPaymentEndpoint.Request, MemberDto>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";

        /// <summary>The amount actually received, in cents. Set by the admin in the record-payment dialog.</summary>
        public int AmountCents { get; set; }
    }

    public override void Configure()
    {
        Post("/members/{id}/record-membership-payment");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (req.AmountCents < 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Enter an amount of $0 or more." }));
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTime.UtcNow;
        var periodEnd = MembershipPeriod.ComputePaidThrough(now);

        db.MembershipPayments.Add(new MembershipPayment
        {
            UserId = user.Id,
            Method = PaymentMethod.Manual,
            StripeCheckoutSessionId = "",
            AmountCents = req.AmountCents,
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
