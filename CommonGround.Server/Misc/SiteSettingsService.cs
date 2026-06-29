using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Misc;

/// <summary>
/// Reads and updates the single <see cref="SiteSettings"/> row. The row is seeded by
/// migration, so it always exists.
/// </summary>
public sealed class SiteSettingsService(AppDbContext db)
{
    public Task<int> GetMembershipPriceCentsAsync(CancellationToken ct) =>
        db.SiteSettings.AsNoTracking().Select(s => s.MembershipPriceCents).SingleAsync(ct);

    /// <summary>
    /// Updates the membership price. Caller is responsible for validating the amount first.
    /// </summary>
    public async Task SetMembershipPriceCentsAsync(int cents, CancellationToken ct)
    {
        var settings = await db.SiteSettings.SingleAsync(ct);
        settings.MembershipPriceCents = cents;
        await db.SaveChangesAsync(ct);
    }

    public Task<bool> GetComingSoonAsync(CancellationToken ct) =>
        db.SiteSettings.AsNoTracking().Select(s => s.ComingSoon).SingleAsync(ct);

    /// <summary>
    /// Toggles the "coming soon" gate. When enabled, only signed-in admins can view the SPA;
    /// everyone else sees the under-construction page.
    /// </summary>
    public async Task SetComingSoonAsync(bool comingSoon, CancellationToken ct)
    {
        var settings = await db.SiteSettings.SingleAsync(ct);
        settings.ComingSoon = comingSoon;
        await db.SaveChangesAsync(ct);
    }

    public Task<int> GetLeasedBedPriceCentsAsync(CancellationToken ct) =>
        db.SiteSettings.AsNoTracking().Select(s => s.LeasedBedPriceCents).SingleAsync(ct);

    /// <summary>
    /// Updates the default leased-bed price. Caller is responsible for validating the amount first.
    /// </summary>
    public async Task SetLeasedBedPriceCentsAsync(int cents, CancellationToken ct)
    {
        var settings = await db.SiteSettings.SingleAsync(ct);
        settings.LeasedBedPriceCents = cents;
        await db.SaveChangesAsync(ct);
    }
}
