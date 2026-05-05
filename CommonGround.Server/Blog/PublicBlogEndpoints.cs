using CommonGround.Server.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace CommonGround.Server.Blog;

public static class PublicBlogEndpoints
{
    private const int BodyHeadChars = 1000;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const int MorePostsCount = 3;

    public static IEndpointRouteBuilder MapPublicBlog(this IEndpointRouteBuilder app)
    {
        var blog = app.MapGroup("/api/blog");

        blog.MapGet("/posts", async (AppDbContext db, int? page, int? pageSize, CancellationToken ct) =>
        {
            var resolvedPage = page is { } p && p > 0 ? p : 1;
            var resolvedSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

            var publishedQuery = db.BlogPosts
                .AsNoTracking()
                .Where(post => post.Status == BlogPostStatus.Published);

            var total = await publishedQuery.CountAsync(ct);

            var rows = await PublishedSummaryQuery(publishedQuery)
                .Skip((resolvedPage - 1) * resolvedSize)
                .Take(resolvedSize)
                .ToListAsync(ct);

            var posts = rows.Select(ToSummaryDto).ToList();
            return Results.Ok(new BlogPostListDto(posts, resolvedPage, resolvedSize, total));
        });

        blog.MapGet("/posts/{slug}", async (string slug, AppDbContext db, CancellationToken ct) =>
        {
            var post = await db.BlogPosts
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == BlogPostStatus.Published, ct);

            if (post is null) return Results.NotFound();

            var morePostsQuery = db.BlogPosts
                .AsNoTracking()
                .Where(p => p.Status == BlogPostStatus.Published && p.Id != post.Id);

            var moreRows = await PublishedSummaryQuery(morePostsQuery)
                .Take(MorePostsCount)
                .ToListAsync(ct);

            var morePosts = moreRows.Select(ToSummaryDto).ToList();

            var dto = new BlogPostDto(
                post.Id, post.Slug, post.Title,
                post.Excerpt ?? BlogExcerpt.FromHtml(post.BodyHtml),
                post.BodyHtml,
                post.AuthorName, post.PublishedAt, post.Category?.Name, post.FeaturedImageId,
                morePosts);

            return Results.Ok(dto);
        });

        blog.MapGet("/categories", async (AppDbContext db, CancellationToken ct) =>
        {
            var categories = await db.BlogCategories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => new BlogCategoryDto(c.Id, c.Name, c.Slug))
                .ToListAsync(ct);
            return Results.Ok(categories);
        });

        blog.MapGet("/images/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var image = await db.BlogImages
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => new { i.ContentType, i.Bytes })
                .FirstOrDefaultAsync(ct);

            if (image is null) return Results.NotFound();

            return Results.File(
                image.Bytes,
                contentType: image.ContentType,
                fileDownloadName: null,
                lastModified: null,
                entityTag: new EntityTagHeaderValue($"\"{id}\""));
        }).WithMetadata(new ResponseCacheAttribute
        {
            Duration = 31536000,
            Location = ResponseCacheLocation.Any,
            NoStore = false,
        });

        return app;
    }

    private record SummaryRow(
        int Id, string Slug, string Title, string? Excerpt, string AuthorName,
        DateTime? PublishedAt, string? CategoryName, int? FeaturedImageId, string BodyHead);

    private static IQueryable<SummaryRow> PublishedSummaryQuery(IQueryable<BlogPost> source) =>
        source
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new SummaryRow(
                p.Id, p.Slug, p.Title, p.Excerpt, p.AuthorName,
                p.PublishedAt,
                p.Category != null ? p.Category.Name : null,
                p.FeaturedImageId,
                p.BodyHtml.Length > BodyHeadChars ? p.BodyHtml.Substring(0, BodyHeadChars) : p.BodyHtml));

    private static BlogPostSummaryDto ToSummaryDto(SummaryRow r) => new(
        r.Id, r.Slug, r.Title,
        r.Excerpt ?? BlogExcerpt.FromHtml(r.BodyHead),
        r.AuthorName, r.PublishedAt, r.CategoryName, r.FeaturedImageId);
}
