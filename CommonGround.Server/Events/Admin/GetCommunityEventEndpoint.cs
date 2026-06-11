using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Events.Admin;

public sealed class GetCommunityEventEndpoint(AppDbContext db)
    : Endpoint<GetCommunityEventEndpoint.Request, CommunityEventAdminDto>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Get("/{id:int}");
        Group<AdminEventsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var ev = await db.CommunityEvents.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == req.Id, ct);
        if (ev is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        await Send.OkAsync(EventsMapping.ToAdminDto(ev), ct);
    }
}
