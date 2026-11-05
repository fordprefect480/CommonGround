using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

/// <summary>
/// Soft-deletes ("archives") members: frees their garden beds, hides them via the
/// ArchivedAtUtc query filter and invalidates their sessions. Shared by the single
/// and bulk delete endpoints so both apply the same guards.
/// </summary>
public sealed class MemberArchiveService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IActivityLogger activityLogger)
{
    /// <summary>
    /// Returns an error message if archiving <paramref name="users"/> would lock the
    /// current admin out or leave the site with no administrators; otherwise null.
    /// </summary>
    public async Task<string?> ValidateAsync(IReadOnlyCollection<ApplicationUser> users, string? currentUserId)
    {
        var single = users.Count == 1;

        if (users.Any(u => u.Id == currentUserId))
            return single ? "You can't delete your own account." : "You can't delete your own account. Untick yourself and try again.";

        var targetIds = users.Select(u => u.Id).ToHashSet();
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        if (admins.Count > 0 && admins.All(a => targetIds.Contains(a.Id)))
            return single ? "This is the only administrator; make someone else an admin first." : "That would remove every administrator; untick at least one admin, or make someone else an admin first.";

        return null;
    }

    public static string ArchivedUserName(string userId) => $"archived-{userId}";

    public async Task ArchiveAsync(IReadOnlyCollection<ApplicationUser> users, CancellationToken ct)
    {
        var targetIds = users.Select(u => u.Id).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var leases = await db.BedLeases
            .Where(l => targetIds.Contains(l.UserId) && l.Status != BedLeaseStatus.Released)
            .ToListAsync(ct);
        foreach (var lease in leases)
        {
            lease.Status = BedLeaseStatus.Released;
            lease.EndDate = today;
        }

        var now = DateTime.UtcNow;
        foreach (var user in users)
        {
            user.ArchivedAtUtc = now;
            // The username is the email and UserNameIndex is unique: free it so they can re-join.
            user.UserName = ArchivedUserName(user.Id);
            user.NormalizedUserName = userManager.NormalizeName(user.UserName);
        }
        await db.SaveChangesAsync(ct);

        foreach (var user in users)
        {
            // Invalidate any live session at the next security-stamp check.
            await userManager.UpdateSecurityStampAsync(user);
            await activityLogger.LogAsync("member.archived", $"deleted the member {user.Email}", targetType: "Member", targetId: user.Id, ct: ct);
        }
    }
}
