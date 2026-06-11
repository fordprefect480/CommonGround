using System.Security.Claims;
using CommonGround.Server.Activity;
using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Admin;

public sealed class CreateBlogPostEndpoint(
    AppDbContext db,
    BlogHtmlSanitizer sanitizer,
    UserManager<ApplicationUser> userManager,
    IActivityLogger activityLogger)
    : Endpoint<BlogPostWriteDto, BlogPostAdminDto>
{
    public override void Configure()
    {
        Post("/posts");
        Group<AdminBlogGroup>();
    }

    public override async Task HandleAsync(BlogPostWriteDto req, CancellationToken ct)
    {
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

        var baseSlug = BlogSlug.Derive(
            string.IsNullOrWhiteSpace(req.Slug) ? req.Title : req.Slug);
        if (string.IsNullOrEmpty(baseSlug))
        {
            await SendBadRequest("Could not derive a slug from the title.");
            return;
        }

        var resolvedSlug = await BlogSlug.ResolveAsync(
            baseSlug,
            (candidate, token) => db.BlogPosts.AnyAsync(p => p.Slug == candidate, token),
            ct);

        var currentUser = await userManager.GetUserAsync(User);
        var authorName = currentUser?.DisplayName
            ?? currentUser?.UserName
            ?? User.Identity?.Name
            ?? "Anonymous";

        var status = (BlogPostStatus)req.Status;
        var now = DateTime.UtcNow;

        var post = new BlogPost
        {
            Slug = resolvedSlug,
            Title = req.Title.Trim(),
            Excerpt = BlogPostMapping.NullIfBlank(req.Excerpt),
            BodyHtml = sanitizer.Sanitize(req.BodyHtml ?? ""),
            AuthorName = authorName,
            CategoryId = req.CategoryId,
            FeaturedImageId = req.FeaturedImageId,
            Status = status,
            PublishedAt = status == BlogPostStatus.Published ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = currentUser?.Id ?? User.FindFirstValue(ClaimTypes.NameIdentifier),
        };

        db.BlogPosts.Add(post);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (BlogPostMapping.IsUniqueSlugViolation(ex))
        {
            await Send.ResultAsync(Results.Conflict(new { error = $"Slug '{resolvedSlug}' is already in use." }));
            return;
        }

        await activityLogger.LogAsync(
            "blog.post_created",
            $"Created blog post '{post.Title}' ({post.Status})",
            targetType: "BlogPost",
            targetId: post.Id.ToString(),
            ct: ct);

        await Send.ResultAsync(Results.Created(
            $"/api/admin/blog/posts/{post.Id}",
            BlogPostMapping.ToAdminDto(post)));
    }

    private Task SendBadRequest(string error) =>
        Send.ResultAsync(Results.BadRequest(new { error }));
}
