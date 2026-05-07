using CommonGround.Server.Auth;
using CommonGround.Server.Blog.BlogImport;
using FastEndpoints;

namespace CommonGround.Server.Blog.AdminTools;

public sealed class ImportBlogEndpoint(BlogImporter importer)
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
        await Send.OkAsync(result, ct);
    }
}
