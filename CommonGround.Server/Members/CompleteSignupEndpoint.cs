using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Stripe.Checkout;

namespace CommonGround.Server.Members;

public sealed class CompleteSignupEndpoint(
    MembershipActivationService activation,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : Endpoint<CompleteRequest, CompleteResult>
{
    public override void Configure()
    {
        Post("/membership/complete");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CompleteRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.SessionId))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Missing session." }));
            return;
        }

        Session session;
        try
        {
            session = await new SessionService().GetAsync(req.SessionId, cancellationToken: ct);
        }
        catch (Stripe.StripeException)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Unknown payment session." }));
            return;
        }

        if (session.PaymentStatus != "paid")
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Payment not completed." }));
            return;
        }

        var userId = await activation.ActivateAsync(session.Id, session.PaymentIntentId, ct);
        if (userId is null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Unknown payment session." }));
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Account not found." }));
            return;
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        await Send.OkAsync(new CompleteResult(true), ct);
    }
}
