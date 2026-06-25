using System.Threading.Channels;
using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Blog.BlogImport;
using FastEndpoints;

namespace CommonGround.Server.Blog.AdminTools;

public sealed class ImportBlogEndpoint(BlogImporter importer, IActivityLogger activityLogger)
    : Endpoint<ImportBlogEndpoint.Request>
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
        var channel = Channel.CreateUnbounded<StreamItem>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        // Run the importer concurrently with the SSE stream: it pushes progress into the
        // channel while Send.EventStreamAsync pulls items out and writes them to the client.
        var importTask = RunImportAsync(channel.Writer, req.Limit, ct);
        await Send.EventStreamAsync(channel.Reader.ReadAllAsync(ct), ct);
        await importTask;
    }

    private async Task RunImportAsync(ChannelWriter<StreamItem> writer, int? limit, CancellationToken ct)
    {
        try
        {
            var result = await importer.ImportAsync(
                limit,
                progress => writer.TryWrite(new StreamItem("progress", progress)),
                ct);

            writer.TryWrite(new StreamItem("done", result));

            // The result has already been streamed to the client, so a logging failure must not
            // fault this task (it would surface as an unhandled error after the response is sent).
            try
            {
                await activityLogger.LogAsync(
                    "tool.blog_import_run",
                    $"ran a blog import - {result.Imported} posts imported, {result.Skipped} skipped, {result.Failed} failed",
                    details: new { result.Imported, result.Skipped, result.Failed, Limit = limit },
                    ct: ct);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Blog import completed but writing the activity log entry failed.");
            }
        }
        finally
        {
            writer.Complete();
        }
    }
}
