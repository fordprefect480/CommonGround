using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CommonGround.Server.Events.Admin;

public sealed class DeleteCommunityEventEndpoint(
    AppDbContext db,
    IActivityLogger activityLogger,
    IMemoryCache cache)
    : Endpoint<DeleteCommunityEventEndpoint.Request>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Delete("/{id:int}");
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

        var deletedId = ev.Id;
        var deletedTitle = ev.Title;
        db.CommunityEvents.Remove(ev);
        await db.SaveChangesAsync(ct);

        EventsCache.InvalidateUpcoming(cache);

        await activityLogger.LogAsync(
            "event.deleted",
            $"removed the event \"{deletedTitle}\"",
            targetType: "CommunityEvent",
            targetId: deletedId.ToString(),
            ct: ct);

        await Send.NoContentAsync(ct);
    }
}
