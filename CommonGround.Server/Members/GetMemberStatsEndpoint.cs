using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class GetMemberStatsEndpoint(AppDbContext db)
    : EndpointWithoutRequest<GetMemberStatsEndpoint.Result>
{
    public sealed record Result(int PaidMembers, int NotYetPaidMembers, int NewMembersLast30Days);

    public override void Configure()
    {
        Get("/members/stats");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        // "Paid" means the membership fee is covered through the upcoming
        // renewal boundary - the paid-through date a payment made now would
        // reach (the next 1 July, with the late-join carry-over). Everyone else
        // - never paid, lapsed, or covered only for the current year but not yet
        // renewed for the year ahead - is "not yet paid". This matches the admin
        // Members page so the Dashboard count and the page's chips agree.
        var renewalTarget = MembershipPeriod.ComputePaidThrough(now);

        var totalMembers = await db.Users.CountAsync(ct);

        var paidMembers = await db.Users
            .Where(u => u.MembershipPaidThroughUtc >= renewalTarget)
            .CountAsync(ct);

        var notYetPaidMembers = totalMembers - paidMembers;

        var newMembersLast30Days = await db.Users
            .Where(u => u.JoinedAt >= thirtyDaysAgo)
            .CountAsync(ct);

        await Send.OkAsync(new Result(paidMembers, notYetPaidMembers, newMembersLast30Days), ct);
    }
}
