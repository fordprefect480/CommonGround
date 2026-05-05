using CommonGround.Server.Blog.BlogImport;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog;

public static class AdminToolsEndpoints
{
    private const string ImagePathPrefix = "/api/blog/images/";

    public static RouteGroupBuilder MapAdminTools(this RouteGroupBuilder admin)
    {
        var tools = admin.MapGroup("/tools");

        tools.MapPost("/orphan-images/cleanup", async (AppDbContext db, CancellationToken ct) =>
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            var referencedIds = new HashSet<int>();

            await foreach (var row in db.BlogPosts
                .AsNoTracking()
                .Select(p => new { p.FeaturedImageId, p.BodyHtml })
                .AsAsyncEnumerable()
                .WithCancellation(ct))
            {
                if (row.FeaturedImageId is int fid) referencedIds.Add(fid);
                CollectReferencedImageIds(row.BodyHtml, referencedIds);
            }

            var orphans = await db.BlogImages
                .Where(i => i.CreatedAt < cutoff && !referencedIds.Contains(i.Id))
                .ToListAsync(ct);

            db.BlogImages.RemoveRange(orphans);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { deleted = orphans.Count });
        });

        tools.MapPost("/import-blog", async (
            BlogImporter importer,
            int? limit,
            CancellationToken ct) =>
        {
            var result = await importer.ImportAsync(limit, ct);
            return Results.Ok(result);
        });

        return admin;
    }

    private static void CollectReferencedImageIds(string? bodyHtml, HashSet<int> ids)
    {
        if (string.IsNullOrEmpty(bodyHtml)) return;

        var idx = 0;
        while ((idx = bodyHtml.IndexOf(ImagePathPrefix, idx, StringComparison.Ordinal)) >= 0)
        {
            idx += ImagePathPrefix.Length;
            var end = idx;
            while (end < bodyHtml.Length && char.IsDigit(bodyHtml[end])) end++;
            if (end > idx && int.TryParse(bodyHtml.AsSpan(idx, end - idx), out var imgId))
                ids.Add(imgId);
            idx = end;
        }
    }
}
