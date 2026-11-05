using System.Security.Claims;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

/// <summary>
/// Deletes (archives) several members at once. All-or-nothing: if any guard fails,
/// no one is archived. Ids that no longer exist are ignored.
/// </summary>
public sealed class BulkArchiveMembersEndpoint(AppDbContext db, MemberArchiveService archiver) : Endpoint<BulkArchiveMembersEndpoint.Request>
{
    public sealed class Request
    {
        public List<string> Ids { get; set; } = [];
    }

    public override void Configure()
    {
        Post("/members/bulk-delete");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var ids = req.Ids.Distinct().ToList();
        if (ids.Count == 0)
        {
            await SendBadRequest("Select at least one member to delete.");
            return;
        }

        var users = await db.Users.Where(u => ids.Contains(u.Id)).ToListAsync(ct);
        if (users.Count == 0)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var error = await archiver.ValidateAsync(users, User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (error is not null)
        {
            await SendBadRequest(error);
            return;
        }

        await archiver.ArchiveAsync(users, ct);
        await Send.NoContentAsync(ct);
    }

    private Task SendBadRequest(string error) =>
        Send.ResultAsync(Results.BadRequest(new { error }));
}
