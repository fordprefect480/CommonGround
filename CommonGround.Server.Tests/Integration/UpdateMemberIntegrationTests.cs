using System.Net;
using System.Net.Http.Json;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommonGround.Server.Tests.Integration;

public class UpdateMemberIntegrationTests
{
    private sealed record MemberResponse(string? Address, string[] SecondaryMembers);

    private static string SeedUser(IServiceProvider services, string email)
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
            JoinedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    [Fact]
    public async Task Update_member_persists_address_and_secondary_members()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "member@example.com");

        var response = await client.PutAsJsonAsync(
            $"/api/admin/members/{userId}",
            new
            {
                firstName = "Ada",
                lastName = "Lovelace",
                phoneNumber = "0400000000",
                address = "  12 Garden St  ",
                secondaryMembers = new[] { " Alan ", "", "  ", "Grace" },
                isAdmin = false,
                isSubscribedToMailingList = true,
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var member = await response.Content.ReadFromJsonAsync<MemberResponse>();
        Assert.NotNull(member);
        Assert.Equal("12 Garden St", member!.Address);
        Assert.Equal(new[] { "Alan", "Grace" }, member.SecondaryMembers);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var savedNames = await db.SecondaryMembers
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.Id)
            .Select(s => s.FullName)
            .ToArrayAsync();
        Assert.Equal(new[] { "Alan", "Grace" }, savedNames);
        var savedAddress = await db.Users.Where(u => u.Id == userId).Select(u => u.Address).SingleAsync();
        Assert.Equal("12 Garden St", savedAddress);
    }

    [Fact]
    public async Task Update_member_rejects_more_than_four_secondary_members()
    {
        using var factory = new TestApiFactory();
        var client = factory.CreateClient();
        var userId = SeedUser(factory.Services, "toomany@example.com");

        var response = await client.PutAsJsonAsync(
            $"/api/admin/members/{userId}",
            new
            {
                firstName = "Ada",
                lastName = "Lovelace",
                phoneNumber = (string?)null,
                address = (string?)null,
                secondaryMembers = new[] { "A", "B", "C", "D", "E" },
                isAdmin = false,
                isSubscribedToMailingList = true,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
