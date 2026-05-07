using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Admin;

public sealed class DeleteBlogPostEndpoint(AppDbContext db)
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

        db.BlogPosts.Remove(post);
        await db.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
