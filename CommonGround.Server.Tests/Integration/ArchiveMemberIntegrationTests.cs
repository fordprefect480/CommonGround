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

    [Fact]
    public async Task Archiving_a_member_frees_their_email_for_the_unique_username_index()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "rejoin@example.com");
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId);
            await userManager.UpdateNormalizedUserNameAsync(user!);
        }

        var delete = await client.DeleteAsync($"/api/admin/members/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var archived = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == userId);
            Assert.NotEqual("REJOIN@EXAMPLE.COM", archived.NormalizedUserName);
            Assert.Equal("rejoin@example.com", archived.Email);
        }
    }

    [Fact]
    public async Task A_deleted_member_can_join_the_mailing_list_again()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var oldId = SeedUser(factory.Services, "comeback@example.com");

        var delete = await client.DeleteAsync($"/api/admin/members/{oldId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var subscribe = await client.PostAsJsonAsync("/api/subscribe", new { email = "comeback@example.com", captchaToken = (string?)null });
        Assert.Equal(HttpStatusCode.OK, subscribe.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rejoined = await db.Users.SingleAsync(u => u.Email == "comeback@example.com");
        Assert.NotEqual(oldId, rejoined.Id);
    }

    [Fact]
    public async Task A_deleted_member_can_be_added_again_by_an_admin()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var oldId = SeedUser(factory.Services, "readded@example.com");

        var delete = await client.DeleteAsync($"/api/admin/members/{oldId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var create = await client.PostAsJsonAsync("/api/admin/members", new { email = "readded@example.com", password = "TestPass123!", isAdmin = false, isSubscribedToMailingList = true });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    private static Task<HttpResponseMessage> BulkDelete(HttpClient client, params string[] ids) =>
        client.PostAsJsonAsync("/api/admin/members/bulk-delete", new { ids });

    [Fact]
    public async Task Bulk_delete_archives_every_selected_member_and_releases_leases()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var firstId = SeedUser(factory.Services, "bulk1@example.com");
        var secondId = SeedUser(factory.Services, "bulk2@example.com");
        var keptId = SeedUser(factory.Services, "kept@example.com");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.BedLeases.Add(new BedLease
            {
                BedId = 1,
                UserId = secondId,
                StartDate = new DateOnly(2026, 7, 1),
                ExpiresOn = new DateOnly(2027, 6, 30),
                Status = BedLeaseStatus.Active,
                PriceAtAllocationCents = 8000,
            });
            db.SaveChanges();
        }

        var response = await BulkDelete(client, firstId, secondId);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Users.AnyAsync(u => u.Id == firstId || u.Id == secondId));
            Assert.True(await db.Users.AnyAsync(u => u.Id == keptId));
            var lease = await db.BedLeases.SingleAsync(l => l.UserId == secondId);
            Assert.Equal(BedLeaseStatus.Released, lease.Status);
        }
    }

    [Fact]
    public async Task Bulk_delete_including_yourself_archives_no_one()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        SeedUser(factory.Services, "self@example.com", id: "test-admin");
        var otherId = SeedUser(factory.Services, "other@example.com");

        var response = await BulkDelete(client, otherId, "test-admin");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Users.AnyAsync(u => u.Id == otherId));
    }

    [Fact]
    public async Task Bulk_delete_of_every_administrator_is_rejected()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var firstAdmin = await SeedAdmin(factory.Services, "admin1@example.com");
        var secondAdmin = await SeedAdmin(factory.Services, "admin2@example.com");

        var response = await BulkDelete(client, firstAdmin, secondAdmin);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.Users.CountAsync(u => u.Id == firstAdmin || u.Id == secondAdmin));
    }

    [Fact]
    public async Task Bulk_delete_ignores_ids_that_no_longer_exist()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var memberId = SeedUser(factory.Services, "real@example.com");

        var response = await BulkDelete(client, memberId, "no-such-member");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Id == memberId));
    }

    [Fact]
    public async Task Bulk_delete_of_some_administrators_is_allowed_when_one_remains()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var firstAdmin = await SeedAdmin(factory.Services, "admin1@example.com");
        await SeedAdmin(factory.Services, "admin2@example.com");
        var memberId = SeedUser(factory.Services, "member@example.com");

        var response = await BulkDelete(client, firstAdmin, memberId);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Bulk_delete_with_no_ids_is_rejected()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();

        var response = await BulkDelete(client);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
