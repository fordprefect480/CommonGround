using System.Net;
using System.Net.Http.Json;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommonGround.Server.Tests.Integration;

public class WheelchairBedTests
{
    private sealed record Bed(int Id, string Label, bool IsWheelchairAccessible);
    private sealed record Capacity(int Total, int Leased, int Remaining, bool IsFull);
    private sealed record Overview(Capacity Capacity, List<Bed> Beds);

    // The app skips DB init under the Testing environment, and EF in-memory only
    // applies HasData seeds on EnsureCreated - so create the store explicitly to
    // materialise the seeded beds before querying. Data Protection's
    // PersistKeysToDbContext<AppDbContext>() touches (and implicitly creates, empty)
    // the same in-memory store during host startup, before this call runs - so
    // EnsureCreated() alone would see the store as already created and skip seeding.
    // EnsureDeleted() first resets that, so EnsureCreated() actually applies HasData.
    private static void EnsureSeeded(TestApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Seeded_W_beds_are_wheelchair_accessible_and_others_are_not()
    {
        using var factory = new TestApiFactory();
        EnsureSeeded(factory);
        var client = factory.CreateClient();

        var overview = await client.GetFromJsonAsync<Overview>("/api/admin/leased-beds");

        Assert.NotNull(overview);
        var w1 = overview!.Beds.Single(b => b.Label == "W1");
        var n1 = overview.Beds.Single(b => b.Label == "N1");
        Assert.True(w1.IsWheelchairAccessible);
        Assert.False(n1.IsWheelchairAccessible);
    }

    [Fact]
    public async Task Admin_can_add_a_wheelchair_accessible_bed()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/admin/leased-beds/beds",
            new { label = "X1", isWheelchairAccessible = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var overview = await response.Content.ReadFromJsonAsync<Overview>();
        Assert.True(overview!.Beds.Single(b => b.Label == "X1").IsWheelchairAccessible);
    }

    private sealed record MyRequest(string Status, int? WaitlistPosition, bool RequiresWheelchairAccessible);
    private sealed record MyStatus(MyRequest? Request);
    private sealed record AdminRequest(int RequestId, bool RequiresWheelchairAccessible);
    private sealed record AdminRequests(List<AdminRequest> Pending, List<AdminRequest> Waitlist);

    [Fact]
    public async Task Applying_with_the_flag_surfaces_it_to_member_and_admin()
    {
        using var factory = new TestApiFactory();
        EnsureSeeded(factory); // reset + apply HasData bed seeds so capacity has free beds (applicant -> Pending)

        // TestAuthHandler authenticates every request as user id "test-admin";
        // seed that user with an active membership so the member apply endpoint accepts it.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new ApplicationUser
            {
                Id = "test-admin",
                Email = "test-admin@local",
                UserName = "test-admin@local",
                EmailConfirmed = true,
                MembershipPaidThroughUtc = DateTime.UtcNow.AddMonths(6),
                JoinedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        var client = factory.CreateClient();

        var apply = await client.PostAsJsonAsync("/api/leased-beds/requests", new { requiresWheelchairAccessible = true });
        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);

        var myStatus = await apply.Content.ReadFromJsonAsync<MyStatus>();
        Assert.NotNull(myStatus!.Request);
        Assert.True(myStatus.Request!.RequiresWheelchairAccessible);

        var adminRequests = await client.GetFromJsonAsync<AdminRequests>("/api/admin/leased-beds/requests");
        Assert.True(adminRequests!.Pending.Single().RequiresWheelchairAccessible);
    }
}
