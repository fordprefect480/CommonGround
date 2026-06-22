using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Member;

/// <summary>Starts Stripe Checkout for the member's own awaiting-payment lease (fee &gt; $0).</summary>
public sealed class PayLeaseEndpoint(
    UserManager<ApplicationUser> userManager,
    LeasedBedCheckoutService checkout,
    AppDbContext db,
    ILogger<PayLeaseEndpoint> logger)
    : EndpointWithoutRequest<PayLeaseEndpoint.Result>
{
    public sealed record Result(string CheckoutUrl);

    public override void Configure()
    {
        Post("/leased-beds/leases/{leaseId:int}/pay");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!checkout.IsAvailable)
        {
            await Send.ResultAsync(Results.Problem(
                title: "Payments unavailable",
                detail: "Online payments are temporarily unavailable.",
                statusCode: 503));
            return;
        }

        var leaseId = Route<int>("leaseId");
        var lease = await db.BedLeases
            .Include(l => l.Bed)
            .SingleOrDefaultAsync(l => l.Id == leaseId && l.UserId == user.Id, ct);
        if (lease is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (lease.Status != BedLeaseStatus.AwaitingPayment)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "This lease isn't awaiting payment." }));
            return;
        }

        if (lease.PriceAtAllocationCents <= 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "No payment is required for this lease." }));
            return;
        }

        string checkoutUrl;
        try
        {
            checkoutUrl = await checkout.CreateCheckoutAsync(user, lease, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Stripe Checkout session for bed lease {LeaseId}", lease.Id);
            await Send.ResultAsync(Results.Problem(
                title: "Could not start payment",
                detail: "Sorry - something went wrong starting your payment. Please try again later.",
                statusCode: 502));
            return;
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new Result(checkoutUrl), ct);
    }
}
