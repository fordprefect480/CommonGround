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
    : Endpoint<CommunityEventWriteDto, CommunityEventCreatedDto>
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

        var startUtc = DateTime.SpecifyKind(req.StartUtc, DateTimeKind.Utc);
        var frequency = RecurrenceExpander.ParseFrequency(req.RepeatFrequency);

        IReadOnlyList<DateTime> starts;
        if (frequency == RepeatFrequency.None)
        {
            starts = [startUtc];
        }
        else
        {
            if (req.RepeatUntil is not { } until)
            {
                await Send.ResultAsync(Results.BadRequest(new { error = "A repeat-until date is required for a recurring event." }));
                return;
            }
            if (until < RecurrenceExpander.GardenDate(startUtc))
            {
                await Send.ResultAsync(Results.BadRequest(new { error = "The repeat-until date must be on or after the start date." }));
                return;
            }
            starts = RecurrenceExpander.Expand(startUtc, frequency, until);
        }

        var duration = req.EndUtc is { } endRaw
            ? DateTime.SpecifyKind(endRaw, DateTimeKind.Utc) - startUtc
            : (TimeSpan?)null;

        var baseOrder = req.DisplayOrder ?? ((await db.CommunityEvents.MaxAsync(e => (int?)e.DisplayOrder, ct) ?? -1) + 1);
        var imageId = await ResolveImageIdAsync(req.FeaturedImageId, ct);
        var tone = EventsMapping.NormalizeTone(req.Tone);
        var url = string.IsNullOrWhiteSpace(req.Url) ? null : req.Url.Trim();
        var location = string.IsNullOrWhiteSpace(req.Location) ? null : req.Location.Trim();
        var title = req.Title.Trim();
        var body = req.Body.Trim();
        var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var now = DateTime.UtcNow;

        var created = new List<CommunityEvent>(starts.Count);
        for (var i = 0; i < starts.Count; i++)
        {
            var occurrenceStart = starts[i];
            created.Add(new CommunityEvent
            {
                Title = title,
                StartUtc = occurrenceStart,
                EndUtc = duration is { } d ? occurrenceStart + d : null,
                Body = body,
                Url = url,
                Location = location,
                Tone = tone,
                DisplayOrder = baseOrder + i,
                FeaturedImageId = imageId,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = createdBy,
            });
        }

        db.CommunityEvents.AddRange(created);
        await db.SaveChangesAsync(ct);

        EventsCache.InvalidateUpcoming(cache);

        var first = created[0];
        var summary = created.Count == 1
            ? $"added a new event \"{first.Title}\""
            : $"added a recurring event \"{first.Title}\" ({created.Count} occurrences)";
        await activityLogger.LogAsync("event.created", summary, targetType: "CommunityEvent", targetId: first.Id.ToString(), ct: ct);

        await Send.ResultAsync(Results.Created(
            $"/api/admin/events/{first.Id}",
            new CommunityEventCreatedDto(EventsMapping.ToAdminDto(first), created.Count)));
    }

    private async Task<int?> ResolveImageIdAsync(int? requested, CancellationToken ct)
    {
        if (requested is not { } id) return null;
        var exists = await db.BlogImages.AsNoTracking().AnyAsync(i => i.Id == id, ct);
        return exists ? id : null;
    }
}
