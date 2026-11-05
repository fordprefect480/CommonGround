using System.Net;
using System.Net.Http.Json;
using CommonGround.Server.Data;
using CommonGround.Server.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommonGround.Server.Tests.Integration;

public class MembershipIntegrationTests
{
    private sealed record MemberResponse(string Id, DateTime? MembershipPaidThroughUtc);
    private sealed record StatsResponse(int PaidMembers, int NotYetPaidMembers, int NewMembersLast30Days);
    private sealed record RenewalResponse(DateTime RenewalTargetUtc);
    private sealed record LastPaymentResponse(string Id, DateTime? LastMembershipPaymentUtc);

    private static string SeedUser(IServiceProvider services, string email, DateTime? paidThrough)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            MembershipPaidThroughUtc = paidThrough,
            JoinedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    [Fact]
    public async Task Record_membership_payment_advances_paid_through_and_stores_a_paid_payment()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "payer@example.com", paidThrough: null);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/members/{userId}/record-membership-payment",
            new { amountCents = 2500 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        Assert.NotNull(member);
        Assert.Equal(MembershipPeriod.ComputePaidThrough(DateTime.UtcNow), member!.MembershipPaidThroughUtc);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payment = await db.MembershipPayments.SingleAsync(p => p.UserId == userId);
        Assert.Equal(MembershipPaymentStatus.Paid, payment.Status);
        Assert.Equal(2500, payment.AmountCents);
        Assert.Equal(PaymentMethod.Manual, payment.Method);
    }

    [Fact]
    public async Task Member_stats_count_paid_and_unpaid_by_the_renewal_year()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var renewalStart = MembershipPeriod.RenewalYearStart(DateTime.UtcNow);
        SeedUser(factory.Services, "paid@example.com", renewalStart.AddDays(2));         // into the renewal year -> paid
        SeedUser(factory.Services, "boundary@example.com", renewalStart.AddSeconds(-1)); // held membership, just lapsed -> not yet paid
        SeedUser(factory.Services, "never@example.com", paidThrough: null);              // never held a membership (mailing-list only) -> excluded from both counts

        var stats = await client.GetFromJsonAsync<StatsResponse>("/api/admin/members/stats");

        Assert.NotNull(stats);
        Assert.Equal(1, stats!.PaidMembers);
        Assert.Equal(1, stats.NotYetPaidMembers);
    }

    private static void SeedPayment(IServiceProvider services, string userId, MembershipPaymentStatus status, DateTime? paidAtUtc, int amountCents = 2500, int refundedCents = 0)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.MembershipPayments.Add(new MembershipPayment
        {
            UserId = userId,
            Method = PaymentMethod.Manual,
            AmountCents = amountCents,
            RefundedAmountCents = refundedCents,
            Status = status,
            CreatedAtUtc = paidAtUtc ?? DateTime.UtcNow,
            PaidAtUtc = paidAtUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Members_list_and_detail_report_the_most_recent_paid_membership_payment()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var older = new DateTime(2025, 7, 3, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);
        var payerId = SeedUser(factory.Services, "payer@example.com", paidThrough: null);
        SeedPayment(factory.Services, payerId, MembershipPaymentStatus.Paid, older);
        SeedPayment(factory.Services, payerId, MembershipPaymentStatus.Paid, newer);
        SeedPayment(factory.Services, payerId, MembershipPaymentStatus.Failed, paidAtUtc: null);
        var neverPaidId = SeedUser(factory.Services, "never@example.com", paidThrough: null);
        SeedPayment(factory.Services, neverPaidId, MembershipPaymentStatus.Pending, paidAtUtc: null);

        var members = await client.GetFromJsonAsync<List<LastPaymentResponse>>("/api/admin/members");
        Assert.NotNull(members);
        Assert.Equal(newer, members!.Single(m => m.Id == payerId).LastMembershipPaymentUtc);
        Assert.Null(members.Single(m => m.Id == neverPaidId).LastMembershipPaymentUtc);

        var detail = await client.GetFromJsonAsync<LastPaymentResponse>($"/api/admin/members/{payerId}");
        Assert.Equal(newer, detail!.LastMembershipPaymentUtc);
    }

    [Fact]
    public async Task Last_payment_skips_fully_refunded_payments_but_keeps_partial_refunds_and_comped_payments()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var older = new DateTime(2025, 7, 3, 0, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);
        var refundedId = SeedUser(factory.Services, "refunded@example.com", paidThrough: null);
        SeedPayment(factory.Services, refundedId, MembershipPaymentStatus.Paid, older);
        SeedPayment(factory.Services, refundedId, MembershipPaymentStatus.Paid, newer, refundedCents: 2500);
        var onlyRefundedId = SeedUser(factory.Services, "only-refunded@example.com", paidThrough: null);
        SeedPayment(factory.Services, onlyRefundedId, MembershipPaymentStatus.Paid, newer, refundedCents: 2500);
        var partialId = SeedUser(factory.Services, "partial@example.com", paidThrough: null);
        SeedPayment(factory.Services, partialId, MembershipPaymentStatus.Paid, newer, refundedCents: 1000);
        var compedId = SeedUser(factory.Services, "comped@example.com", paidThrough: null);
        SeedPayment(factory.Services, compedId, MembershipPaymentStatus.Paid, newer, amountCents: 0);

        var members = await client.GetFromJsonAsync<List<LastPaymentResponse>>("/api/admin/members");
        Assert.NotNull(members);
        Assert.Equal(older, members!.Single(m => m.Id == refundedId).LastMembershipPaymentUtc);
        Assert.Null(members.Single(m => m.Id == onlyRefundedId).LastMembershipPaymentUtc);
        Assert.Equal(newer, members.Single(m => m.Id == partialId).LastMembershipPaymentUtc);
        Assert.Equal(newer, members.Single(m => m.Id == compedId).LastMembershipPaymentUtc);

        var detail = await client.GetFromJsonAsync<LastPaymentResponse>($"/api/admin/members/{refundedId}");
        Assert.Equal(older, detail!.LastMembershipPaymentUtc);
    }

    [Fact]
    public async Task Membership_renewal_endpoint_returns_the_compute_paid_through_target()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var renewal = await client.GetFromJsonAsync<RenewalResponse>("/api/admin/members/membership-renewal");

        Assert.NotNull(renewal);
        Assert.Equal(MembershipPeriod.ComputePaidThrough(DateTime.UtcNow), renewal!.RenewalTargetUtc);
    }
}
