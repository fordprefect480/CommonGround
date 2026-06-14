using CommonGround.Server.Data;
using CommonGround.Server.Members;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Account;

public sealed class ListMyPaymentsEndpoint(AppDbContext db, UserManager<ApplicationUser> userManager)
    : EndpointWithoutRequest<List<MembershipPaymentDto>>
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

        // Members see only their own successful payments — a clean receipts history.
        var payments = (await db.MembershipPayments
            .Where(p => p.UserId == current.Id && p.Status == MembershipPaymentStatus.Paid)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct))
            .Select(p => p.ToDto())
            .ToList();

        await Send.OkAsync(payments, ct);
    }
}
