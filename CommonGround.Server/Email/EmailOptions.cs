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

    /// <summary>Resend's sending cap: the most emails accepted in any 24-hour period.</summary>
    public int DailyLimit { get; set; } = 100;

    /// <summary>
    /// Slice of <see cref="DailyLimit"/> that bulk sends never use, so welcome, password-reset and
    /// bed-assignment emails can still go out on a day a newsletter has used the rest.
    /// </summary>
    public int TransactionalReserve { get; set; } = 10;

    /// <summary>How many bulk-send recipients can go out in any 24-hour period.</summary>
    public int BulkDailyAllowance => Math.Max(0, DailyLimit - TransactionalReserve);

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
