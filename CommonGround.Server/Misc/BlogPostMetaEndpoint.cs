using CommonGround.Server.Blog;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Misc;

/// <summary>
/// Serves <c>/blog/{slug}</c> with the post's Open Graph / Twitter tags injected
/// into the SPA shell, so link unfurlers that don't run JavaScript show the post's
/// featured image and title. Humans receive the same HTML and the SPA boots normally.
/// </summary>
public static class BlogPostMetaEndpoint
{
    private const string SiteUrl = "https://seafordwetlandscommunitygarden.com";
    private const string GardenName = "Seaford Wetlands Community Garden";

    public static void MapBlogPostMetaEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/blog/{slug}", HandleAsync).AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(string slug, AppDbContext db, IndexHtmlProvider indexHtml, CancellationToken ct)
    {
        var html = indexHtml.GetHtml();
        if (html is null)
        {
            // No built SPA shell (local dev only, where Vite serves the SPA): return 404.
            return Results.NotFound();
        }

        var post = await db.BlogPosts
            .AsNoTracking()
            .Where(p => p.Slug == slug && p.Status == BlogPostStatus.Published)
            .Select(p => new { p.Slug, p.Title, p.Excerpt, p.BodyHtml, p.FeaturedImageId })
            .FirstOrDefaultAsync(ct);

        if (post is null)
        {
            return Results.Content(html, "text/html");
        }

        var meta = new BlogPostMetaTags.PostMeta(
            post.Title,
            post.Excerpt ?? BlogExcerpt.FromHtml(post.BodyHtml),
            post.Slug,
            post.FeaturedImageId);

        return Results.Content(BlogPostMetaTags.Inject(html, meta, SiteUrl, GardenName), "text/html");
    }
}
