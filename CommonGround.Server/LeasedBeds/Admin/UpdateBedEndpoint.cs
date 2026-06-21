using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

public sealed class UpdateBedEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<UpdateBedEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public int BedId { get; set; }

        /// <summary>When provided, replaces the label (empty clears it). Null leaves it unchanged.</summary>
        public string? Label { get; set; }

        /// <summary>When provided, replaces the notes (empty clears them). Null leaves them unchanged.</summary>
        public string? Notes { get; set; }

        /// <summary>When provided, takes the bed out of service (false) or returns it (true). Null leaves it unchanged.</summary>
        public bool? IsActive { get; set; }
    }

    public override void Configure()
    {
        Patch("/beds/{bedId:int}");
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

        var deactivating = req.IsActive == false && bed.IsActive;
        if (deactivating && await beds.IsBedOccupiedAsync(bed.Id, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new
            {
                error = "This bed is currently leased and can't be taken out of service. Release or reassign its lease first.",
            }));
            return;
        }

        if (req.Label is not null) bed.Label = LeasedBedService.NormalizeText(req.Label);
        if (req.Notes is not null) bed.Notes = LeasedBedService.NormalizeText(req.Notes);

        var serviceChanged = req.IsActive is { } active && active != bed.IsActive;
        if (req.IsActive is not null) bed.IsActive = req.IsActive.Value;

        await db.SaveChangesAsync(ct);

        var summary = serviceChanged
            ? bed.IsActive
                ? $"returned leased bed {bed.Code} to service"
                : $"took leased bed {bed.Code} out of service"
            : $"updated leased bed {bed.Code}";

        await activityLogger.LogAsync(
            "leased_bed.updated",
            summary,
            targetType: "Bed",
            targetId: bed.Id.ToString(),
            ct: ct);

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
