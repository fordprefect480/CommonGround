using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Email;

public sealed class GetSentEmailEndpoint(AppDbContext db)
    : Endpoint<GetSentEmailEndpoint.Request, GetSentEmailEndpoint.Result>
{
    public sealed record Request(long Id);
    public sealed record Result(
        long Id,
        DateTime SentAt,
        string Subject,
        string HtmlBody,
        string TextBody,
        string? SenderUserId,
        string? SenderEmail,
        int RecipientCount,
        int SentCount,
        int FailedCount,
        IReadOnlyList<Recipient> Recipients);

    public sealed record Recipient(
        long Id,
        string? UserId,
        string Email,
        string Status,
        string? ErrorMessage);

    public override void Configure()
    {
        Get("/email/sent/{Id}");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var entity = await db.SentEmails
            .Where(e => e.Id == req.Id)
            .Select(e => new
            {
                e.Id,
                e.SentAt,
                e.Subject,
                e.HtmlBody,
                e.TextBody,
                e.SenderUserId,
                e.SenderEmailSnapshot,
                e.RecipientCount,
                e.SentCount,
                e.FailedCount,
                Recipients = e.Recipients
                    .OrderBy(r => r.Status)
                    .ThenBy(r => r.Email)
                    .Select(r => new Recipient(
                        r.Id,
                        r.UserId,
                        r.Email,
                        r.Status == SentEmailRecipientStatus.Sent ? "sent" : "failed",
                        r.ErrorMessage))
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new Result(
            entity.Id,
            entity.SentAt,
            entity.Subject,
            entity.HtmlBody,
            entity.TextBody,
            entity.SenderUserId,
            entity.SenderEmailSnapshot,
            entity.RecipientCount,
            entity.SentCount,
            entity.FailedCount,
            entity.Recipients), ct);
    }
}
