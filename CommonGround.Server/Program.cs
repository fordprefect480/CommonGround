using System.Security.Claims;
using ClosedXML.Excel;
using CommonGround.Server.Blog;
using CommonGround.Server.Blog.BlogImport;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

const string AdminRole = "Admin";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BlogHtmlSanitizer>();
builder.Services.AddOpenApi();

builder.AddSqlServerDbContext<AppDbContext>("commongroundDb");

builder.Services.Configure<GardenOptions>(builder.Configuration.GetSection(GardenOptions.SectionName));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddHttpClient<WixBlogClient>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("CommonGround/1.0 (+blog-import)");
});
builder.Services.AddScoped<BlogImporter>();

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

var account = app.MapGroup("/api/account").RequireAuthorization();

account.MapGet("/me", async (
    ClaimsPrincipal user,
    UserManager<ApplicationUser> userManager) =>
{
    var current = await userManager.GetUserAsync(user);
    if (current is null) return Results.Unauthorized();

    var isAdmin = await userManager.IsInRoleAsync(current, AdminRole);
    return Results.Ok(new
    {
        email = current.Email,
        firstName = current.FirstName,
        lastName = current.LastName,
        displayName = current.DisplayName,
        isAdmin,
    });
});

account.MapPut("/me", async (
    UpdateProfileDto input,
    ClaimsPrincipal user,
    UserManager<ApplicationUser> userManager) =>
{
    var current = await userManager.GetUserAsync(user);
    if (current is null) return Results.Unauthorized();

    current.FirstName = NullIfBlank(input.FirstName);
    current.LastName = NullIfBlank(input.LastName);

    var result = await userManager.UpdateAsync(current);
    if (!result.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

    var isAdmin = await userManager.IsInRoleAsync(current, AdminRole);
    return Results.Ok(new
    {
        email = current.Email,
        firstName = current.FirstName,
        lastName = current.LastName,
        displayName = current.DisplayName,
        isAdmin,
    });
});

var admin = app.MapGroup("/api/admin")
    .RequireAuthorization(p => p.RequireRole(AdminRole));

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
            u.Id, u.Email, u.UserName, u.FirstName, u.LastName, u.DisplayName,
            u.PhoneNumber, u.JoinedAt, u.EmailConfirmed,
            rolesByUser.GetValueOrDefault(u.Id, [])))
        .ToList();

    return Results.Ok(members);
});

admin.MapGet("/members/export.xlsx", async (AppDbContext db) =>
{
    var users = await db.Users.OrderBy(u => u.Email).ToListAsync();

    var rolePairs = await (
        from ur in db.UserRoles
        join r in db.Roles on ur.RoleId equals r.Id
        select new { ur.UserId, RoleName = r.Name! })
        .ToListAsync();

    var rolesByUser = rolePairs
        .GroupBy(x => x.UserId)
        .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.RoleName).Order()));

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("Members");

    string[] headers = ["First name", "Last name", "Email", "Username", "Phone", "Member since", "Roles", "Email confirmed"];
    for (var c = 0; c < headers.Length; c++)
        ws.Cell(1, c + 1).Value = headers[c];
    ws.Row(1).Style.Font.Bold = true;

    for (var i = 0; i < users.Count; i++)
    {
        var u = users[i];
        var row = i + 2;
        ws.Cell(row, 1).Value = u.FirstName;
        ws.Cell(row, 2).Value = u.LastName;
        ws.Cell(row, 3).Value = u.Email;
        ws.Cell(row, 4).Value = u.UserName;
        ws.Cell(row, 5).Value = u.PhoneNumber;
        ws.Cell(row, 6).Value = u.JoinedAt;
        ws.Cell(row, 6).Style.DateFormat.Format = "yyyy-mm-dd";
        ws.Cell(row, 7).Value = rolesByUser.GetValueOrDefault(u.Id, "");
        ws.Cell(row, 8).Value = u.EmailConfirmed ? "Yes" : "No";
    }

    ws.Columns().AdjustToContents();

    var stream = new MemoryStream();
    wb.SaveAs(stream);
    stream.Position = 0;

    var fileName = $"members-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
    return Results.File(
        stream,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
});

