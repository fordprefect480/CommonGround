using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class UpdateMemberEndpoint(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IActivityLogger activityLogger)
    : Endpoint<UpdateMemberEndpoint.Request, MemberDto>
{
    private const int MaxSecondaryMembers = 4;

    public sealed class Request
    {
        public string Id { get; set; } = "";
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string[]? SecondaryMembers { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSubscribedToMailingList { get; set; }
    }

    public override void Configure()
    {
        Put("/members/{id}");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var secondaryMembers = (req.SecondaryMembers ?? [])
            .Select(MemberHelpers.NullIfBlank)
            .OfType<string>()
            .ToList();
        if (secondaryMembers.Count > MaxSecondaryMembers)
        {
            await SendBadRequest([$"You can list at most {MaxSecondaryMembers} additional members."]);
            return;
        }

        user.FirstName = MemberHelpers.NullIfBlank(req.FirstName);
        user.LastName = MemberHelpers.NullIfBlank(req.LastName);
        user.PhoneNumber = MemberHelpers.NullIfBlank(req.PhoneNumber);
        user.Address = MemberHelpers.NullIfBlank(req.Address);
        user.IsSubscribedToMailingList = req.IsSubscribedToMailingList;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await SendBadRequest(updateResult.Errors.Select(e => e.Description));
            return;
        }

        var existing = db.SecondaryMembers.Where(s => s.UserId == user.Id);
        db.SecondaryMembers.RemoveRange(existing);
        foreach (var name in secondaryMembers)
        {
            db.SecondaryMembers.Add(new SecondaryMember { UserId = user.Id, FullName = name });
        }
        await db.SaveChangesAsync(ct);

        var isAdmin = await userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (req.IsAdmin && !isAdmin)
        {
            var addResult = await userManager.AddToRoleAsync(user, AppRoles.Admin);
            if (!addResult.Succeeded)
            {
                await SendBadRequest(addResult.Errors.Select(e => e.Description));
                return;
            }
        }
        else if (!req.IsAdmin && isAdmin)
        {
            var removeResult = await userManager.RemoveFromRoleAsync(user, AppRoles.Admin);
            if (!removeResult.Succeeded)
            {
                await SendBadRequest(removeResult.Errors.Select(e => e.Description));
                return;
            }
        }

        await activityLogger.LogAsync(
            "member.updated",
            $"updated the member {user.Email}",
            targetType: "Member",
            targetId: user.Id,
            ct: ct);

        var roles = await userManager.GetRolesAsync(user);
        var savedSecondary = await db.SecondaryMembers
            .Where(s => s.UserId == user.Id)
            .OrderBy(s => s.Id)
            .Select(s => s.FullName)
            .ToArrayAsync(ct);

        await Send.OkAsync(new MemberDto(
            user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
            user.PhoneNumber, user.Address, user.JoinedAt, user.MembershipPaidThroughUtc,
            user.EmailConfirmed, user.IsSubscribedToMailingList, savedSecondary, roles.ToArray()), ct);
    }

    private Task SendBadRequest(IEnumerable<string> errors) =>
        Send.ResultAsync(Results.BadRequest(new { error = string.Join("; ", errors) }));
}
