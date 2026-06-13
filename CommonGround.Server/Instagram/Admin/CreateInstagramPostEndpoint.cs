using System.Security.Claims;
using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Instagram.Admin;

public sealed class CreateInstagramPostEndpoint(AppDbContext db, IActivityLogger activityLogger)
    : Endpoint<InstagramPostWriteDto, InstagramPostAdminDto>
{
    public override void Configure()
    {
        Post("/posts");
        Group<AdminInstagramGroup>();
    }

    public override async Task HandleAsync(InstagramPostWriteDto req, CancellationToken ct)
    {
        if (!InstagramEmbedSanitizer.TryExtract(req.EmbedHtml, out var sanitized, out var error))
        {
            await Send.ResultAsync(Results.BadRequest(new { error }));
            return;
        }

        var displayOrder = req.DisplayOrder
            ?? ((await db.InstagramPosts.MaxAsync(p => (int?)p.DisplayOrder, ct) ?? -1) + 1);

        var now = DateTime.UtcNow;
        var post = new InstagramPost
        {
            EmbedHtml = sanitized,
            DisplayOrder = displayOrder,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        };

        db.InstagramPosts.Add(post);
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "instagram.post_created",
            $"added a new Instagram tile (#{post.Id})",
            targetType: "InstagramPost",
            targetId: post.Id.ToString(),
            ct: ct);

        await Send.ResultAsync(Results.Created(
            $"/api/admin/instagram/posts/{post.Id}",
            InstagramMapping.ToAdminDto(post)));
    }
}
