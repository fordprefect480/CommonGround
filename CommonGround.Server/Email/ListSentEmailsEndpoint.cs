using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Email;

public sealed class ListSentEmailsEndpoint(AppDbContext db)
    : EndpointWithoutRequest<ListSentEmailsEndpoint.Result>
{
    public sealed record Result(IReadOnlyList<Item> Items);
    public sealed record Item(
        long Id,
        DateTime SentAt,
        string Subject,
        string? SenderEmail,
        bool IsNewsletter,
        string? RecipientEmail,
        int RecipientCount);

    public override void Configure()
    {
        Get("/email/sent");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await db.SentEmails
            .OrderByDescending(e => e.SentAt)
            .Select(e => new Item(
                e.Id,
                e.SentAt,
                e.Subject,
                e.SenderEmailSnapshot,
                e.IsNewsletter,
                e.Recipients.Select(r => r.Email).FirstOrDefault(),
                e.RecipientCount))
            .ToListAsync(ct);

        await Send.OkAsync(new Result(items), ct);
    }
}
