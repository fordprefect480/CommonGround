using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Blog;
using CommonGround.Server.Blog.BlogImport;
using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using CommonGround.Server.Events;
using CommonGround.Server.Misc;
using FastEndpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using Resend;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<BlogHtmlSanitizer>();
builder.Services.AddSingleton<CommonGround.Server.Blog.BlogImport.BlogImportHtmlNormalizer>();
builder.Services.AddOpenApi();
// Restrict endpoint discovery to this assembly. All endpoints live here, and
// scanning the full dependency closure (which happens under the test host) trips
// over the AngleSharp/AngleSharp.Css version mismatch dragged in by HtmlSanitizer.
builder.Services.AddFastEndpoints(cfg =>
{
    cfg.Assemblies = new[] { typeof(Program).Assembly };
    cfg.DisableAutoDiscovery = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
builder.Services.AddScoped<CommonGround.Server.Members.MembershipActivationService>();
builder.Services.AddScoped<CommonGround.Server.Members.MembershipCheckoutService>();
builder.Services.AddScoped<SiteSettingsService>();
builder.Services.AddScoped<CommonGround.Server.LeasedBeds.LeasedBedService>();
builder.Services.AddScoped<CommonGround.Server.LeasedBeds.LeasedBedCheckoutService>();
builder.Services.AddScoped<CommonGround.Server.LeasedBeds.LeasedBedActivationService>();
builder.Services.AddScoped<CommonGround.Server.LeasedBeds.LeasedBedNotifications>();
builder.Services.AddScoped<TransactionalEmailSender>();
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()
    .SetApplicationName("CommonGround");
builder.Services.AddSingleton<UnsubscribeTokenService>();

builder.AddSqlServerDbContext<AppDbContext>("commongroundDb", settings =>
{
    // The Azure SQL database is serverless with auto-pause, so the first request
    // after an idle period must wait out the compute resume, which routinely
    // exceeds the 15s default connection timeout and 500s the caller. Widen the
    // connection timeout so a single attempt outlasts the resume; Aspire keeps
    // EF's transient-error retries on by default as a backstop.
    settings.ConnectionString = new SqlConnectionStringBuilder(settings.ConnectionString)
    {
        ConnectTimeout = 60,
    }.ConnectionString;
    settings.CommandTimeout = 60;
}, optionsBuilder =>
{
    // ApplicationUser carries a soft-delete query filter while its dependents
    // (payments, leases, requests) keep required FKs to it. That pairing is
    // deliberate, so silence EF's advisory warning about it.
    optionsBuilder.ConfigureWarnings(w =>
        w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
});

builder.Services.Configure<GardenOptions>(builder.Configuration.GetSection(GardenOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection(ContactOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));
builder.Services.Configure<EventbriteOptions>(builder.Configuration.GetSection(EventbriteOptions.SectionName));
builder.Services.Configure<LeasedBedsOptions>(builder.Configuration.GetSection(LeasedBedsOptions.SectionName));
builder.Services.AddHttpClient<TurnstileVerifier>();
builder.Services.AddHttpClient<EventbriteClient>(c => c.Timeout = TimeSpan.FromSeconds(10));

builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(o => o.ApiToken = builder.Configuration[$"{EmailOptions.SectionName}:ApiToken"] ?? "");

// Guard against mailing real members from a developer machine: when Email:RedirectAllTo
// is set in Development, every outgoing email is rerouted to that address. IsDevelopment()
// is an additional backstop so this can never engage in production.
var redirectAllEmailTo = builder.Configuration[$"{EmailOptions.SectionName}:RedirectAllTo"];
if (builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(redirectAllEmailTo))
{
    builder.Services.AddTransient<IResend>(sp => RedirectingResendProxy.Wrap(
        sp.GetRequiredService<ResendClient>(),
        redirectAllEmailTo,
        sp.GetRequiredService<ILogger<RedirectingResendProxy>>()));
}
else
{
    builder.Services.AddTransient<IResend, ResendClient>();
}

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Override Identity's no-op email sender so the /forgotPassword flow emails a
// real reset link via Resend. Registered after AddIdentityApiEndpoints so this
// wins over the default registration.
builder.Services.AddTransient<IEmailSender<ApplicationUser>, IdentityEmailSender>();

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

var stripeSecretKey = app.Configuration[$"{StripeOptions.SectionName}:SecretKey"];
if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

// Integration tests boot the app against an in-memory provider that can't run
// the SQL Server migrations, and seed their own data, so the startup
// migrate/seed/warmup is skipped under the "Testing" environment.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DevSeed.SeedAdminAsync(sp);
    }

    var protection = sp.GetRequiredService<IDataProtectionProvider>();
    try
    {
        protection.CreateProtector("startup-warmup").Protect("");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Data protection warmup failed; keys will be created on first use.");
    }
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

app.MapUnsubscribeEndpoint();

app.MapSitemapEndpoint();

app.MapDefaultEndpoints();

app.UseFileServer();

// Serve the SPA shell for client-side routes (e.g. /membership/welcome from the
// Stripe redirect). API routes are under /api and static assets are served above,
// so anything else is a React Router path that needs index.html.
app.MapFallbackToFile("index.html");

app.Run();
