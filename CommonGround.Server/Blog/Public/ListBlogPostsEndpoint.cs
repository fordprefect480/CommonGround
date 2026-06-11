using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Public;

public sealed class ListBlogPostsEndpoint(AppDbContext db)
    : Endpoint<ListBlogPostsEndpoint.Request, BlogPostListDto>
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public sealed class Request
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
    }

    public override void Configure()
    {
        Get("/posts");
        Group<PublicBlogGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var resolvedPage = req.Page is { } p && p > 0 ? p : 1;
        var resolvedSize = Math.Clamp(req.PageSize ?? DefaultPageSize, 1, MaxPageSize);

        var publishedQuery = db.BlogPosts
            .AsNoTracking()
            .Where(post => post.Status == BlogPostStatus.Published);

        var total = await publishedQuery.CountAsync(ct);

        var rows = await BlogPostSummaryQuery.PublishedSummary(publishedQuery)
            .Skip((resolvedPage - 1) * resolvedSize)
            .Take(resolvedSize)
            .ToListAsync(ct);

        var posts = rows.Select(BlogPostSummaryQuery.ToSummaryDto).ToList();
        await Send.OkAsync(new BlogPostListDto(posts, resolvedPage, resolvedSize, total), ct);
    }
}
