namespace CommonGround.Server.Configuration;

public sealed class ContactOptions
{
    public const string SectionName = "ContactForm";

    /// <summary>
    /// Email address that contact form submissions are delivered to.
    /// </summary>
    public string RecipientAddress { get; set; } = "";

    /// <summary>
    /// Cloudflare Turnstile site key, exposed to the frontend so it can render the widget.
    /// </summary>
    public string? TurnstileSiteKey { get; set; }

    /// <summary>
    /// Cloudflare Turnstile secret key, used server-side to verify the captcha token.
    /// </summary>
    public string? TurnstileSecretKey { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(RecipientAddress);

    public bool CaptchaEnabled =>
        !string.IsNullOrWhiteSpace(TurnstileSiteKey) && !string.IsNullOrWhiteSpace(TurnstileSecretKey);
}
