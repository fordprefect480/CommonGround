using CommonGround.Server.Data;
using CommonGround.Server.Members;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Account;

public sealed class ListMyPaymentsEndpoint(AppDbContext db, UserManager<ApplicationUser> userManager)
    : EndpointWithoutRequest<List<PaymentDto>>
{
    public override void Configure()
    {
        Get("/account/me/payments");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        if (current is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Members see only their own successful payments — a clean receipts history. Both
        // membership and leased-bed payments are included, newest first.
        var membership = (await db.MembershipPayments
            .Where(p => p.UserId == current.Id && p.Status == MembershipPaymentStatus.Paid)
            .ToListAsync(ct))
            .Select(p => p.ToDto());

        var beds = (await db.BedLeasePayments
            .Where(p => p.UserId == current.Id && p.Status == BedLeasePaymentStatus.Paid)
            .Select(p => new { Payment = p, BedLabel = p.BedLease!.Bed!.Label })
            .ToListAsync(ct))
            .Select(x => x.Payment.ToDto(x.BedLabel));

        var payments = membership.Concat(beds)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();

        await Send.OkAsync(payments, ct);
    }
}
