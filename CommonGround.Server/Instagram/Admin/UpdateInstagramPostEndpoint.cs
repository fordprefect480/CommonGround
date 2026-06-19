using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Admin;

public sealed class UpdateInstagramPostEndpoint(AppDbContext db, IActivityLogger activityLogger)
    : Endpoint<UpdateInstagramPostEndpoint.Request, InstagramPostAdminDto>
{
    public sealed class Request
    {
        public int Id { get; set; }
        public string EmbedHtml { get; set; } = "";
    }

    public override void Configure()
    {
        Put("/posts/{id:int}");
        Group<AdminInstagramGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.InstagramPosts.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!InstagramEmbedSanitizer.TryExtract(req.EmbedHtml, out var sanitized, out var error))
        {
            await Send.ResultAsync(Results.BadRequest(new { error }));
            return;
        }

        post.EmbedHtml = sanitized;
        post.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "instagram.post_updated",
            $"updated an Instagram tile (#{post.Id})",
            targetType: "InstagramPost",
            targetId: post.Id.ToString(),
            ct: ct);

        await Send.OkAsync(InstagramMapping.ToAdminDto(post), ct);
    }
}
