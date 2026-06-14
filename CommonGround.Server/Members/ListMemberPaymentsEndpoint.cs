using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ListMemberPaymentsEndpoint(AppDbContext db)
    : Endpoint<ListMemberPaymentsEndpoint.Request, List<MembershipPaymentDto>>
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
        // excluded as noise.
        var payments = (await db.MembershipPayments
            .Where(p => p.UserId == req.Id
                && (p.Status == MembershipPaymentStatus.Paid || p.Status == MembershipPaymentStatus.Failed))
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct))
            .Select(p => p.ToDto())
            .ToList();

        await Send.OkAsync(payments, ct);
    }
}
