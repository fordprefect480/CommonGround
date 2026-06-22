namespace CommonGround.Server.Data;

/// <summary>How a payment was made. Existing Stripe rows default to <see cref="Stripe"/> (0).</summary>
public enum PaymentMethod
{
    Stripe = 0,
    Manual = 1,
}
