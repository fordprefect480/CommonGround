using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

// NOTE: The "Delete bed" action is currently hidden from the admin UI, but this endpoint is
// intentionally kept and fully functional in case we need to surface bed deletion again.
public sealed class DeleteBedEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<DeleteBedEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public int BedId { get; set; }
    }

    public override void Configure()
    {
        Delete("/beds/{bedId:int}");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var bed = await db.Beds.SingleOrDefaultAsync(b => b.Id == req.BedId, ct);
        if (bed is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (await beds.IsBedOccupiedAsync(bed.Id, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new
            {
                error = "This bed is currently leased. Release or reassign its lease before deleting it.",
            }));
            return;
        }

        var label = bed.Label;
        bed.IsDeleted = true;
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "leased_bed.deleted",
            $"deleted leased bed {label}",
            targetType: "Bed",
            targetId: req.BedId.ToString(),
            ct: ct);

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
