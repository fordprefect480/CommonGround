using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.LeasedBeds.Admin;

public sealed class AddBedEndpoint(
    AppDbContext db,
    LeasedBedService beds,
    IActivityLogger activityLogger)
    : Endpoint<AddBedEndpoint.Request, LeasedBedsOverview>
{
    public sealed class Request
    {
        public string Section { get; set; } = "";
        public string? Label { get; set; }
        public string? Notes { get; set; }
    }

    public override void Configure()
    {
        Post("/beds");
        Group<AdminLeasedBedsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var section = req.Section.Trim().ToUpperInvariant();
        if (section is not ("N" or "S"))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Section must be N or S." }));
            return;
        }

        var bed = new Bed
        {
            Section = section,
            Number = await beds.NextBedNumberAsync(section, ct),
            Label = LeasedBedService.NormalizeText(req.Label),
            Notes = LeasedBedService.NormalizeText(req.Notes),
            IsActive = true,
        };

        db.Beds.Add(bed);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two admins adding to the same section at once can race for the next number.
            await Send.ResultAsync(Results.BadRequest(new { error = "Couldn't add the bed just now - please try again." }));
            return;
        }

        await activityLogger.LogAsync(
            "leased_bed.added",
            $"added leased bed {bed.Code}",
            targetType: "Bed",
            targetId: bed.Id.ToString(),
            ct: ct);

        await Send.OkAsync(await beds.GetOverviewAsync(ct), ct);
    }
}
