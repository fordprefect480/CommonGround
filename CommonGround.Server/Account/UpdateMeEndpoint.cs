using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Account;

public sealed class UpdateMeEndpoint(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IActivityLogger activityLogger)
    : Endpoint<UpdateProfileDto, MeDto>
{
    private const int MaxSecondaryMembers = 4;

    public override void Configure()
    {
        Put("/account/me");
    }

    public override async Task HandleAsync(UpdateProfileDto req, CancellationToken ct)
    {
        var current = await userManager.GetUserAsync(User);
        if (current is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var secondaryMembers = (req.SecondaryMembers ?? [])
            .Select(NullIfBlank)
            .OfType<string>()
            .ToList();
        if (secondaryMembers.Count > MaxSecondaryMembers)
        {
            await Send.ResultAsync(Results.BadRequest(new
            {
                error = $"You can list at most {MaxSecondaryMembers} additional members.",
            }));
            return;
        }

        current.FirstName = NullIfBlank(req.FirstName);
        current.LastName = NullIfBlank(req.LastName);
        current.PhoneNumber = NullIfBlank(req.PhoneNumber);
        current.Address = NullIfBlank(req.Address);
        current.IsSubscribedToMailingList = req.IsSubscribedToMailingList;

        var result = await userManager.UpdateAsync(current);
        if (!result.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(new
            {
                error = string.Join("; ", result.Errors.Select(e => e.Description)),
            }));
            return;
        }

        var existing = db.SecondaryMembers.Where(s => s.UserId == current.Id);
        db.SecondaryMembers.RemoveRange(existing);
        foreach (var name in secondaryMembers)
        {
            db.SecondaryMembers.Add(new SecondaryMember { UserId = current.Id, FullName = name });
        }
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "member.profile_updated",
            "updated their profile",
            targetType: "Member",
            targetId: current.Id,
            ct: ct);

        var isAdmin = await userManager.IsInRoleAsync(current, AppRoles.Admin);
        var savedSecondary = await db.SecondaryMembers
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
            savedSecondary,
            current.MembershipPaidThroughUtc,
            isAdmin,
            current.IsSubscribedToMailingList), ct);
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
