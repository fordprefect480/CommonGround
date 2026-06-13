using CommonGround.Server.Configuration;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CommonGround.Server.Members;

public sealed class StripeWebhookEndpoint(
    IOptions<StripeOptions> stripeOptions,
    MembershipActivationService activation,
    ILogger<StripeWebhookEndpoint> logger)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/membership/stripe-webhook");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var webhookSecret = stripeOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            await Send.ResultAsync(Results.StatusCode(503));
            return;
        }

        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync(ct);
        var signature = HttpContext.Request.Headers["Stripe-Signature"].ToString();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Rejected Stripe webhook with invalid signature");
            await Send.ResultAsync(Results.BadRequest());
            return;
        }

        if (stripeEvent.Data.Object is Session session)
        {
            if (stripeEvent.Type == "checkout.session.completed" && session.PaymentStatus == "paid")
            {
                await activation.ActivateAsync(session.Id, session.PaymentIntentId, ct);
            }
            else if (stripeEvent.Type == "checkout.session.expired")
            {
                await activation.ExpireAsync(session.Id, ct);
            }
        }

        await Send.OkAsync(ct);
    }
}
