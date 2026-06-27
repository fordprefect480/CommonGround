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
        SeedUser(factory.Services, "boundary@example.com", renewalStart.AddSeconds(-1)); // just short -> not paid
        SeedUser(factory.Services, "never@example.com", paidThrough: null);              // never paid -> not paid

        var stats = await client.GetFromJsonAsync<StatsResponse>("/api/admin/members/stats");

        Assert.NotNull(stats);
        Assert.Equal(1, stats!.PaidMembers);
        Assert.Equal(2, stats.NotYetPaidMembers);
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
