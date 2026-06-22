using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace CommonGround.Server.LeasedBeds;

/// <summary>
/// Creates the Stripe Checkout session and matching pending <see cref="BedLeasePayment"/>
/// for a leased-bed fee. Mirrors the membership checkout; the session carries a
/// <c>kind=bed-lease</c> metadata tag so the shared webhook can route it.
/// </summary>
public sealed class LeasedBedCheckoutService(
    IOptions<StripeOptions> stripeOptions,
    IOptions<GardenOptions> gardenOptions,
    AppDbContext db)
{
    /// <summary>Whether online payments are currently configured and available.</summary>
    public bool IsAvailable => stripeOptions.Value.IsConfigured;

    /// <summary>
    /// Creates a Checkout session for <paramref name="lease"/> and records a pending payment
    /// against it. The pending row is tracked but not saved - the caller owns SaveChanges.
    /// Returns the hosted checkout URL.
    /// </summary>
    public async Task<string> CreateCheckoutAsync(ApplicationUser user, BedLease lease, CancellationToken ct)
    {
        var stripe = stripeOptions.Value;
        var session = await new SessionService().CreateAsync(BuildSessionOptions(user, lease, stripe), cancellationToken: ct);

        db.BedLeasePayments.Add(new BedLeasePayment
        {
            UserId = user.Id,
            BedLeaseId = lease.Id,
            Method = PaymentMethod.Stripe,
            StripeCheckoutSessionId = session.Id,
            AmountCents = lease.PriceAtAllocationCents,
            Currency = stripe.Currency,
            Status = BedLeasePaymentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });

        return session.Url;
    }

    private SessionCreateOptions BuildSessionOptions(ApplicationUser user, BedLease lease, StripeOptions stripe)
    {
        var baseUrl = (gardenOptions.Value.PublicUrl ?? "").TrimEnd('/');
        return new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = user.Email,
            ClientReferenceId = user.Id,
            Metadata = new Dictionary<string, string>
            {
                ["kind"] = "bed-lease",
                ["leaseId"] = lease.Id.ToString(),
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = stripe.Currency,
                        UnitAmount = lease.PriceAtAllocationCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Leased garden bed {lease.Bed?.Label}",
                        },
                    },
                },
            ],
            SuccessUrl = $"{baseUrl}/profile?payment=success",
            CancelUrl = $"{baseUrl}/profile?payment=cancelled",
        };
    }
}
