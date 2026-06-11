using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Public;

public sealed class GetBlogPostEndpoint(AppDbContext db)
    : Endpoint<GetBlogPostEndpoint.Request, BlogPostDto>
{
    private const int MorePostsCount = 3;

    public sealed class Request
    {
        public string Slug { get; set; } = "";
    }

    public override void Configure()
    {
        Get("/posts/{slug}");
        Group<PublicBlogGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.BlogPosts
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == req.Slug && p.Status == BlogPostStatus.Published, ct);

        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var morePostsQuery = db.BlogPosts
            .AsNoTracking()
            .Where(p => p.Status == BlogPostStatus.Published && p.Id != post.Id);

        var moreRows = await BlogPostSummaryQuery.PublishedSummary(morePostsQuery)
            .Take(MorePostsCount)
            .ToListAsync(ct);

        var morePosts = moreRows.Select(BlogPostSummaryQuery.ToSummaryDto).ToList();

        var dto = new BlogPostDto(
            post.Id, post.Slug, post.Title,
            post.Excerpt ?? BlogExcerpt.FromHtml(post.BodyHtml),
            post.BodyHtml,
            post.AuthorName, post.PublishedAt, post.Category?.Name, post.FeaturedImageId,
            morePosts);

        await Send.OkAsync(dto, ct);
    }
}
