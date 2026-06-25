using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ListMemberPaymentsEndpoint(AppDbContext db)
    : Endpoint<ListMemberPaymentsEndpoint.Request, List<PaymentDto>>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";
    }

    public override void Configure()
    {
        Get("/members/{id}/payments");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Admins see successful and failed payments (failures help with "I paid
        // but it didn't work" support); abandoned/expired checkouts (Pending) are
        // excluded as noise. Membership and leased-bed payments are merged, newest first.
        var membership = (await db.MembershipPayments
            .Where(p => p.UserId == req.Id
                && (p.Status == MembershipPaymentStatus.Paid || p.Status == MembershipPaymentStatus.Failed))
            .ToListAsync(ct))
            .Select(p => p.ToDto());

        var beds = (await db.BedLeasePayments
            .Where(p => p.UserId == req.Id
                && (p.Status == BedLeasePaymentStatus.Paid || p.Status == BedLeasePaymentStatus.Failed))
            .Select(p => new { Payment = p, BedLabel = p.BedLease!.Bed!.Label })
            .ToListAsync(ct))
            .Select(x => x.Payment.ToDto(x.BedLabel));

        var payments = membership.Concat(beds)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToList();

        await Send.OkAsync(payments, ct);
    }
}
