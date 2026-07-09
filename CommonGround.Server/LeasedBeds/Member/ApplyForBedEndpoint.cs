using CommonGround.Server.Activity;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Member;

/// <summary>
/// Single apply / join-waitlist action. The server re-checks availability and
/// routes to Pending (beds free) or Waitlisted (full), enforcing active membership,
/// one in-flight request per member, and no lease still awaiting payment.
/// </summary>
public sealed class ApplyForBedEndpoint(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    LeasedBedService beds,
    LeasedBedNotifications notifications,
    IActivityLogger activityLogger)
    : Endpoint<ApplyForBedEndpoint.Request, MyLeasedBedStatus>
{
    public sealed class Request
    {
        /// <summary>Set by the member when they need a wheelchair-accessible bed.</summary>
        public bool RequiresWheelchairAccessible { get; set; }
    }

    public override void Configure()
    {
        Post("/leased-beds/requests");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!(user.MembershipPaidThroughUtc > DateTime.UtcNow))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "You need an active membership to apply for a bed." }));
            return;
        }

        if (await beds.GetActiveRequestAsync(user.Id, ct) is not null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "You already have a bed request in progress." }));
            return;
        }

        if (await db.BedLeases.AnyAsync(l => l.UserId == user.Id && l.Status == BedLeaseStatus.AwaitingPayment, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Please pay for your assigned bed before applying for another." }));
            return;
        }

        var capacity = await beds.GetCapacityAsync(ct);
        var status = capacity.Remaining > 0 ? BedRequestStatus.Pending : BedRequestStatus.Waitlisted;

        db.BedRequests.Add(new BedRequest
        {
            UserId = user.Id,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            RequiresWheelchairAccessible = req.RequiresWheelchairAccessible,
        });
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "leased_bed.requested",
            status == BedRequestStatus.Pending ? "applied for a bed" : "joined the bed waitlist",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        var waitlistTotal = await db.BedRequests.CountAsync(r => r.Status == BedRequestStatus.Waitlisted, ct);
        await notifications.SendApplicationReceivedAsync(
            user.DisplayName ?? user.Email ?? "A member", status, capacity.Remaining, waitlistTotal, req.RequiresWheelchairAccessible, ct);

        await Send.OkAsync(await beds.GetMyStatusAsync(user, ct), ct);
    }
}
