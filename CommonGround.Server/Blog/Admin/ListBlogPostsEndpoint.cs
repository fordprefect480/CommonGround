using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Admin;

public sealed class ListBlogPostsEndpoint(AppDbContext db)
    : EndpointWithoutRequest<List<BlogPostAdminListItemDto>>
{
    public override void Configure()
    {
        Get("/posts");
        Group<AdminBlogGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var posts = await db.BlogPosts
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new BlogPostAdminListItemDto(
                p.Id, p.Slug, p.Title, p.AuthorName,
                p.CategoryId, p.FeaturedImageId, (int)p.Status,
                p.PublishedAt, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);

        await Send.OkAsync(posts, ct);
    }
}
