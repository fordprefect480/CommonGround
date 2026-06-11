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

var commonGroundDb = sql.AddDatabase("commongroundDb", sqlDbName);

if (builder.ExecutionContext.IsPublishMode)
{
	sql.ConfigureInfrastructure(infra =>
	{
		var db = infra.GetProvisionableResources().OfType<SqlDatabase>().Single();
		db.Sku = new SqlSku
		{
			Name = "GP_S_Gen5_1",
			Tier = "GeneralPurpose",
			Family = "Gen5",
			Capacity = 1,
		};
		db.MinCapacity = 0.5;
		db.AutoPauseDelay = 60;
	});
}

var server = builder.AddProject<Projects.CommonGround_Server>("server")
	.WithHttpHealthCheck("/health")
	.WithReference(commonGroundDb)
	.WaitFor(commonGroundDb)
	.WithExternalHttpEndpoints()
	.PublishAsAzureContainerApp((_, app) =>
	{
		app.Template.Scale.MinReplicas = 0;
		app.Template.Scale.MaxReplicas = 1;
		foreach (var c in app.Template.Containers)
		{
			c.Value!.Resources.Cpu = 0.25;
			c.Value!.Resources.Memory = "0.5Gi";
		}
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

if (!string.IsNullOrWhiteSpace(builder.Configuration["Parameters:eventbrite-private-token"]))
{
	var eventbriteToken = builder.AddParameter("eventbrite-private-token", secret: true);
	server.WithEnvironment("Eventbrite__PrivateToken", eventbriteToken);
}

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
	.WithReference(server)
	.WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
