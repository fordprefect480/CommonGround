using System.Security;
using System.Text;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Misc;

/// <summary>
/// Serves <c>/sitemap.xml</c> at the site root. The fixed marketing pages are
/// listed statically; published blog posts are appended dynamically with a
/// <c>lastmod</c> so crawlers re-fetch changed posts and discover new ones
/// without the file going stale.
/// </summary>
public static class SitemapEndpoint
{
    private const string SiteUrl = "https://seafordwetlandscommunitygarden.com";

    private static readonly (string Path, string ChangeFreq, string Priority)[] StaticPages =
    [
        ("/", "weekly", "1.0"),
        ("/membership", "monthly", "0.9"),
        ("/lease-a-plot", "monthly", "0.9"),
        ("/events", "weekly", "0.8"),
        ("/blog", "weekly", "0.8"),
        ("/resources", "monthly", "0.6"),
        ("/donate", "monthly", "0.6"),
    ];

    public static void MapSitemapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/sitemap.xml", HandleAsync).AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(AppDbContext db, CancellationToken ct)
    {
        var posts = await db.BlogPosts
            .AsNoTracking()
            .Where(p => p.Status == BlogPostStatus.Published)
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new { p.Slug, p.PublishedAt, p.UpdatedAt })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var (path, changeFreq, priority) in StaticPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{SiteUrl}{path}</loc>");
            sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }

        foreach (var post in posts)
        {
            var lastMod = post.UpdatedAt != default ? post.UpdatedAt : post.PublishedAt;
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{SiteUrl}/blog/{SecurityElement.Escape(post.Slug)}</loc>");
            if (lastMod is { } dt)
            {
                sb.AppendLine($"    <lastmod>{dt:yyyy-MM-dd}</lastmod>");
            }
            sb.AppendLine("    <changefreq>monthly</changefreq>");
            sb.AppendLine("    <priority>0.7</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Results.Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
