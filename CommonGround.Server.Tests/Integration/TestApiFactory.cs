using System.Security.Claims;
using System.Text.Encodings.Web;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommonGround.Server.Tests.Integration;

/// <summary>
/// Boots the real API for integration tests against an isolated EF in-memory
/// database, with every request authenticated as an admin. Each instance gets
/// its own in-memory store, so create one per test for a clean slate.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"cg-tests-{Guid.NewGuid()}";

    public TestApiFactory()
    {
        // Aspire's AddSqlServerDbContext resolves this connection string while
        // Program is building - before our ConfigureServices swap runs - so a
        // syntactically valid placeholder keeps registration happy. The provider
        // is replaced with EF in-memory below and never actually connects.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__commongroundDb",
            "Server=localhost;Database=cg_test;Trusted_Connection=True;TrustServerCertificate=True");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the SQL Server context (and any Aspire/EF options or pool
            // registrations for it) with an isolated in-memory store.
            var toRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType == typeof(DbContextOptions)
                    || d.ServiceType == typeof(AppDbContext)
                    || (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") ?? false)
                    || (d.ServiceType.FullName?.Contains("IDbContextPool") ?? false))
                .ToList();
            foreach (var d in toRemove)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));

            // Authenticate every request as an admin so authorized endpoints are
            // reachable without driving the real cookie login flow.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultScheme = TestAuthHandler.SchemeName;
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }
}

/// <summary>Authenticates every request as an admin user.</summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-admin"),
            new Claim(ClaimTypes.Name, "test-admin@local"),
            new Claim(ClaimTypes.Role, AppRoles.Admin),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
