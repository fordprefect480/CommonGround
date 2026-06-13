using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Admin;

public sealed class DeleteInstagramPostEndpoint(AppDbContext db, IActivityLogger activityLogger)
    : Endpoint<DeleteInstagramPostEndpoint.Request>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Delete("/posts/{id:int}");
        Group<AdminInstagramGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.InstagramPosts.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var deletedId = post.Id;
        db.InstagramPosts.Remove(post);
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "instagram.post_deleted",
            $"removed an Instagram tile (#{deletedId})",
            targetType: "InstagramPost",
            targetId: deletedId.ToString(),
            ct: ct);

        await Send.NoContentAsync(ct);
    }
}
