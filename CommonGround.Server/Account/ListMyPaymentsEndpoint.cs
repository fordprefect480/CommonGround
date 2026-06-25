using CommonGround.Server.Data;
using CommonGround.Server.Members;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

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

        // Members see only their own successful payments — a clean receipts history, covering
        // both membership and leased-bed payments.
        var payments = await PaymentHistory.LoadAsync(db, current.Id, includeFailed: false, ct);

        await Send.OkAsync(payments, ct);
    }
}
