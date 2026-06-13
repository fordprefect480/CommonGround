using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CommonGround.Server.Events.Admin;

public sealed class UpdateCommunityEventEndpoint(
    AppDbContext db,
    IActivityLogger activityLogger,
    IMemoryCache cache)
    : Endpoint<UpdateCommunityEventEndpoint.Request, CommunityEventAdminDto>
{
    public sealed class Request
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }
        public string Body { get; set; } = "";
        public string? Url { get; set; }
        public int? FeaturedImageId { get; set; }
        public string? Tone { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public override void Configure()
    {
        Put("/{id:int}");
        Group<AdminEventsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var ev = await db.CommunityEvents.SingleOrDefaultAsync(e => e.Id == req.Id, ct);
        if (ev is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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

        var imageId = await ResolveImageIdAsync(req.FeaturedImageId, ct);

        ev.Title = req.Title.Trim();
        ev.StartUtc = DateTime.SpecifyKind(req.StartUtc, DateTimeKind.Utc);
        ev.EndUtc = req.EndUtc is { } end ? DateTime.SpecifyKind(end, DateTimeKind.Utc) : null;
        ev.Body = req.Body.Trim();
        ev.Url = string.IsNullOrWhiteSpace(req.Url) ? null : req.Url.Trim();
        ev.Tone = EventsMapping.NormalizeTone(req.Tone);
        ev.FeaturedImageId = imageId;
        if (req.DisplayOrder is { } order) ev.DisplayOrder = order;
        ev.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        EventsCache.InvalidateUpcoming(cache);

        await activityLogger.LogAsync(
            "event.updated",
            $"updated the event \"{ev.Title}\"",
            targetType: "CommunityEvent",
            targetId: ev.Id.ToString(),
            ct: ct);

        await Send.OkAsync(EventsMapping.ToAdminDto(ev), ct);
    }

    private async Task<int?> ResolveImageIdAsync(int? requested, CancellationToken ct)
    {
        if (requested is not { } id) return null;
        var exists = await db.BlogImages.AsNoTracking().AnyAsync(i => i.Id == id, ct);
        return exists ? id : null;
    }
}
