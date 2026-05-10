using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Activity;

public sealed class ListActivityEndpoint(AppDbContext db)
    : Endpoint<ListActivityEndpoint.Request, ActivityListDto>
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    public sealed class Request
    {
        public string? Cursor { get; set; }
        public int? Take { get; set; }
    }

    public override void Configure()
    {
        Get("/activity");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var take = Math.Clamp(req.Take ?? DefaultTake, 1, MaxTake);

        var query = db.Activities.AsNoTracking()
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .AsQueryable();

        if (TryParseCursor(req.Cursor, out var cursorTicks, out var cursorId))
        {
            var cursorTime = new DateTime(cursorTicks, DateTimeKind.Utc);
            query = query.Where(a =>
                a.OccurredAt < cursorTime ||
                (a.OccurredAt == cursorTime && a.Id < cursorId));
        }

        var rows = await query
            .Take(take + 1)
            .Select(a => new
            {
                a.Id,
                a.OccurredAt,
                a.ActivityType,
                a.ActorUserId,
                ActorLiveEmail = a.Actor != null ? a.Actor.Email : null,
                a.ActorEmailSnapshot,
                a.Summary,
                a.TargetType,
                a.TargetId,
                a.DetailsJson,
            })
            .ToListAsync(ct);

        string? nextCursor = null;
        if (rows.Count > take)
        {
            var last = rows[take - 1];
            nextCursor = $"{last.OccurredAt.Ticks}:{last.Id}";
            rows = rows.Take(take).ToList();
        }

        var items = rows
            .Select(r => new ActivityItemDto(
                r.Id,
                r.OccurredAt,
                r.ActivityType,
                r.ActorUserId,
                r.ActorLiveEmail ?? r.ActorEmailSnapshot,
                r.Summary,
                r.TargetType,
                r.TargetId,
                r.DetailsJson))
            .ToList();

        await Send.OkAsync(new ActivityListDto(items, nextCursor), ct);
    }

    private static bool TryParseCursor(string? cursor, out long ticks, out long id)
    {
        ticks = 0;
        id = 0;
        if (string.IsNullOrWhiteSpace(cursor)) return false;
        var parts = cursor.Split(':');
        return parts.Length == 2
            && long.TryParse(parts[0], out ticks)
            && long.TryParse(parts[1], out id);
    }
}
