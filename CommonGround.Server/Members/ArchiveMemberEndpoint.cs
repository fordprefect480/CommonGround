using System.Security.Claims;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ArchiveMemberEndpoint(AppDbContext db, MemberArchiveService archiver) : Endpoint<ArchiveMemberEndpoint.Request>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";
    }

    public override void Configure()
    {
        Delete("/members/{id}");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == req.Id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        ApplicationUser[] users = [user];
        var error = await archiver.ValidateAsync(users, User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (error is not null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error }));
            return;
        }

        await archiver.ArchiveAsync(users, ct);
        await Send.NoContentAsync(ct);
    }
}
