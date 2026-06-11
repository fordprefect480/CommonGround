using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Admin;

public sealed class ListInstagramPostsEndpoint(AppDbContext db)
    : EndpointWithoutRequest<List<InstagramPostAdminDto>>
{
    public override void Configure()
    {
        Get("/posts");
        Group<AdminInstagramGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var posts = await db.InstagramPosts
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new InstagramPostAdminDto(
                p.Id, p.EmbedHtml, p.DisplayOrder, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(ct);

        await Send.OkAsync(posts, ct);
    }
}
