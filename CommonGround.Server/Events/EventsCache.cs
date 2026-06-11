using Microsoft.Extensions.Caching.Memory;

namespace CommonGround.Server.Events;

internal static class EventsCache
{
    public const string UpcomingKey = "events.upcoming";

    public static void InvalidateUpcoming(IMemoryCache cache) => cache.Remove(UpcomingKey);
}
