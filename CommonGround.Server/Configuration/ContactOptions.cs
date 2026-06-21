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
    public string? TurnstileSiteKey
    {
        get;
        set => field = Normalize(value);
    }

    /// <summary>
    /// Cloudflare Turnstile secret key, used server-side to verify the captcha token.
    /// </summary>
    public string? TurnstileSecretKey
    {
        get;
        set => field = Normalize(value);
    }

    // Config values are often pasted by hand, so trim stray whitespace and treat blanks as
    // unset. A leading space on the site key makes Turnstile reject it and the widget fails
    // to render; the same on the secret key silently breaks server-side verification.
    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(RecipientAddress);

    public bool CaptchaEnabled =>
        !string.IsNullOrWhiteSpace(TurnstileSiteKey) && !string.IsNullOrWhiteSpace(TurnstileSecretKey);
}
