using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ListMembersEndpoint(AppDbContext db)
    : EndpointWithoutRequest<List<MemberDto>>
{
    public override void Configure()
    {
        Get("/members");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await db.Users.OrderBy(u => u.Email).ToListAsync(ct);

        var rolePairs = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(ct);

        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToArray());

        var secondaryPairs = await db.SecondaryMembers
            .OrderBy(s => s.Id)
            .Select(s => new { s.UserId, s.FullName })
            .ToListAsync(ct);

        var secondaryByUser = secondaryPairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.FullName).ToArray());

        var lastPaymentByUser = await PaymentHistory.LastMembershipPaymentsAsync(db, ct);

        var members = users
            .Select(u => new MemberDto(
                u.Id, u.Email, u.UserName, u.FirstName, u.LastName, u.DisplayName,
                u.PhoneNumber, u.Address, u.JoinedAt, u.MembershipPaidThroughUtc, lastPaymentByUser.GetValueOrDefault(u.Id), u.EmailConfirmed,
                u.IsSubscribedToMailingList, secondaryByUser.GetValueOrDefault(u.Id, []), rolesByUser.GetValueOrDefault(u.Id, [])))
            .ToList();

        await Send.OkAsync(members, ct);
    }
}
