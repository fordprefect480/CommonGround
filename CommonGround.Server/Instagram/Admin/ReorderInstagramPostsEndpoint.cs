using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Admin;

public sealed class ReorderInstagramPostsEndpoint(AppDbContext db, IActivityLogger activityLogger)
    : Endpoint<ReorderInstagramPostsEndpoint.Request>
{
    public sealed class Request
    {
        public List<int> Ids { get; set; } = [];
    }

    public override void Configure()
    {
        Put("/posts/reorder");
        Group<AdminInstagramGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var orderById = req.Ids
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        var posts = await db.InstagramPosts.ToListAsync(ct);
        foreach (var post in posts)
        {
            if (orderById.TryGetValue(post.Id, out var order))
                post.DisplayOrder = order;
        }

        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "instagram.posts_reordered",
            "reordered the Instagram tiles",
            targetType: "InstagramPost",
            ct: ct);

        await Send.NoContentAsync(ct);
    }
}
