using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string AdminRole = "Admin";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddSqlServerDbContext<AppDbContext>("commongroundDb");

builder.Services.Configure<GardenOptions>(builder.Configuration.GetSection(GardenOptions.SectionName));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await SeedDevAdminAsync(sp);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/api/auth");
auth.MapIdentityApi<ApplicationUser>();
auth.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
}).RequireAuthorization();

var admin = app.MapGroup("/api/admin")
    .RequireAuthorization(p => p.RequireRole(AdminRole));

admin.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) =>
    Results.Ok(new { email = user.Identity?.Name, isAdmin = true }));

admin.MapGet("/members", async (AppDbContext db) =>
{
    var users = await db.Users.OrderBy(u => u.Email).ToListAsync();

    var rolePairs = await (
        from ur in db.UserRoles
        join r in db.Roles on ur.RoleId equals r.Id
        select new { ur.UserId, RoleName = r.Name! })
        .ToListAsync();

    var rolesByUser = rolePairs
        .GroupBy(x => x.UserId)
        .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToArray());

    var members = users
        .Select(u => new MemberDto(
            u.Id, u.Email, u.UserName, u.EmailConfirmed,
            rolesByUser.GetValueOrDefault(u.Id, [])))
        .ToList();

    return Results.Ok(members);
});

app.MapGet("/api/config", (IOptions<GardenOptions> garden) =>
    Results.Ok(new { gardenName = garden.Value.Name }));

app.MapGet("/api/health/ping", () => Results.Ok(new { status = "ok" }));

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

static async Task SeedDevAdminAsync(IServiceProvider sp)
{
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(AdminRole))
        await roleManager.CreateAsync(new IdentityRole(AdminRole));

    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    const string adminEmail = "admin@local";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Password123!");
        if (!result.Succeeded) return;
    }

    if (!await userManager.IsInRoleAsync(admin, AdminRole))
        await userManager.AddToRoleAsync(admin, AdminRole);
}

internal record MemberDto(string Id, string? Email, string? UserName, bool EmailConfirmed, string[] Roles);
