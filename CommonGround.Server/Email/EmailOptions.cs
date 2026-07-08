namespace CommonGround.Server.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? ApiToken { get; set; }
    public string FromAddress { get; set; } = "";
    public string? FromName { get; set; }

    /// <summary>Resend template used for newsletters (bulk, opt-out, unsubscribe link).</summary>
    public string? TemplateId { get; set; }

    /// <summary>Resend template used for transactional/membership mail (welcomes, password reset, bed assignment).</summary>
    public string? TransactionalTemplateId { get; set; }

    public string From =>
        string.IsNullOrWhiteSpace(FromName) ? FromAddress : $"{FromName} <{FromAddress}>";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiToken) && !string.IsNullOrWhiteSpace(FromAddress);

    /// <summary>
    /// Resolves the published Resend template for the given audience: newsletters use
    /// <see cref="TemplateId"/>, everything else uses <see cref="TransactionalTemplateId"/>.
    /// Both are required configuration.
    /// </summary>
    public Guid TemplateIdFor(bool isNewsletter) =>
        Guid.Parse((isNewsletter ? TemplateId : TransactionalTemplateId)!);
}
