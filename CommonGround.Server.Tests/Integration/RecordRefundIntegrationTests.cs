using System.Net;
using System.Net.Http.Json;
using CommonGround.Server.Data;
using CommonGround.Server.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommonGround.Server.Tests.Integration;

public class RecordRefundIntegrationTests
{
    private sealed record MemberResponse(string Id, DateTime? MembershipPaidThroughUtc);

    private static (string UserId, int[] PaymentIds) SeedMemberWithPayments(IServiceProvider services, int paymentCount)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        var now = DateTime.UtcNow;
        var paidThrough = MembershipPeriod.ComputePaidThrough(now);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "refund@example.com",
            UserName = "refund@example.com",
            EmailConfirmed = true,
            MembershipPaidThroughUtc = paidThrough,
            JoinedAt = now,
        };
        db.Users.Add(user);

        var payments = Enumerable.Range(0, paymentCount).Select(_ => new MembershipPayment
        {
            UserId = user.Id,
            Method = PaymentMethod.Stripe,
            StripeCheckoutSessionId = Guid.NewGuid().ToString(),
            AmountCents = 2500,
            Status = MembershipPaymentStatus.Paid,
            CreatedAtUtc = now,
            PaidAtUtc = now,
            PeriodStartUtc = now,
            PeriodEndUtc = paidThrough,
        }).ToList();
        db.MembershipPayments.AddRange(payments);
        db.SaveChanges();

        return (user.Id, payments.Select(p => p.Id).ToArray());
    }

    [Fact]
    public async Task Refunding_a_duplicate_payment_records_the_refund_and_keeps_membership()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var (userId, paymentIds) = SeedMemberWithPayments(factory.Services, paymentCount: 2);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/members/{userId}/payments/{paymentIds[1]}/refund",
            new { kind = "Membership", amountCents = 2500 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        Assert.Equal(MembershipPeriod.ComputePaidThrough(DateTime.UtcNow), member!.MembershipPaidThroughUtc);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refunded = await db.MembershipPayments.SingleAsync(p => p.Id == paymentIds[1]);
        Assert.Equal(2500, refunded.RefundedAmountCents);
        Assert.NotNull(refunded.RefundedAtUtc);
        Assert.Equal(MembershipPaymentStatus.Paid, refunded.Status);
    }

    [Fact]
    public async Task Fully_refunding_the_only_payment_rolls_membership_back()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var (userId, paymentIds) = SeedMemberWithPayments(factory.Services, paymentCount: 1);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/members/{userId}/payments/{paymentIds[0]}/refund",
            new { kind = "Membership", amountCents = 2500 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        Assert.False(MembershipPeriod.IsPaidForRenewalYear(member!.MembershipPaidThroughUtc, DateTime.UtcNow));
    }

    [Fact]
    public async Task Refund_above_the_remaining_amount_is_rejected()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var (userId, paymentIds) = SeedMemberWithPayments(factory.Services, paymentCount: 1);

        var first = await client.PostAsJsonAsync(
            $"/api/admin/members/{userId}/payments/{paymentIds[0]}/refund",
            new { kind = "Membership", amountCents = 2000 });
        var second = await client.PostAsJsonAsync(
            $"/api/admin/members/{userId}/payments/{paymentIds[0]}/refund",
            new { kind = "Membership", amountCents = 1000 });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2000, (await db.MembershipPayments.SingleAsync(p => p.Id == paymentIds[0])).RefundedAmountCents);
    }

    [Fact]
    public async Task Refund_of_another_members_payment_is_not_found()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var (_, paymentIds) = SeedMemberWithPayments(factory.Services, paymentCount: 1);

        var response = await client.PostAsJsonAsync(
            $"/api/admin/members/{Guid.NewGuid()}/payments/{paymentIds[0]}/refund",
            new { kind = "Membership", amountCents = 2500 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
