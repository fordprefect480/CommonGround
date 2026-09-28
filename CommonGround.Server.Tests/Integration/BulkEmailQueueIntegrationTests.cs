using System.Net;
using System.Net.Http.Json;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using CommonGround.Server.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Resend;

namespace CommonGround.Server.Tests.Integration;

public class BulkEmailQueueIntegrationTests
{
    // DailyLimit 5 with 1 held back for membership mail leaves 4 bulk sends per 24 hours.
    private const int DailyLimit = 5;
    private const int Reserve = 1;
    private const int BulkAllowance = DailyLimit - Reserve;

    private static readonly DateTimeOffset Start = new(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);

    private sealed record SendResponse(long Id, int Sent, int Failed, int Queued);
    private sealed record QuotaResponse(int DailyLimit, int TransactionalReserve, int SentLast24Hours, int BulkRemaining, int QueuedTotal, DateTime? NextAllowanceAtUtc);

    private sealed class EmailApiFactory : TestApiFactory
    {
        private readonly IResend _resend;

        public EmailApiFactory()
        {
            _resend = FakeResend.Create(out var fake);
            Fake = fake;
        }

        public FakeResend Fake { get; }
        public ManualTimeProvider Clock { get; } = new(Start);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IResend>();
                services.AddSingleton(_resend);
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
                services.PostConfigure<EmailOptions>(o =>
                {
                    o.ApiToken = "test-token";
                    o.FromAddress = "garden@example.org";
                    o.TemplateId = Guid.NewGuid().ToString();
                    o.TransactionalTemplateId = Guid.NewGuid().ToString();
                    o.DailyLimit = DailyLimit;
                    o.TransactionalReserve = Reserve;
                });
            });
        }
    }

    private static List<string> SeedSubscribers(IServiceProvider services, int count)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var ids = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var email = $"member{i:D2}@example.com";
            var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = email, UserName = email, IsSubscribedToMailingList = true };
            db.Users.Add(user);
            ids.Add(user.Id);
        }
        db.SaveChanges();
        return ids;
    }

    private static async Task<SendResponse> SendNewsletterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/admin/tools/email/newsletter", new { subject = "Spring working bee", htmlBody = "<p>Hi</p>", mode = "all_subscribers", isNewsletter = true });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SendResponse>())!;
    }

    private static async Task RunWorkerAsync(EmailApiFactory factory) =>
        await factory.Services.GetRequiredService<BulkEmailQueueWorker>().RunOnceAsync(default);

    private static SentEmail LoadEmail(IServiceProvider services, long id)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.SentEmails.Include(e => e.Recipients).Single(e => e.Id == id);
    }

    [Fact]
    public async Task Send_within_the_allowance_goes_out_immediately()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, BulkAllowance);

        var result = await SendNewsletterAsync(client);

        Assert.Equal(new SendResponse(result.Id, BulkAllowance, 0, 0), result);
        Assert.Equal(BulkAllowance, factory.Fake.Sent.Count);
        var email = LoadEmail(factory.Services, result.Id);
        Assert.Equal(email.Recipients.Select(r => $"sent-email-recipient-{r.Id}").Order(), factory.Fake.IdempotencyKeys.Order());
        Assert.All(email.Recipients, r =>
        {
            Assert.Equal(SentEmailRecipientStatus.Sent, r.Status);
            Assert.Equal(Start.UtcDateTime, r.SentAtUtc);
        });
    }

    [Fact]
    public async Task Send_over_the_allowance_queues_the_rest_and_reports_when_it_resumes()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 7);

        var result = await SendNewsletterAsync(client);

        Assert.Equal(BulkAllowance, result.Sent);
        Assert.Equal(3, result.Queued);
        Assert.Equal(BulkAllowance, factory.Fake.Sent.Count);

        var email = LoadEmail(factory.Services, result.Id);
        Assert.Equal(3, email.QueuedCount);
        Assert.Equal(3, email.Recipients.Count(r => r.Status == SentEmailRecipientStatus.Queued));

        var quota = await client.GetFromJsonAsync<QuotaResponse>("/api/admin/tools/email/quota");
        Assert.Equal(new QuotaResponse(DailyLimit, Reserve, BulkAllowance, 0, 3, Start.UtcDateTime.AddHours(24)), quota);
    }

    [Fact]
    public async Task Worker_does_nothing_until_the_allowance_frees_then_finishes_the_send()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 7);
        var result = await SendNewsletterAsync(client);

        factory.Clock.Advance(TimeSpan.FromHours(23));
        await RunWorkerAsync(factory);
        Assert.Equal(BulkAllowance, factory.Fake.Sent.Count);

        factory.Clock.Advance(TimeSpan.FromHours(1));
        await RunWorkerAsync(factory);

        Assert.Equal(7, factory.Fake.Sent.Count);
        Assert.Equal(7, factory.Fake.Sent.Select(m => m.To.Single().Email).Distinct().Count());
        var email = LoadEmail(factory.Services, result.Id);
        Assert.Equal(0, email.QueuedCount);
        Assert.Equal(7, email.SentCount);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var activity = await db.Activities.SingleAsync(a => a.ActivityType == "email.queued_batch_sent");
        Assert.StartsWith("Finished sending \"Spring working bee\"", activity.Summary);
    }

    [Fact]
    public async Task Queued_newsletter_skips_a_member_who_unsubscribed_in_the_meantime()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 6);
        var result = await SendNewsletterAsync(client);

        var queuedUserId = LoadEmail(factory.Services, result.Id).Recipients.First(r => r.Status == SentEmailRecipientStatus.Queued).UserId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Users.SingleAsync(u => u.Id == queuedUserId)).IsSubscribedToMailingList = false;
            await db.SaveChangesAsync();
        }

        factory.Clock.Advance(TimeSpan.FromHours(24));
        await RunWorkerAsync(factory);

        var email = LoadEmail(factory.Services, result.Id);
        Assert.Equal(SentEmailRecipientStatus.Skipped, email.Recipients.Single(r => r.UserId == queuedUserId).Status);
        Assert.Equal(5, email.SentCount);
        Assert.Equal(0, email.QueuedCount);
        Assert.DoesNotContain(factory.Fake.Sent, m => m.To.Single().Email == email.Recipients.Single(r => r.UserId == queuedUserId).Email);
    }

    [Fact]
    public async Task Queued_email_skips_a_member_who_was_deleted_in_the_meantime()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 6);
        var result = await SendNewsletterAsync(client);

        var queued = LoadEmail(factory.Services, result.Id).Recipients.First(r => r.Status == SentEmailRecipientStatus.Queued);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Users.SingleAsync(u => u.Id == queued.UserId)).ArchivedAtUtc = factory.Clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync();
        }

        factory.Clock.Advance(TimeSpan.FromHours(24));
        await RunWorkerAsync(factory);

        var email = LoadEmail(factory.Services, result.Id);
        Assert.Equal(SentEmailRecipientStatus.Skipped, email.Recipients.Single(r => r.Id == queued.Id).Status);
        Assert.Equal(5, email.SentCount);
        Assert.DoesNotContain(factory.Fake.Sent, m => m.To.Single().Email == queued.Email);
    }

    [Fact]
    public async Task Resend_quota_refusal_leaves_recipients_queued_rather_than_failed()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 3);
        factory.Fake.FailWith = m => m.To.Single().Email == "member01@example.com" ? FakeResend.QuotaExceeded() : null;

        var result = await SendNewsletterAsync(client);

        Assert.Equal(1, result.Sent);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, result.Queued);

        factory.Fake.FailWith = _ => null;
        factory.Clock.Advance(BulkEmailQueueWorker.Interval);
        await RunWorkerAsync(factory);

        var email = LoadEmail(factory.Services, result.Id);
        Assert.Equal(3, email.SentCount);
        Assert.Equal(0, email.FailedCount);
    }

    [Fact]
    public async Task Resend_rejecting_a_reused_idempotency_key_records_the_recipient_as_sent()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 2);
        factory.Fake.FailWith = m => m.To.Single().Email == "member01@example.com" ? FakeResend.AlreadySent() : null;

        var result = await SendNewsletterAsync(client);

        Assert.Equal(2, result.Sent);
        Assert.Equal(0, result.Failed);
        Assert.All(LoadEmail(factory.Services, result.Id).Recipients, r => Assert.Equal(SentEmailRecipientStatus.Sent, r.Status));
    }

    [Fact]
    public async Task Newer_send_waits_behind_an_email_that_is_still_going_out()
    {
        using var factory = new EmailApiFactory();
        var client = factory.CreateClient();
        SeedSubscribers(factory.Services, 6);
        var first = await SendNewsletterAsync(client);

        factory.Clock.Advance(TimeSpan.FromHours(24));
        var second = await SendNewsletterAsync(client);

        Assert.Equal(2, second.Sent);
        Assert.Equal(4, second.Queued);
        Assert.Equal(0, LoadEmail(factory.Services, first.Id).QueuedCount);
    }
}
