using CommonGround.Server.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog;

public static class PublicBlogEndpoints
{
    public static IEndpointRouteBuilder MapPublicBlog(this IEndpointRouteBuilder app)
    {
        var blog = app.MapGroup("/api/blog");

        blog.MapGet("/posts", async (AppDbContext db) =>
        {
            var rows = await db.BlogPosts
                .AsNoTracking()
                .Where(p => p.Status == BlogPostStatus.Published)
                .OrderByDescending(p => p.PublishedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Slug,
                    p.Title,
                    p.Excerpt,
                    p.AuthorName,
                    p.PublishedAt,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    p.FeaturedImageId,
                    BodyHead = p.BodyHtml.Length > 1000 ? p.BodyHtml.Substring(0, 1000) : p.BodyHtml,
                })
                .ToListAsync();

            var posts = rows
                .Select(r => new BlogPostSummaryDto(
                    r.Id, r.Slug, r.Title,
                    r.Excerpt ?? BlogExcerpt.FromHtml(r.BodyHead, 200),
                    r.AuthorName, r.PublishedAt, r.CategoryName, r.FeaturedImageId))
                .ToList();

            return Results.Ok(posts);
        });

        blog.MapGet("/posts/{slug}", async (string slug, AppDbContext db) =>
        {
            var post = await db.BlogPosts
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == BlogPostStatus.Published);

            if (post is null) return Results.NotFound();

            var relatedRows = await db.BlogPosts
                .AsNoTracking()
                .Where(p => p.Status == BlogPostStatus.Published && p.Id != post.Id)
                .OrderByDescending(p => p.PublishedAt)
                .Take(3)
                .Select(p => new
                {
                    p.Id, p.Slug, p.Title, p.Excerpt, p.AuthorName, p.PublishedAt,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    p.FeaturedImageId,
                    BodyHead = p.BodyHtml.Length > 1000 ? p.BodyHtml.Substring(0, 1000) : p.BodyHtml,
                })
                .ToListAsync();

            var related = relatedRows
                .Select(r => new BlogPostSummaryDto(
                    r.Id, r.Slug, r.Title,
                    r.Excerpt ?? BlogExcerpt.FromHtml(r.BodyHead, 200),
                    r.AuthorName, r.PublishedAt, r.CategoryName, r.FeaturedImageId))
                .ToList();

            var dto = new BlogPostDto(
                post.Id, post.Slug, post.Title,
                post.Excerpt ?? BlogExcerpt.FromHtml(post.BodyHtml, 200),
                post.BodyHtml,
                post.AuthorName, post.PublishedAt, post.Category?.Name, post.FeaturedImageId,
                related);

            return Results.Ok(dto);
        });

        blog.MapGet("/categories", async (AppDbContext db) =>
        {
            var categories = await db.BlogCategories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .Select(c => new BlogCategoryDto(c.Id, c.Name, c.Slug))
                .ToListAsync();
            return Results.Ok(categories);
        });

        blog.MapGet("/images/{id:int}", async (int id, AppDbContext db) =>
        {
            var image = await db.BlogImages
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => new { i.ContentType, i.Bytes })
                .FirstOrDefaultAsync();

            if (image is null) return Results.NotFound();

            return Results.File(
                image.Bytes,
                contentType: image.ContentType,
                fileDownloadName: null,
                lastModified: null,
                entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{id}\""));
        }).WithMetadata(new Microsoft.AspNetCore.Mvc.ResponseCacheAttribute
        {
            Duration = 31536000,
            Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any,
            NoStore = false,
        });

        return app;
    }
}
