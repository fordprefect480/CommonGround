using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CommonGround.Server.Events.Public;

public sealed class ListUpcomingEventsEndpoint(
    AppDbContext db,
    EventbriteClient eventbrite,
    IMemoryCache cache)
    : Endpoint<ListUpcomingEventsEndpoint.Request, List<UpcomingEventDto>>
{
    private const int DefaultTake = 3;
    private const int MaxTake = 24;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public sealed class Request
    {
        public int? Take { get; set; }
    }

    public override void Configure()
    {
        Get("/upcoming");
        Group<PublicEventsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var take = Math.Clamp(req.Take ?? DefaultTake, 1, MaxTake);

        var combined = await cache.GetOrCreateAsync(EventsCache.UpcomingKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await LoadCombinedAsync(ct);
        }) ?? [];

        await Send.OkAsync(combined.Take(take).ToList(), ct);
    }

    private async Task<List<UpcomingEventDto>> LoadCombinedAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var manualTask = db.CommunityEvents
            .AsNoTracking()
            .Where(e => (e.EndUtc ?? e.StartUtc) >= now)
            .OrderBy(e => e.StartUtc)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync(ct);

        var eventbriteTask = eventbrite.ListUpcomingAsync(MaxTake, ct);

        await Task.WhenAll(manualTask, eventbriteTask);

        var manual = manualTask.Result.Select(EventsMapping.ToUpcomingDto);
        var fromEventbrite = eventbriteTask.Result.Select((e, i) => EventsMapping.ToUpcomingDto(e, i)).OfType<UpcomingEventDto>();

        return manual.Concat(fromEventbrite).OrderBy(x => x.StartUtc).ToList();
    }
}
