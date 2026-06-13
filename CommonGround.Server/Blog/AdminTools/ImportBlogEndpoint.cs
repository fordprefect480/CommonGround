using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Blog.BlogImport;
using FastEndpoints;

namespace CommonGround.Server.Blog.AdminTools;

public sealed class ImportBlogEndpoint(BlogImporter importer, IActivityLogger activityLogger)
    : Endpoint<ImportBlogEndpoint.Request, ImportBlogResultDto>
{
    public sealed class Request
    {
        public int? Limit { get; set; }
    }

    public override void Configure()
    {
        Post("/import-blog");
        Group<AdminToolsGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var result = await importer.ImportAsync(req.Limit, ct);

        await activityLogger.LogAsync(
            "tool.blog_import_run",
            $"ran a blog import - {result.Imported} posts imported, {result.Skipped} skipped, {result.Failed} failed",
            details: new { result.Imported, result.Skipped, result.Failed, Limit = req.Limit },
            ct: ct);

        await Send.OkAsync(result, ct);
    }
}
