using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Public;

public sealed class ListInstagramPostsEndpoint(AppDbContext db)
    : Endpoint<ListInstagramPostsEndpoint.Request, List<InstagramPostDto>>
{
    private const int DefaultTake = 6;
    private const int MaxTake = 24;

    public sealed class Request
    {
        public int? Take { get; set; }
    }

    public override void Configure()
    {
        Get("/posts");
        Group<PublicInstagramGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var take = Math.Clamp(req.Take ?? DefaultTake, 1, MaxTake);

        var posts = await db.InstagramPosts
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.CreatedAt)
            .Take(take)
            .Select(p => new InstagramPostDto(p.Id, p.EmbedHtml, p.DisplayOrder))
            .ToListAsync(ct);

        await Send.OkAsync(posts, ct);
    }
}
