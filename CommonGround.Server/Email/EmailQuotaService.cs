using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Email;

/// <summary>
/// Tracks how much of Resend's daily sending cap is left. Usage is counted from our own
/// sent-email history over a rolling 24-hour window rather than guessing when Resend's day
/// resets: any window that respects the cap also respects a calendar-day cap, and the count
/// survives restarts because it lives in the database.
/// </summary>
public sealed class EmailQuotaService(AppDbContext db, IOptions<EmailOptions> options, TimeProvider time)
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);

    /// <param name="DailyLimit">Resend's cap on emails per 24 hours.</param>
    /// <param name="TransactionalReserve">Part of the cap held back for membership emails.</param>
    /// <param name="SentLast24Hours">Emails (bulk and membership) accepted by Resend in the last 24 hours.</param>
    /// <param name="BulkRemaining">Bulk-send recipients that can go out right now.</param>
    /// <param name="QueuedTotal">Recipients across all emails still waiting for allowance.</param>
    /// <param name="NextAllowanceAtUtc">When allowance next frees up for bulk sends; null when some is available now.</param>
    public sealed record Snapshot(
        int DailyLimit,
        int TransactionalReserve,
        int SentLast24Hours,
        int BulkRemaining,
        int QueuedTotal,
        DateTime? NextAllowanceAtUtc);

    public async Task<Snapshot> GetAsync(CancellationToken ct)
    {
        var opts = options.Value;
        var now = time.GetUtcNow().UtcDateTime;
        var windowStart = now - Window;

        // Rows recorded before SentAtUtc existed fall back to their email's timestamp; back then every
        // recipient went out within the same request, so that is when Resend counted them.
        var sentTimes = await db.SentEmailRecipients
            .Where(r => r.Status == SentEmailRecipientStatus.Sent)
            .Select(r => r.SentAtUtc ?? r.SentEmail.SentAt)
            .Where(sentAt => sentAt > windowStart)
            .OrderBy(sentAt => sentAt)
            .ToListAsync(ct);

        var queuedTotal = await db.SentEmailRecipients
            .CountAsync(r => r.Status == SentEmailRecipientStatus.Queued, ct);

        var bulkRemaining = Math.Max(0, opts.BulkDailyAllowance - sentTimes.Count);

        // Membership mail can push usage past the bulk allowance, so more than one old send may need to age out.
        DateTime? nextAllowanceAt = null;
        if (bulkRemaining == 0 && opts.BulkDailyAllowance > 0)
        {
            var mustAgeOut = sentTimes.Count - opts.BulkDailyAllowance;
            nextAllowanceAt = sentTimes[mustAgeOut] + Window;
        }

        return new Snapshot(opts.DailyLimit, opts.TransactionalReserve, sentTimes.Count, bulkRemaining, queuedTotal, nextAllowanceAt);
    }
}
