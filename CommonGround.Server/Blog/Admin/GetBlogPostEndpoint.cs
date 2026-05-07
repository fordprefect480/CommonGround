using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Admin;

public sealed class GetBlogPostEndpoint(AppDbContext db)
    : Endpoint<GetBlogPostEndpoint.Request, BlogPostAdminDto>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Get("/posts/{id:int}");
        Group<AdminBlogGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(BlogPostMapping.ToAdminDto(post), ct);
    }
}
