using ClosedXML.Excel;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ExportMembersEndpoint(AppDbContext db) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/members/export.xlsx");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var users = await db.Users.OrderBy(u => u.Email).ToListAsync(ct);

        var rolePairs = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(ct);

        var rolesByUser = rolePairs
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.RoleName).Order()));

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Members");

        string[] headers = ["First name", "Last name", "Email", "Username", "Phone", "Member since", "Roles", "Email confirmed", "Subscribed to mailing list"];
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];
        ws.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < users.Count; i++)
        {
            var u = users[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = u.FirstName;
            ws.Cell(row, 2).Value = u.LastName;
            ws.Cell(row, 3).Value = u.Email;
            ws.Cell(row, 4).Value = u.UserName;
            ws.Cell(row, 5).Value = u.PhoneNumber;
            ws.Cell(row, 6).Value = u.JoinedAt;
            ws.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Cell(row, 7).Value = rolesByUser.GetValueOrDefault(u.Id, "");
            ws.Cell(row, 8).Value = u.EmailConfirmed ? "Yes" : "No";
            ws.Cell(row, 9).Value = u.IsSubscribedToMailingList ? "Yes" : "No";
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        wb.SaveAs(stream);

        var fileName = $"members-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        await Send.BytesAsync(
            stream.ToArray(),
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            cancellation: ct);
    }
}
