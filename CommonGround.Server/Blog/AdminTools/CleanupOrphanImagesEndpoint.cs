using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.AdminTools;

public sealed class CleanupOrphanImagesEndpoint(AppDbContext db, IActivityLogger activityLogger)
    : EndpointWithoutRequest<CleanupOrphanImagesEndpoint.CleanupResult>
{
    private const string ImagePathPrefix = "/api/blog/images/";

    public sealed record CleanupResult(int Deleted);

    public override void Configure()
    {
        Post("/orphan-images/cleanup");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
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

        await activityLogger.LogAsync(
            "tool.orphan_cleanup_run",
            $"Cleaned up {orphans.Count} orphan image(s)",
            details: new { Deleted = orphans.Count },
            ct: ct);

        await Send.OkAsync(new CleanupResult(orphans.Count), ct);
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
