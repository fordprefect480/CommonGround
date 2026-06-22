using CommonGround.Server.Data;
using CommonGround.Server.LeasedBeds;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Members;

public sealed class PayMembershipEndpoint(
    MembershipCheckoutService checkout,
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    ILogger<PayMembershipEndpoint> logger)
    : EndpointWithoutRequest<SignupResult>
{
    public override void Configure()
    {
        Post("/membership/pay");
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

        // Active members can pay only inside the renewal window (one month before expiry);
        // outside it, there's nothing to pay for.
        if (user.MembershipPaidThroughUtc is { } paidThrough && paidThrough > DateTime.UtcNow)
        {
            var today = FinancialYear.Today(DateTime.UtcNow);
            if (!FinancialYear.IsRenewalOpen(today, FinancialYear.Today(paidThrough)))
            {
                await Send.ResultAsync(Results.BadRequest(new { error = "Your membership is already active." }));
                return;
            }
        }

        string checkoutUrl;
        try
        {
            checkoutUrl = await checkout.CreateCheckoutAsync(user, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Stripe Checkout session for {Email}", user.Email);
            await Send.ResultAsync(Results.Problem(
                title: "Could not start payment",
                detail: "Sorry - something went wrong starting your payment. Please try again later.",
                statusCode: 502));
            return;
        }

        await db.SaveChangesAsync(ct);
        await Send.OkAsync(new SignupResult(checkoutUrl), ct);
    }
}
