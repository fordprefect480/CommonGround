using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Admin;

public sealed class DeleteBlogPostEndpoint(AppDbContext db, IActivityLogger activityLogger)
    : Endpoint<DeleteBlogPostEndpoint.Request>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Delete("/posts/{id:int}");
        Group<AdminBlogGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var deletedTitle = post.Title;
        var deletedId = post.Id;

        db.BlogPosts.Remove(post);
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "blog.post_deleted",
            $"deleted the blog post '{deletedTitle}'",
            targetType: "BlogPost",
            targetId: deletedId.ToString(),
            ct: ct);

        await Send.NoContentAsync(ct);
    }
}
