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

        // "Paid" means the member is paid up for the financial year the next
        // renewal covers (the year ending at ComputePaidThrough - the next
        // 1 July, with the late-join carry-over). We test coverage past that
        // year's START so a member covered through to its end counts even though
        // their paid-through instant may sit just before the exact boundary;
        // everyone else - never paid, lapsed, or covered only for the prior year
        // - is "not yet paid". This matches the admin Members page so the
        // Dashboard count and the page's chips agree.
        var renewalYearStart = MembershipPeriod.ComputePaidThrough(now).AddYears(-1);

        var totalMembers = await db.Users.CountAsync(ct);

        var paidMembers = await db.Users
            .Where(u => u.MembershipPaidThroughUtc > renewalYearStart)
            .CountAsync(ct);

        var notYetPaidMembers = totalMembers - paidMembers;

        var newMembersLast30Days = await db.Users
            .Where(u => u.JoinedAt >= thirtyDaysAgo)
            .CountAsync(ct);

        await Send.OkAsync(new Result(paidMembers, notYetPaidMembers, newMembersLast30Days), ct);
    }
}
