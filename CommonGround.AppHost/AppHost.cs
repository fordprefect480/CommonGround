var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
	.WithDataVolume()
	.WithLifetime(ContainerLifetime.Persistent);

var commonGroundDb = sql.AddDatabase("commongroundDb");

var server = builder.AddProject<Projects.CommonGround_Server>("server")
	.WithHttpHealthCheck("/health")
	.WithReference(commonGroundDb)
	.WaitFor(commonGroundDb)
	.WithExternalHttpEndpoints();

if (!string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:appinsights"]))
{
	var appInsights = builder.AddConnectionString("appinsights");
	server.WithReference(appInsights);
}

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
	.WithReference(server)
	.WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
