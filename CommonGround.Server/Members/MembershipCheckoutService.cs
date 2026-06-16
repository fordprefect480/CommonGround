using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace CommonGround.Server.Members;

/// <summary>
/// Creates the Stripe Checkout session and matching pending <see cref="MembershipPayment"/>
/// for a membership purchase. Shared by initial signup and later pay-from-profile payments.
/// </summary>
public sealed class MembershipCheckoutService(
    IOptions<StripeOptions> stripeOptions,
    IOptions<GardenOptions> gardenOptions,
    AppDbContext db)
{
    /// <summary>Whether online payments are currently configured and available.</summary>
    public bool IsAvailable => stripeOptions.Value.IsConfigured;

    /// <summary>
    /// Creates a Stripe Checkout session for <paramref name="user"/> and records a pending
    /// payment against it. The pending row is tracked but not saved - the caller owns the
    /// <see cref="AppDbContext.SaveChangesAsync(CancellationToken)"/> call. Returns the hosted
    /// checkout URL the browser should be sent to.
    /// </summary>
    public async Task<string> CreateCheckoutAsync(ApplicationUser user, CancellationToken ct)
    {
        var stripe = stripeOptions.Value;
        var session = await new SessionService().CreateAsync(BuildSessionOptions(user, stripe), cancellationToken: ct);

        db.MembershipPayments.Add(new MembershipPayment
        {
            UserId = user.Id,
            StripeCheckoutSessionId = session.Id,
            AmountCents = stripe.PriceAmountCents,
            Currency = stripe.Currency,
            Status = MembershipPaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });

        return session.Url;
    }

    private SessionCreateOptions BuildSessionOptions(ApplicationUser user, StripeOptions stripe)
    {
        var baseUrl = (gardenOptions.Value.PublicUrl ?? "").TrimEnd('/');
        return new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = user.Email,
            ClientReferenceId = user.Id,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = stripe.Currency,
                        UnitAmount = stripe.PriceAmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Annual garden membership",
                        },
                    },
                },
            ],
            SuccessUrl = $"{baseUrl}/membership/welcome?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/membership?canceled=1",
        };
    }
}
