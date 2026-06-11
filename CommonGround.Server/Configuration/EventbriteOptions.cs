namespace CommonGround.Server.Configuration;

public sealed class EventbriteOptions
{
    public const string SectionName = "Eventbrite";

    /// <summary>
    /// Eventbrite private OAuth token used to call /v3/organizations/{id}/events/.
    /// Issued at https://www.eventbrite.com/platform/api-keys/. Leave blank to disable
    /// the Eventbrite integration entirely.
    /// </summary>
    public string? PrivateToken { get; set; }

    /// <summary>
    /// Eventbrite organization (organizer) ID whose upcoming events should be displayed.
    /// Visible in the organizer profile URL, e.g.
    /// https://www.eventbrite.com.au/o/{slug}-{organizationId}.
    /// </summary>
    public string? OrganizationId { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PrivateToken) && !string.IsNullOrWhiteSpace(OrganizationId);
}