admin.MapGet("/members/{id}", async (
    string id,
    AppDbContext db,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    if (user is null) return Results.NotFound();

    var roles = await userManager.GetRolesAsync(user);

    return Results.Ok(new MemberDto(
        user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
        user.PhoneNumber, user.JoinedAt, user.EmailConfirmed,
        roles.ToArray()));
});

admin.MapPut("/members/{id}", async (
    string id,
    UpdateMemberDto input,
    UserManager<ApplicationUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(id);
    if (user is null) return Results.NotFound();

    user.FirstName = NullIfBlank(input.FirstName);
    user.LastName = NullIfBlank(input.LastName);
    user.PhoneNumber = NullIfBlank(input.PhoneNumber);

    var updateResult = await userManager.UpdateAsync(user);
    if (!updateResult.Succeeded)
        return Results.BadRequest(new { error = string.Join("; ", updateResult.Errors.Select(e => e.Description)) });

    var isAdmin = await userManager.IsInRoleAsync(user, AdminRole);
    if (input.IsAdmin && !isAdmin)
    {
        var addResult = await userManager.AddToRoleAsync(user, AdminRole);
        if (!addResult.Succeeded)
            return Results.BadRequest(new { error = string.Join("; ", addResult.Errors.Select(e => e.Description)) });
    }
    else if (!input.IsAdmin && isAdmin)
    {
        var removeResult = await userManager.RemoveFromRoleAsync(user, AdminRole);
        if (!removeResult.Succeeded)
            return Results.BadRequest(new { error = string.Join("; ", removeResult.Errors.Select(e => e.Description)) });
    }

    var roles = await userManager.GetRolesAsync(user);

    return Results.Ok(new MemberDto(
        user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
        user.PhoneNumber, user.JoinedAt, user.EmailConfirmed,
        roles.ToArray()));
});

admin.MapPost("/members", async (
    CreateMemberDto input,
    UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(input.Email))
        return Results.BadRequest(new { error = "Email is required." });
    if (string.IsNullOrWhiteSpace(input.Password))
        return Results.BadRequest(new { error = "Password is required." });

    var email = input.Email.Trim();

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        EmailConfirmed = true,
        FirstName = NullIfBlank(input.FirstName),
        LastName = NullIfBlank(input.LastName),
        PhoneNumber = NullIfBlank(input.PhoneNumber),
    };

    var createResult = await userManager.CreateAsync(user, input.Password);
    if (!createResult.Succeeded)
        return Results.BadRequest(new
        {
            error = string.Join("; ", createResult.Errors.Select(e => e.Description)),
        });

    var roles = new List<string>();
    if (input.IsAdmin)
    {
        var roleResult = await userManager.AddToRoleAsync(user, AdminRole);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Results.BadRequest(new
            {
                error = string.Join("; ", roleResult.Errors.Select(e => e.Description)),
            });
        }
        roles.Add(AdminRole);
    }

    return Results.Created(
        $"/api/admin/members/{user.Id}",
        new MemberDto(user.Id, user.Email, user.UserName, user.FirstName, user.LastName, user.DisplayName,
            user.PhoneNumber, user.JoinedAt, user.EmailConfirmed, roles.ToArray()));
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
    const string adminFirstName = "Site";
    const string adminLastName = "Admin";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = adminFirstName,
            LastName = adminLastName,
        };
        var result = await userManager.CreateAsync(admin, "Password123!");
        if (!result.Succeeded) return;
    }
    else if (string.IsNullOrWhiteSpace(admin.FirstName) && string.IsNullOrWhiteSpace(admin.LastName))
    {
        admin.FirstName = adminFirstName;
        admin.LastName = adminLastName;
        await userManager.UpdateAsync(admin);
    }

    if (!await userManager.IsInRoleAsync(admin, AdminRole))
        await userManager.AddToRoleAsync(admin, AdminRole);
}

static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

internal record MemberDto(
    string Id, string? Email, string? UserName,
    string? FirstName, string? LastName, string? DisplayName,
    string? PhoneNumber, DateTime JoinedAt,
    bool EmailConfirmed, string[] Roles);

internal record UpdateProfileDto(string? FirstName, string? LastName);

internal record CreateMemberDto(string Email, string? FirstName, string? LastName, string? PhoneNumber, string Password, bool IsAdmin);

internal record UpdateMemberDto(string? FirstName, string? LastName, string? PhoneNumber, bool IsAdmin);
