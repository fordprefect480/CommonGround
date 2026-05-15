namespace CommonGround.Server.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? ApiToken { get; set; }
    public string FromAddress { get; set; } = "";
    public string? FromName { get; set; }
    public string? TemplateId { get; set; }

    public string From =>
        string.IsNullOrWhiteSpace(FromName) ? FromAddress : $"{FromName} <{FromAddress}>";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiToken) && !string.IsNullOrWhiteSpace(FromAddress);
}
