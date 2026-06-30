using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Members;

public sealed class ListMemberEmailsEndpoint(AppDbContext db)
    : Endpoint<ListMemberEmailsEndpoint.Request, ListMemberEmailsEndpoint.Result>
{
    public sealed class Request
    {
        public string Id { get; set; } = "";
    }

    public sealed record Result(IReadOnlyList<Item> Items);

    public sealed record Item(
        long Id,
        DateTime SentAt,
        string Subject,
        string? SenderEmail,
        bool IsNewsletter,
        string Email,
        string Status,
        string? ErrorMessage);

    public override void Configure()
    {
        Get("/members/{id}/emails");
        Group<AdminGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        // Every email this member was a recipient of, newest first. The status
        // reflects delivery to *this* member specifically (not the email overall).
        var items = await db.SentEmailRecipients
            .Where(r => r.UserId == req.Id)
            .OrderByDescending(r => r.SentEmail.SentAt)
            .Select(r => new Item(
                r.SentEmailId,
                r.SentEmail.SentAt,
                r.SentEmail.Subject,
                r.SentEmail.SenderEmailSnapshot,
                r.SentEmail.IsNewsletter,
                r.Email,
                r.Status == SentEmailRecipientStatus.Sent ? "sent" : "failed",
                r.ErrorMessage))
            .ToListAsync(ct);

        await Send.OkAsync(new Result(items), ct);
    }
}
