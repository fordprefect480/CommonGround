namespace CommonGround.Server.Data;

/// <summary>
/// Single-row table (Id is always 1) holding editable site-wide settings.
/// </summary>
public class SiteSettings
{
    public int Id { get; set; }

    /// <summary>Annual membership price in the smallest currency unit (cents).</summary>
    public int MembershipPriceCents { get; set; }

    /// <summary>Default annual leased-bed price in the smallest currency unit (cents).</summary>
    public int LeasedBedPriceCents { get; set; }

    /// <summary>
    /// When true, the public site is gated behind an "under construction" page and only
    /// signed-in admins can view the real SPA. Used to preview the site before launch.
    /// </summary>
    public bool ComingSoon { get; set; }
}
