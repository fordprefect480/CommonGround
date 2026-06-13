using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class GetMemberStatsEndpoint(AppDbContext db)
    : EndpointWithoutRequest<GetMemberStatsEndpoint.Result>
{
    public sealed record Result(int ActiveMembers, int LapsedMembers, int NewMembersLast30Days);

    public override void Configure()
    {
        Get("/members/stats");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var activeMembers = await db.Users
            .Where(u => u.MembershipPaidThroughUtc >= now)
            .CountAsync(ct);

        var lapsedMembers = await db.Users
            .Where(u => u.MembershipPaidThroughUtc < now)
            .CountAsync(ct);

        var newMembersLast30Days = await db.Users
            .Where(u => u.JoinedAt >= thirtyDaysAgo)
            .CountAsync(ct);

        await Send.OkAsync(new Result(activeMembers, lapsedMembers, newMembersLast30Days), ct);
    }
}
