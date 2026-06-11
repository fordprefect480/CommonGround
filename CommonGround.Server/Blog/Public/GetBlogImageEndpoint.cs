using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace CommonGround.Server.Blog.Public;

public sealed class GetBlogImageEndpoint(AppDbContext db) : Endpoint<GetBlogImageEndpoint.Request>
{
    public sealed class Request
    {
        public int Id { get; set; }
    }

    public override void Configure()
    {
        Get("/images/{id:int}");
        Group<PublicBlogGroup>();
        Description(b => b.WithMetadata(new ResponseCacheAttribute
        {
            Duration = 31536000,
            Location = ResponseCacheLocation.Any,
            NoStore = false,
        }));
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var image = await db.BlogImages
            .AsNoTracking()
            .Where(i => i.Id == req.Id)
            .Select(i => new { i.ContentType, i.Bytes })
            .FirstOrDefaultAsync(ct);

        if (image is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResultAsync(Results.File(
            image.Bytes,
            contentType: image.ContentType,
            fileDownloadName: null,
            lastModified: null,
            entityTag: new EntityTagHeaderValue($"\"{req.Id}\"")));
    }
}
