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

    private const int BodyHeadChars = 500;
    private const int ExcerptChars = 90;

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rows = await db.BlogPosts
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id, p.Slug, p.Title, p.AuthorName,
                p.CategoryId, p.FeaturedImageId, p.Status,
                p.PublishedAt, p.CreatedAt, p.UpdatedAt,
                BodyHead = p.BodyHtml.Length > BodyHeadChars ? p.BodyHtml.Substring(0, BodyHeadChars) : p.BodyHtml,
            })
            .ToListAsync(ct);

        var posts = rows.Select(p => new BlogPostAdminListItemDto(
            p.Id, p.Slug, p.Title, p.AuthorName,
            p.CategoryId, p.FeaturedImageId, (int)p.Status,
            p.PublishedAt, p.CreatedAt, p.UpdatedAt,
            BlogExcerpt.FromHtml(p.BodyHead, ExcerptChars))).ToList();

        await Send.OkAsync(posts, ct);
    }
}
