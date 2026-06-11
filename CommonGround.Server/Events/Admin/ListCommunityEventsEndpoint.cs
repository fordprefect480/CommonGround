using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Events.Admin;

public sealed class ListCommunityEventsEndpoint(AppDbContext db)
    : EndpointWithoutRequest<List<CommunityEventAdminDto>>
{
    public override void Configure()
    {
        Get("");
        Group<AdminEventsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var events = await db.CommunityEvents
            .AsNoTracking()
            .OrderBy(e => e.StartUtc)
            .ThenBy(e => e.DisplayOrder)
            .ToListAsync(ct);

        await Send.OkAsync(events.Select(EventsMapping.ToAdminDto).ToList(), ct);
    }
}
