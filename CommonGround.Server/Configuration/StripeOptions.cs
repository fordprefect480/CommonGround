namespace CommonGround.Server.Configuration;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string Currency { get; set; } = "aud";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
