using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class GetMemberEndpoint(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : Endpoint<GetMemberEndpoint.Request, MemberDto>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";
    }

    public override void Configure()
    {
        Get("/members/{id}");
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

        var roles = await userManager.GetRolesAsync(user);

        await Send.OkAsync(new MemberDto(
            user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
            user.PhoneNumber, user.JoinedAt, user.EmailConfirmed, user.IsSubscribedToMailingList,
            roles.ToArray()), ct);
    }
}
