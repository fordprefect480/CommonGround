using System.Security.Claims;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog;

public static class AdminBlogEndpoints
{
    private const int SqlUniqueConstraintError = 2627;
    private const int SqlUniqueIndexError = 2601;

    public static RouteGroupBuilder MapAdminBlog(this RouteGroupBuilder admin)
    {
        var blog = admin.MapGroup("/blog");

        blog.MapGet("/posts", async (AppDbContext db, CancellationToken ct) =>
        {
            var posts = await db.BlogPosts
                .AsNoTracking()
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => new BlogPostAdminListItemDto(
                    p.Id, p.Slug, p.Title, p.AuthorName,
                    p.CategoryId, p.FeaturedImageId, (int)p.Status,
                    p.PublishedAt, p.CreatedAt, p.UpdatedAt))
                .ToListAsync(ct);
            return Results.Ok(posts);
        });

        blog.MapGet("/posts/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var post = await db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return post is null ? Results.NotFound() : Results.Ok(ToAdminDto(post));
        });

        blog.MapPost("/posts", async (
            BlogPostWriteDto input,
            AppDbContext db,
            BlogHtmlSanitizer sanitizer,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest(new { error = "Title is required." });

            if (!Enum.IsDefined((BlogPostStatus)input.Status))
                return Results.BadRequest(new { error = "Invalid status." });

            var baseSlug = BlogSlug.Derive(
                string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug);
            if (string.IsNullOrEmpty(baseSlug))
                return Results.BadRequest(new { error = "Could not derive a slug from the title." });

            var resolvedSlug = await BlogSlug.ResolveAsync(
                baseSlug,
                (candidate, token) => db.BlogPosts.AnyAsync(p => p.Slug == candidate, token),
                ct);

            var currentUser = await userManager.GetUserAsync(user);
            var authorName = currentUser?.DisplayName
                ?? currentUser?.UserName
                ?? user.Identity?.Name
                ?? "Anonymous";

            var status = (BlogPostStatus)input.Status;
            var now = DateTime.UtcNow;

            var post = new BlogPost
            {
                Slug = resolvedSlug,
                Title = input.Title.Trim(),
                Excerpt = NullIfBlank(input.Excerpt),
                BodyHtml = sanitizer.Sanitize(input.BodyHtml ?? ""),
                AuthorName = authorName,
                CategoryId = input.CategoryId,
                FeaturedImageId = input.FeaturedImageId,
                Status = status,
                PublishedAt = status == BlogPostStatus.Published ? now : null,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = currentUser?.Id ?? user.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            db.BlogPosts.Add(post);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueSlugViolation(ex))
            {
                return Results.Conflict(new { error = $"Slug '{resolvedSlug}' is already in use." });
            }

            return Results.Created($"/api/admin/blog/posts/{post.Id}", ToAdminDto(post));
        });

        blog.MapPut("/posts/{id:int}", async (
            int id,
            BlogPostWriteDto input,
            AppDbContext db,
            BlogHtmlSanitizer sanitizer,
            CancellationToken ct) =>
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (post is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest(new { error = "Title is required." });

            if (!Enum.IsDefined((BlogPostStatus)input.Status))
                return Results.BadRequest(new { error = "Invalid status." });

            var requestedSlug = string.IsNullOrWhiteSpace(input.Slug) ? post.Slug : BlogSlug.Derive(input.Slug);
            if (string.IsNullOrEmpty(requestedSlug))
                return Results.BadRequest(new { error = "Slug cannot be empty." });

            if (requestedSlug != post.Slug && await db.BlogPosts.AnyAsync(p => p.Slug == requestedSlug, ct))
                return Results.Conflict(new { error = $"Slug '{requestedSlug}' is already in use." });

            var status = (BlogPostStatus)input.Status;
            var becomingPublished = status == BlogPostStatus.Published && post.Status != BlogPostStatus.Published;

            post.Slug = requestedSlug;
            post.Title = input.Title.Trim();
            post.Excerpt = NullIfBlank(input.Excerpt);
            post.BodyHtml = sanitizer.Sanitize(input.BodyHtml ?? "");
            post.CategoryId = input.CategoryId;
            post.FeaturedImageId = input.FeaturedImageId;
            post.Status = status;
            post.UpdatedAt = DateTime.UtcNow;
            if (becomingPublished && post.PublishedAt is null)
                post.PublishedAt = post.UpdatedAt;

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueSlugViolation(ex))
            {
                return Results.Conflict(new { error = $"Slug '{requestedSlug}' is already in use." });
            }

            return Results.Ok(ToAdminDto(post));
        });

        blog.MapDelete("/posts/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (post is null) return Results.NotFound();

            db.BlogPosts.Remove(post);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        blog.MapPost("/images", async (
            HttpRequest request,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart form data." });

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file was uploaded." });

            if (file.Length > BlogImageContentType.MaxBytes)
                return Results.BadRequest(new { error = "File exceeds 10 MB limit." });

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);

            var detected = BlogImageContentType.DetectFromBytes(stream.GetBuffer().AsSpan(0, (int)stream.Length));
            if (detected is null)
                return Results.BadRequest(new { error = "File contents do not match a supported image format." });

            var image = new BlogImage
            {
                ContentType = detected,
                Bytes = stream.ToArray(),
                OriginalFileName = file.FileName,
                CreatedAt = DateTime.UtcNow,
                UploadedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            db.BlogImages.Add(image);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new BlogImageUploadDto(image.Id, $"/api/blog/images/{image.Id}"));
        })
        .DisableAntiforgery();

        return admin;
    }

    private static BlogPostAdminDto ToAdminDto(BlogPost p) => new(
        p.Id, p.Slug, p.Title, p.Excerpt, p.BodyHtml, p.AuthorName,
        p.CategoryId, p.FeaturedImageId, (int)p.Status,
        p.PublishedAt, p.CreatedAt, p.UpdatedAt);

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsUniqueSlugViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql &&
        (sql.Number == SqlUniqueConstraintError || sql.Number == SqlUniqueIndexError);
}
