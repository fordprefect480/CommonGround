using System.Security.Claims;
using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CommonGround.Server.Events.Admin;

public sealed class CreateCommunityEventEndpoint(
    AppDbContext db,
    IActivityLogger activityLogger,
    IMemoryCache cache)
    : Endpoint<CommunityEventWriteDto, CommunityEventAdminDto>
{
    public override void Configure()
    {
        Post("");
        Group<AdminEventsGroup>();
    }

    public override async Task HandleAsync(CommunityEventWriteDto req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Title is required." }));
            return;
        }
        if (string.IsNullOrWhiteSpace(req.Body))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Body is required." }));
            return;
        }

        var displayOrder = req.DisplayOrder ?? ((await db.CommunityEvents.MaxAsync(e => (int?)e.DisplayOrder, ct) ?? -1) + 1);

        var imageId = await ResolveImageIdAsync(req.FeaturedImageId, ct);

        var now = DateTime.UtcNow;
        var ev = new CommunityEvent
        {
            Title = req.Title.Trim(),
            StartUtc = DateTime.SpecifyKind(req.StartUtc, DateTimeKind.Utc),
            EndUtc = req.EndUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null,
            Body = req.Body.Trim(),
            Url = string.IsNullOrWhiteSpace(req.Url) ? null : req.Url.Trim(),
            Tone = EventsMapping.NormalizeTone(req.Tone),
            DisplayOrder = displayOrder,
            FeaturedImageId = imageId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        };

        db.CommunityEvents.Add(ev);
        await db.SaveChangesAsync(ct);

        EventsCache.InvalidateUpcoming(cache);

        await activityLogger.LogAsync(
            "event.created",
            $"Added event \"{ev.Title}\"",
            targetType: "CommunityEvent",
            targetId: ev.Id.ToString(),
            ct: ct);

        await Send.ResultAsync(Results.Created($"/api/admin/events/{ev.Id}", EventsMapping.ToAdminDto(ev)));
    }

    private async Task<int?> ResolveImageIdAsync(int? requested, CancellationToken ct)
    {
        if (requested is not { } id) return null;
        var exists = await db.BlogImages.AsNoTracking().AnyAsync(i => i.Id == id, ct);
        return exists ? id : null;
    }
}
