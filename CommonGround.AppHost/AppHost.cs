using System.Text.RegularExpressions;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.OperationalInsights;
using Azure.Provisioning.Sql;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("cae");

var sql = builder.AddAzureSqlServer("sql")
	.RunAsContainer(c => c
		.WithContainerName("commonground-sql-shared")
		.WithHostPort(11433)
		.WithDataVolume("commonground-sql-shared-data")
		.WithLifetime(ContainerLifetime.Persistent));

var worktreeName = Path.GetFileName(Path.GetDirectoryName(builder.AppHostDirectory)) ?? "default";
var sqlDbName = builder.ExecutionContext.IsPublishMode ? "commongroundDb" : "commongroundDb_" + Regex.Replace(worktreeName, "[^A-Za-z0-9_]", "_");

// WithDefaultAzureSku opts out of Aspire's free-offer defaults (useFreeLimit: true),
// which Azure rejects once the database is on a paid tier ("Cannot update paid
// database to free database"). The actual SKU is codified below instead.
var commonGroundDb = sql.AddDatabase("commongroundDb", sqlDbName).WithDefaultAzureSku();

if (builder.ExecutionContext.IsPublishMode)
{
	sql.ConfigureInfrastructure(infra =>
	{
		var db = infra.GetProvisionableResources().OfType<SqlDatabase>().Single();
		db.Sku = new SqlSku
		{
			Name = "Basic",
			Tier = "Basic",
			Capacity = 5,
		};

		// Backup retention is NOT codified here (Azure.Provisioning.Sql 1.1.0 emits
		// invalid bicep for the retention-policy resources — it omits the required
		// `name`, failing `bicep build` with BCP035); it is set manually. Basic tier
		// caps the PITR window at 7 days. Longer coverage is via scheduled .bacpac
		// exports. See BACKUP.md (repo root).
	});
}

// Custom domain bindings are codified here so they survive redeploys. The managed
// certificates live on the Container App Environment (cae) and persist across app
// deployments; we reference them by name rather than reprovisioning. Supply the
// matching domain/certificate-name parameter pairs in the deploy environment; each
// pair present is bound, so the apex domain is optional.
var customDomains = new[]
{
	(Domain: "custom-domain", Certificate: "certificate-name"),
	(Domain: "apex-domain", Certificate: "apex-certificate-name"),
}
	.Where(p => !string.IsNullOrWhiteSpace(builder.Configuration["Parameters:" + p.Domain]))
	.Select(p => (Domain: builder.AddParameter(p.Domain), Certificate: builder.AddParameter(p.Certificate)))
	.ToList();

var server = builder.AddProject<Projects.CommonGround_Server>("server")
	.WithHttpHealthCheck("/health")
	.WithReference(commonGroundDb)
	.WaitFor(commonGroundDb)
	.WithExternalHttpEndpoints()
	.PublishAsAzureContainerApp((_, app) =>
	{
		app.Template.Scale.MinReplicas = 1;
		app.Template.Scale.MaxReplicas = 1;
		foreach (var c in app.Template.Containers)
		{
			c.Value!.Resources.Cpu = 0.25;
			c.Value!.Resources.Memory = "0.5Gi";
		}

#pragma warning disable ASPIREACADOMAINS001
		foreach (var (domain, certificate) in customDomains)
		{
			app.ConfigureCustomDomain(domain, certificate);
		}
#pragma warning restore ASPIREACADOMAINS001
	});

