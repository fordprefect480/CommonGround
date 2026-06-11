using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Admin;

public sealed class UpdateBlogPostEndpoint(
    AppDbContext db,
    BlogHtmlSanitizer sanitizer,
    IActivityLogger activityLogger)
    : Endpoint<UpdateBlogPostEndpoint.Request, BlogPostAdminDto>
{
    public sealed class Request
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Slug { get; set; }
        public string? Excerpt { get; set; }
        public string BodyHtml { get; set; } = "";
        public int? CategoryId { get; set; }
        public int? FeaturedImageId { get; set; }
        public int Status { get; set; }
    }

    public override void Configure()
    {
        Put("/posts/{id:int}");
        Group<AdminBlogGroup>();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == req.Id, ct);
        if (post is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Title))
        {
            await SendBadRequest("Title is required.");
            return;
        }

        if (!Enum.IsDefined((BlogPostStatus)req.Status))
        {
            await SendBadRequest("Invalid status.");
            return;
        }

        var requestedSlug = string.IsNullOrWhiteSpace(req.Slug) ? post.Slug : BlogSlug.Derive(req.Slug);
        if (string.IsNullOrEmpty(requestedSlug))
        {
            await SendBadRequest("Slug cannot be empty.");
            return;
        }

        if (requestedSlug != post.Slug && await db.BlogPosts.AnyAsync(p => p.Slug == requestedSlug, ct))
        {
            await Send.ResultAsync(Results.Conflict(new { error = $"Slug '{requestedSlug}' is already in use." }));
            return;
        }

        var status = (BlogPostStatus)req.Status;
        var becomingPublished = status == BlogPostStatus.Published && post.Status != BlogPostStatus.Published;

        post.Slug = requestedSlug;
        post.Title = req.Title.Trim();
        post.Excerpt = BlogPostMapping.NullIfBlank(req.Excerpt);
        post.BodyHtml = sanitizer.Sanitize(req.BodyHtml ?? "");
        post.CategoryId = req.CategoryId;
        post.FeaturedImageId = req.FeaturedImageId;
        post.Status = status;
        post.UpdatedAt = DateTime.UtcNow;
        if (becomingPublished && post.PublishedAt is null)
            post.PublishedAt = post.UpdatedAt;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (BlogPostMapping.IsUniqueSlugViolation(ex))
        {
            await Send.ResultAsync(Results.Conflict(new { error = $"Slug '{requestedSlug}' is already in use." }));
            return;
        }

        await activityLogger.LogAsync(
            "blog.post_updated",
            $"Updated blog post '{post.Title}' ({post.Status})",
            targetType: "BlogPost",
            targetId: post.Id.ToString(),
            ct: ct);

        await Send.OkAsync(BlogPostMapping.ToAdminDto(post), ct);
    }

    private Task SendBadRequest(string error) =>
        Send.ResultAsync(Results.BadRequest(new { error }));
}
