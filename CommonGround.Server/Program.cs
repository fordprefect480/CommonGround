using CommonGround.Server.Blog;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string AdminRole = "Admin";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<CommonGround.Server.Blog.BlogHtmlSanitizer>();
builder.Services.AddOpenApi();

builder.AddSqlServerDbContext<AppDbContext>("commongroundDb");

builder.Services.Configure<GardenOptions>(builder.Configuration.GetSection(GardenOptions.SectionName));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<CommonGround.Server.Blog.BlogImport.WixBlogClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("CommonGround/1.0 (+blog-import)");
});
builder.Services.AddScoped<CommonGround.Server.Blog.BlogImport.BlogImporter>();

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

admin.MapGet("/me", async (
    System.Security.Claims.ClaimsPrincipal user,
    UserManager<ApplicationUser> userManager) =>
{
    var current = await userManager.GetUserAsync(user);
    return Results.Ok(new
    {
        email = user.Identity?.Name,
        displayName = current?.DisplayName,
        isAdmin = true,
    });
});

admin.MapPut("/me", async (
    UpdateProfileDto input,
    System.Security.Claims.ClaimsPrincipal user,
    UserManager<ApplicationUser> userManager) =>
{
    var current = await userManager.GetUserAsync(user);
    if (current is null) return Results.Unauthorized();

    var trimmed = input.DisplayName?.Trim();
    current.DisplayName = string.IsNullOrEmpty(trimmed) ? null : trimmed;

    var result = await userManager.UpdateAsync(current);
    if (!result.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

    return Results.Ok(new
    {
        email = user.Identity?.Name,
        displayName = current.DisplayName,
        isAdmin = true,
    });
});

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
            u.Id, u.Email, u.UserName, u.DisplayName, u.EmailConfirmed,
            rolesByUser.GetValueOrDefault(u.Id, [])))
        .ToList();

    return Results.Ok(members);
});

admin.MapAdminBlog();
admin.MapAdminTools();

app.MapPublicBlog();

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
    const string adminDisplayName = "Site Admin";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = adminDisplayName,
        };
        var result = await userManager.CreateAsync(admin, "Password123!");
        if (!result.Succeeded) return;
    }
    else if (string.IsNullOrWhiteSpace(admin.DisplayName))
    {
        admin.DisplayName = adminDisplayName;
        await userManager.UpdateAsync(admin);
    }

    if (!await userManager.IsInRoleAsync(admin, AdminRole))
        await userManager.AddToRoleAsync(admin, AdminRole);
}

internal record MemberDto(
    string Id, string? Email, string? UserName, string? DisplayName,
    bool EmailConfirmed, string[] Roles);

internal record UpdateProfileDto(string? DisplayName);
