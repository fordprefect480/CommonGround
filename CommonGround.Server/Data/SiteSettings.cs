namespace CommonGround.Server.Data;

/// <summary>
/// Single-row table (Id is always 1) holding editable site-wide settings.
/// </summary>
public class SiteSettings
{
    public int Id { get; set; }

    /// <summary>Annual membership price in the smallest currency unit (cents).</summary>
    public int MembershipPriceCents { get; set; }
}
