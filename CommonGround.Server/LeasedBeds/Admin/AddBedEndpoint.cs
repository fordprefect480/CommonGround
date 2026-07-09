using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

// NOTE: The "Add bed" action is currently hidden from the admin UI, but this endpoint is
// intentionally kept and fully functional in case we need to surface bed creation again.
public sealed class AddBedEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<AddBedEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public string Label { get; set; } = "";
        public string? Notes { get; set; }
        public bool IsWheelchairAccessible { get; set; }
    }

    public override void Configure()
    {
        Post("/beds");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var label = LeasedBedService.NormalizeText(req.Label);
        if (label is null)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "A bed label is required." }));
            return;
        }

        if (await db.Beds.AnyAsync(b => b.Label == label, ct))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = $"A bed labelled \"{label}\" already exists." }));
            return;
        }

        var bed = new Bed
        {
            Label = label,
            Notes = LeasedBedService.NormalizeText(req.Notes),
            IsActive = true,
            IsWheelchairAccessible = req.IsWheelchairAccessible,
        };

        db.Beds.Add(bed);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Safety net for the unique-label index if two admins add the same label at once.
            await Send.ResultAsync(Results.BadRequest(new { error = $"A bed labelled \"{label}\" already exists." }));
            return;
        }

        await activityLogger.LogAsync(
            "leased_bed.added",
            $"added leased bed {bed.Label}",
            targetType: "Bed",
            targetId: bed.Id.ToString(),
            ct: ct);

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
