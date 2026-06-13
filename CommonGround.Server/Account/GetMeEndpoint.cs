using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Account;

public sealed class GetMeEndpoint(UserManager<ApplicationUser> userManager, AppDbContext db)
    : EndpointWithoutRequest<MeDto>
{
    public override void Configure()
    {
        Get("/account/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        if (current is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var isAdmin = await userManager.IsInRoleAsync(current, AppRoles.Admin);
        var secondaryMembers = await db.SecondaryMembers
            .Where(s => s.UserId == current.Id)
            .OrderBy(s => s.Id)
            .Select(s => s.FullName)
            .ToArrayAsync(ct);

        await Send.OkAsync(new MeDto(
            current.Email,
            current.FirstName,
            current.LastName,
            current.DisplayName,
            current.PhoneNumber,
            current.Address,
            secondaryMembers,
            current.MembershipPaidThroughUtc,
            isAdmin,
            current.IsSubscribedToMailingList), ct);
    }
}
