namespace CommonGround.Server.Configuration;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public int PriceAmountCents { get; set; } = 2500;
    public string Currency { get; set; } = "aud";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
