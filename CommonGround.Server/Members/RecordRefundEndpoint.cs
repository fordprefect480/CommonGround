using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

/// <summary>
/// Records a refund the admin has already made outside the app (in the Stripe dashboard, or in
/// cash) against one of a member's paid membership or leased-bed payments. A full refund of a
/// membership payment may roll the member's paid-through date back; see <see cref="MembershipRefund"/>.
/// </summary>
public sealed class RecordRefundEndpoint(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IActivityLogger activityLogger)
    : Endpoint<RecordRefundEndpoint.Request, MemberDto>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";

        public int PaymentId { get; set; }

        /// <summary>"Membership" or "LeasedBed", matching <see cref="PaymentDto.Kind"/>.</summary>
        public string Kind { get; set; } = "";

        /// <summary>The amount refunded, in cents. Must be more than zero and no more than what is left to refund.</summary>
        public int AmountCents { get; set; }
    }

    public override void Configure()
    {
        Post("/members/{id}/payments/{paymentId:int}/refund");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTime.UtcNow;
        string description;

        switch (req.Kind)
        {
            case "Membership":
            {
                var payments = await db.MembershipPayments.Where(p => p.UserId == user.Id).ToListAsync(ct);
                var payment = payments.SingleOrDefault(p => p.Id == req.PaymentId && p.Status == MembershipPaymentStatus.Paid);
                if (payment is null)
                {
                    await Send.NotFoundAsync(ct);
                    return;
                }

                if (await RejectAmountAsync(req.AmountCents, payment.AmountCents - payment.RefundedAmountCents))
                {
                    return;
                }

                payment.RefundedAmountCents += req.AmountCents;
                payment.RefundedAtUtc = now;
                user.MembershipPaidThroughUtc = MembershipRefund.RecalculatePaidThrough(user.MembershipPaidThroughUtc, payment, payments);
                description = "membership payment";
                break;
            }
            case "LeasedBed":
            {
                var payment = await db.BedLeasePayments
                    .SingleOrDefaultAsync(p => p.Id == req.PaymentId && p.UserId == user.Id && p.Status == BedLeasePaymentStatus.Paid, ct);
                if (payment is null)
                {
                    await Send.NotFoundAsync(ct);
                    return;
                }

                if (await RejectAmountAsync(req.AmountCents, payment.AmountCents - payment.RefundedAmountCents))
                {
                    return;
                }

                payment.RefundedAmountCents += req.AmountCents;
                payment.RefundedAtUtc = now;
                description = "leased-bed payment";
                break;
            }
            default:
                await Send.ResultAsync(Results.BadRequest(new { error = "Unknown payment type." }));
                return;
        }

        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "member.refund_recorded",
            $"recorded a ${req.AmountCents / 100m:0.00} refund of a {description} for {user.Email}",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        var roles = await userManager.GetRolesAsync(user);
        var secondaryMembers = await db.SecondaryMembers
            .Where(s => s.UserId == user.Id)
            .OrderBy(s => s.Id)
            .Select(s => s.FullName)
            .ToArrayAsync(ct);
        var lastPayment = await PaymentHistory.LastMembershipPaymentAsync(db, user.Id, ct);
        await Send.OkAsync(new MemberDto(
            user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
            user.PhoneNumber, user.Address, user.JoinedAt, user.MembershipPaidThroughUtc, lastPayment,
            user.EmailConfirmed, user.IsSubscribedToMailingList, secondaryMembers, roles.ToArray()), ct);
    }

    private async Task<bool> RejectAmountAsync(int amountCents, int refundableCents)
    {
        if (refundableCents <= 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "This payment has already been fully refunded." }));
            return true;
        }

        if (amountCents <= 0 || amountCents > refundableCents)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"Enter an amount between $0.01 and ${refundableCents / 100m:0.00}." }));
            return true;
        }

        return false;
    }
}
