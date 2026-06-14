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

        // "Paid" means the membership fee is covered for the current financial
        // year; membership always runs to a 1 July boundary, so a paid-through
        // date in the future is exactly that. Everyone else — never paid, or
        // paid only through a past year — is "not yet paid".
        var totalMembers = await db.Users.CountAsync(ct);

        var paidMembers = await db.Users
            .Where(u => u.MembershipPaidThroughUtc >= now)
            .CountAsync(ct);

        var notYetPaidMembers = totalMembers - paidMembers;

        var newMembersLast30Days = await db.Users
            .Where(u => u.JoinedAt >= thirtyDaysAgo)
            .CountAsync(ct);

        await Send.OkAsync(new Result(paidMembers, notYetPaidMembers, newMembersLast30Days), ct);
    }
}
