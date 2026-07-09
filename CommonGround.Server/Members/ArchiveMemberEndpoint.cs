using System.Security.Claims;
using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ArchiveMemberEndpoint(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IActivityLogger activityLogger)
    : Endpoint<ArchiveMemberEndpoint.Request>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";
    }

    public override void Configure()
    {
        Delete("/members/{id}");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Guard: an admin can't archive their own account (avoids self-lockout).
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            await SendBadRequest("You can't delete your own account.");
            return;
        }

        // Guard: don't leave the site with zero administrators. Only relevant when
        // the target is an admin; GetUsersInRoleAsync returns non-archived admins,
        // and the target isn't archived yet, so it's still counted here.
        if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (admins.All(a => a.Id == user.Id))
            {
                await SendBadRequest("This is the only administrator; make someone else an admin first.");
                return;
            }
        }

        // Free any bed the member holds - releasing every lease that still occupies one.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var leases = await db.BedLeases
            .Where(l => l.UserId == user.Id && l.Status != BedLeaseStatus.Released)
            .ToListAsync(ct);
        foreach (var lease in leases)
        {
            lease.Status = BedLeaseStatus.Released;
            lease.EndDate = today;
        }

        user.ArchivedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // Invalidate any live session at the next security-stamp check.
        await userManager.UpdateSecurityStampAsync(user);

        await activityLogger.LogAsync(
            "member.archived",
            $"deleted the member {user.Email}",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        await Send.NoContentAsync(ct);
    }

    private Task SendBadRequest(string error) =>
        Send.ResultAsync(Results.BadRequest(new { error }));
}
