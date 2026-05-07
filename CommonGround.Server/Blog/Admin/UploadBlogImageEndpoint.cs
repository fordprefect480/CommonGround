using System.Security.Claims;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;

namespace CommonGround.Server.Blog.Admin;

public sealed class UploadBlogImageEndpoint(AppDbContext db)
    : EndpointWithoutRequest<BlogImageUploadDto>
{
    public override void Configure()
    {
        Post("/images");
        Group<AdminBlogGroup>();
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var file = Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            await SendBadRequest("No file was uploaded.");
            return;
        }

        if (file.Length > BlogImageContentType.MaxBytes)
        {
            await SendBadRequest("File exceeds 10 MB limit.");
            return;
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);

        var detected = BlogImageContentType.DetectFromBytes(stream.GetBuffer().AsSpan(0, (int)stream.Length));
        if (detected is null)
        {
            await SendBadRequest("File contents do not match a supported image format.");
            return;
        }

        var image = new BlogImage
        {
            ContentType = detected,
            Bytes = stream.ToArray(),
            OriginalFileName = file.FileName,
            CreatedAt = DateTime.UtcNow,
            UploadedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        };

        db.BlogImages.Add(image);
        await db.SaveChangesAsync(ct);

        await Send.OkAsync(new BlogImageUploadDto(image.Id, $"/api/blog/images/{image.Id}"), ct);
    }

    private Task SendBadRequest(string error) =>
        Send.ResultAsync(Results.BadRequest(new { error }));
}
