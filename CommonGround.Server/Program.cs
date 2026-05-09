using CommonGround.Server.Auth;
using CommonGround.Server.Blog;
using CommonGround.Server.Blog.BlogImport;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resend;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BlogHtmlSanitizer>();
builder.Services.AddOpenApi();
builder.Services.AddFastEndpoints();

builder.AddSqlServerDbContext<AppDbContext>("commongroundDb");

builder.Services.Configure<GardenOptions>(builder.Configuration.GetSection(GardenOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o => o.ApiToken = builder.Configuration[$"{EmailOptions.SectionName}:ApiToken"] ?? "");
builder.Services.AddTransient<IResend, ResendClient>();

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
    await DevSeed.SeedAdminAsync(sp);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
});

app.MapGroup("/api/auth").MapIdentityApi<ApplicationUser>();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