if (builder.ExecutionContext.IsPublishMode)
{
	var appInsights = builder.AddAzureApplicationInsights("appinsights")
		.ConfigureInfrastructure(infra =>
		{
			foreach (var workspace in infra.GetProvisionableResources().OfType<OperationalInsightsWorkspace>())
			{
				workspace.RetentionInDays = 30;
				workspace.WorkspaceCapping = new OperationalInsightsWorkspaceCapping
				{
					DailyQuotaInGB = 0.1,
				};
			}
		});
	server.WithReference(appInsights);
}
else if (!string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:appinsights"]))
{
	var appInsights = builder.AddConnectionString("appinsights");
	server.WithReference(appInsights);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:resend-api-key"]))
{
	var resendApiKey = builder.AddParameter("resend-api-key", secret: true);
	server.WithEnvironment("Email__ApiToken", resendApiKey);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:resend-from-address"]))
{
	var resendFromAddress = builder.AddParameter("resend-from-address", secret: false);
	server.WithEnvironment("Email__FromAddress", resendFromAddress);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:resend-from-name"]))
{
	var resendFromName = builder.AddParameter("resend-from-name", secret: false);
	server.WithEnvironment("Email__FromName", resendFromName);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:resend-template-id"]))
{
	var resendTemplateId = builder.AddParameter("resend-template-id", secret: true);
	server.WithEnvironment("Email__TemplateId", resendTemplateId);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:resend-transactional-template-id"]))
{
	var resendTransactionalTemplateId = builder.AddParameter("resend-transactional-template-id", secret: true);
	server.WithEnvironment("Email__TransactionalTemplateId", resendTransactionalTemplateId);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:eventbrite-private-token"]))
{
	var eventbriteToken = builder.AddParameter("eventbrite-private-token", secret: true);
	server.WithEnvironment("Eventbrite__PrivateToken", eventbriteToken);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:eventbrite-organization-id"]))
{
	var eventbriteOrganizationId = builder.AddParameter("eventbrite-organization-id");
	server.WithEnvironment("Eventbrite__OrganizationId", eventbriteOrganizationId);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:contactform-recipient-address"]))
{
	var contactRecipientAddress = builder.AddParameter("contactform-recipient-address", secret: true);
	server.WithEnvironment("ContactForm__RecipientAddress", contactRecipientAddress);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:turnstile-site-key"]))
{
	var turnstileSiteKey = builder.AddParameter("turnstile-site-key");
	server.WithEnvironment("ContactForm__TurnstileSiteKey", turnstileSiteKey);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:turnstile-secret-key"]))
{
	var turnstileSecretKey = builder.AddParameter("turnstile-secret-key", secret: true);
	server.WithEnvironment("ContactForm__TurnstileSecretKey", turnstileSecretKey);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:garden-name"]))
{
	var gardenName = builder.AddParameter("garden-name");
	server.WithEnvironment("Garden__Name", gardenName);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:garden-public-url"]))
{
	var gardenPublicUrl = builder.AddParameter("garden-public-url");
	server.WithEnvironment("Garden__PublicUrl", gardenPublicUrl);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:wix-site-root"]))
{
	var wixSiteRoot = builder.AddParameter("wix-site-root");
	server.WithEnvironment("WixSiteRoot", wixSiteRoot);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:stripe-secret-key"]))
{
	var stripeSecretKey = builder.AddParameter("stripe-secret-key", secret: true);
	server.WithEnvironment("Stripe__SecretKey", stripeSecretKey);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:stripe-webhook-secret"]))
{
	var stripeWebhookSecret = builder.AddParameter("stripe-webhook-secret", secret: true);
	server.WithEnvironment("Stripe__WebhookSecret", stripeWebhookSecret);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:admin-notification-email"]))
{
	var adminNotificationEmail = builder.AddParameter("admin-notification-email", secret: true);
	server.WithEnvironment("LeasedBeds__AdminNotificationEmail", adminNotificationEmail);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:app-version"]))
{
	var appVersion = builder.AddParameter("app-version");
	server.WithEnvironment("BuildInfo__Version", appVersion);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:commit-sha"]))
{
	var commitSha = builder.AddParameter("commit-sha");
	server.WithEnvironment("BuildInfo__CommitSha", commitSha);
}

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
	.WithReference(server)
	.WaitFor(server);

if (builder.ExecutionContext.IsRunMode)
{
	// Pin the Vite dev server to a stable port. Aspire otherwise assigns a
	// random port each run, which (combined with host: true in vite.config.ts)
	// makes the LAN preview URL change every time and breaks any firewall rule.
	webfrontend.WithEndpoint("http", endpoint => endpoint.TargetPort = 5173, createIfNotExists: false);
}

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
