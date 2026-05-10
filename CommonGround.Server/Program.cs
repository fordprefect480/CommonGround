using CommonGround.Server.Auth;
using CommonGround.Server.Blog;
using CommonGround.Server.Blog.BlogImport;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
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
    c.Timeout = TimeSpan.FromMinutes(2);
    c.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
    c.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-AU,en;q=0.9");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.All,
})
.AddResilienceHandler("wix-retry", b =>
{
    b.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(1),
        MaxDelay = TimeSpan.FromSeconds(30),
        UseJitter = true,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(r =>
                r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                (int)r.StatusCode >= 500),
    });
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
