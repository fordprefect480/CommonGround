using CommonGround.Server.Data;
using CommonGround.Server.Email;
using CommonGround.Server.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Tests;

public class EmailQuotaServiceTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    private static (EmailQuotaService Quota, AppDbContext Db) Build(int dailyLimit = 10, int reserve = 2)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase($"quota-{Guid.NewGuid()}").Options);
        var options = Options.Create(new EmailOptions { DailyLimit = dailyLimit, TransactionalReserve = reserve });
        return (new EmailQuotaService(db, options, new ManualTimeProvider(Now)), db);
    }

    private static void AddRecipients(AppDbContext db, SentEmailRecipientStatus status, params DateTime?[] sentAt) =>
        AddEmailWithRecipients(db, status, emailSentAt: default, sentAt);

    private static void AddEmailWithRecipients(AppDbContext db, SentEmailRecipientStatus status, DateTime emailSentAt, params DateTime?[] sentAt)
    {
        var email = new SentEmail { Subject = "s", HtmlBody = "b", SentAt = emailSentAt };
        foreach (var at in sentAt)
        {
            email.Recipients.Add(new SentEmailRecipient { Email = "x@example.com", Status = status, SentAtUtc = at });
        }
        db.SentEmails.Add(email);
        db.SaveChanges();
    }

    [Fact]
    public async Task Counts_only_successful_sends_inside_the_last_24_hours()
    {
        var (quota, db) = Build();
        AddRecipients(db, SentEmailRecipientStatus.Sent, Now.AddHours(-1), Now.AddHours(-23.9), Now.AddHours(-24.1));
        AddRecipients(db, SentEmailRecipientStatus.Failed, null, null);
        AddRecipients(db, SentEmailRecipientStatus.Queued, null, null, null);

        var snapshot = await quota.GetAsync(default);

        Assert.Equal(2, snapshot.SentLast24Hours);
        Assert.Equal(8 - 2, snapshot.BulkRemaining);
        Assert.Equal(3, snapshot.QueuedTotal);
        Assert.Null(snapshot.NextAllowanceAtUtc);
    }

    [Fact]
    public async Task Sends_recorded_before_SentAtUtc_existed_count_from_their_email_timestamp()
    {
        var (quota, db) = Build();
        AddEmailWithRecipients(db, SentEmailRecipientStatus.Sent, emailSentAt: Now.AddHours(-2), null, null, null);
        AddEmailWithRecipients(db, SentEmailRecipientStatus.Sent, emailSentAt: Now.AddHours(-30), null, null);

        var snapshot = await quota.GetAsync(default);

        Assert.Equal(3, snapshot.SentLast24Hours);
    }

    [Fact]
    public async Task Reserve_is_held_back_from_bulk_sends()
    {
        var (quota, db) = Build(dailyLimit: 10, reserve: 2);
        AddRecipients(db, SentEmailRecipientStatus.Sent, Enumerable.Range(1, 8).Select(h => (DateTime?)Now.AddHours(-h)).ToArray());

        var snapshot = await quota.GetAsync(default);

        Assert.Equal(0, snapshot.BulkRemaining);
        Assert.Equal(Now.AddHours(-8) + EmailQuotaService.Window, snapshot.NextAllowanceAtUtc);
    }

    [Fact]
    public async Task Membership_mail_past_the_bulk_allowance_pushes_the_next_slot_later()
    {
        var (quota, db) = Build(dailyLimit: 10, reserve: 2);
        // 10 sends against a bulk allowance of 8: the three oldest must age out before one bulk slot opens.
        AddRecipients(db, SentEmailRecipientStatus.Sent, Enumerable.Range(1, 10).Select(h => (DateTime?)Now.AddHours(-h)).ToArray());

        var snapshot = await quota.GetAsync(default);

        Assert.Equal(0, snapshot.BulkRemaining);
        Assert.Equal(Now.AddHours(-8) + EmailQuotaService.Window, snapshot.NextAllowanceAtUtc);
    }
}
