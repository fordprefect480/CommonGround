using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Admin;

public sealed class GetInstagramPostEndpoint(AppDbContext db)
    : Endpoint<GetInstagramPostEndpoint.Request, InstagramPostAdminDto>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Get("/posts/{id:int}");
        Group<AdminInstagramGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.InstagramPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(InstagramMapping.ToAdminDto(post), ct);
    }
}
