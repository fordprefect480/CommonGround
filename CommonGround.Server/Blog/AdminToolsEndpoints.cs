using CommonGround.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog;

public static class AdminToolsEndpoints
{
    public static RouteGroupBuilder MapAdminTools(this RouteGroupBuilder admin)
    {
        var tools = admin.MapGroup("/tools");

        tools.MapPost("/orphan-images/cleanup", async (AppDbContext db) =>
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var posts = await db.BlogPosts.AsNoTracking()
                .Select(p => new { p.FeaturedImageId, p.BodyHtml })
                .ToListAsync();

            var referencedIds = new HashSet<int>();
            foreach (var p in posts)
            {
                if (p.FeaturedImageId is int fid) referencedIds.Add(fid);
                if (string.IsNullOrEmpty(p.BodyHtml)) continue;
                var idx = 0;
                while ((idx = p.BodyHtml.IndexOf("/api/blog/images/", idx, StringComparison.Ordinal)) >= 0)
                {
                    idx += "/api/blog/images/".Length;
                    var end = idx;
                    while (end < p.BodyHtml.Length && char.IsDigit(p.BodyHtml[end])) end++;
                    if (end > idx && int.TryParse(p.BodyHtml.AsSpan(idx, end - idx), out var imgId))
                        referencedIds.Add(imgId);
                    idx = end;
                }
            }

            var orphans = await db.BlogImages
                .Where(i => i.CreatedAt < cutoff && !referencedIds.Contains(i.Id))
                .ToListAsync();

            db.BlogImages.RemoveRange(orphans);
            await db.SaveChangesAsync();

            return Results.Ok(new { deleted = orphans.Count });
        });

        tools.MapPost("/import-blog", async (
            BlogImport.BlogImporter importer,
            int? limit,
            CancellationToken ct) =>
        {
            var result = await importer.ImportAsync(limit, ct);
            return Results.Ok(result);
        });

        return admin;
    }
}
