using System.Net;
using System.Net.Http.Json;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommonGround.Server.Tests.Integration;

public class ArchiveMemberIntegrationTests
{
    private static string SeedUser(IServiceProvider services, string email, string? id = null)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var user = new ApplicationUser
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            JoinedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    [Fact]
    public void Query_filter_hides_archived_users_but_ignore_filters_reveals_them()
    {
        using var factory = new TestApiFactory();
        var userId = SeedUser(factory.Services, "archived@example.com");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = db.Users.Single(u => u.Id == userId);
            user.ArchivedAtUtc = DateTime.UtcNow;
            db.SaveChanges();
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(db.Users.Any(u => u.Id == userId));
            Assert.True(db.Users.IgnoreQueryFilters().Any(u => u.Id == userId));
        }
    }

    private static async Task<string> SeedAdmin(IServiceProvider services, string email)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
            await roleManager.CreateAsync(new IdentityRole(AppRoles.Admin));
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            JoinedAt = DateTime.UtcNow,
        };
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, AppRoles.Admin);
        return user.Id;
    }

    [Fact]
    public async Task Archiving_a_member_returns_no_content_and_hides_them()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "gone@example.com");

        var delete = await client.DeleteAsync($"/api/admin/members/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var get = await client.GetAsync($"/api/admin/members/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Archiving_a_member_blocks_the_lookup_login_depends_on()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "loginblocked@example.com");

        var delete = await client.DeleteAsync($"/api/admin/members/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        // SignInManager.PasswordSignInAsync first finds the user by name/email; the
        // query filter makes that return null, so login cannot proceed.
        Assert.Null(await userManager.FindByEmailAsync("loginblocked@example.com"));
    }

    [Fact]
    public async Task Archiving_a_member_releases_their_active_bed_lease()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "leaseholder@example.com");

        int leaseId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lease = new BedLease
            {
                BedId = 1,
                UserId = userId,
                StartDate = new DateOnly(2026, 7, 1),
                ExpiresOn = new DateOnly(2027, 6, 30),
                Status = BedLeaseStatus.Active,
                PriceAtAllocationCents = 8000,
            };
            db.BedLeases.Add(lease);
            db.SaveChanges();
            leaseId = lease.Id;
        }

        var delete = await client.DeleteAsync($"/api/admin/members/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lease = await db.BedLeases.SingleAsync(l => l.Id == leaseId);
            Assert.Equal(BedLeaseStatus.Released, lease.Status);
            Assert.NotNull(lease.EndDate);
        }
    }

    [Fact]
    public async Task Cannot_archive_your_own_account()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        // TestAuthHandler authenticates every request as the user id "test-admin".
        SeedUser(factory.Services, "self@example.com", id: "test-admin");

        var delete = await client.DeleteAsync("/api/admin/members/test-admin");
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    [Fact]
    public async Task Cannot_archive_the_only_administrator()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var adminId = await SeedAdmin(factory.Services, "onlyadmin@example.com");

        var delete = await client.DeleteAsync($"/api/admin/members/{adminId}");
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
    }

    [Fact]
    public async Task Can_archive_an_admin_when_others_remain()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var firstId = await SeedAdmin(factory.Services, "admin1@example.com");
        await SeedAdmin(factory.Services, "admin2@example.com");

        var delete = await client.DeleteAsync($"/api/admin/members/{firstId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Archiving_a_member_keeps_their_payment_records()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "payer@example.com");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.MembershipPayments.Add(new MembershipPayment
            {
                UserId = userId,
                Method = PaymentMethod.Manual,
                StripeCheckoutSessionId = "",
                AmountCents = 2500,
                Currency = "aud",
                Status = MembershipPaymentStatus.Paid,
                PaidAtUtc = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        var delete = await client.DeleteAsync($"/api/admin/members/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.MembershipPayments.AnyAsync(p => p.UserId == userId));
        }
    }
}
