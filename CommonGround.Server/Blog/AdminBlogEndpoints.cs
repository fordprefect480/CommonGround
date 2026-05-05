using System.Security.Claims;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog;

public static class AdminBlogEndpoints
{
    public static RouteGroupBuilder MapAdminBlog(this RouteGroupBuilder admin)
    {
        var blog = admin.MapGroup("/blog");

        blog.MapGet("/posts", async (AppDbContext db) =>
        {
            var posts = await db.BlogPosts
                .AsNoTracking()
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => new BlogPostAdminDto(
                    p.Id, p.Slug, p.Title, p.Excerpt, p.BodyHtml, p.AuthorName,
                    p.CategoryId, p.FeaturedImageId, (int)p.Status,
                    p.PublishedAt, p.CreatedAt, p.UpdatedAt))
                .ToListAsync();
            return Results.Ok(posts);
        });

        blog.MapGet("/posts/{id:int}", async (int id, AppDbContext db) =>
        {
            var post = await db.BlogPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (post is null) return Results.NotFound();

            var dto = new BlogPostAdminDto(
                post.Id, post.Slug, post.Title, post.Excerpt, post.BodyHtml, post.AuthorName,
                post.CategoryId, post.FeaturedImageId, (int)post.Status,
                post.PublishedAt, post.CreatedAt, post.UpdatedAt);
            return Results.Ok(dto);
        });

        blog.MapPost("/posts", async (
            BlogPostWriteDto input,
            AppDbContext db,
            BlogHtmlSanitizer sanitizer,
            UserManager<ApplicationUser> userManager,
            ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest(new { error = "Title is required." });

            var baseSlug = !string.IsNullOrWhiteSpace(input.Slug)
                ? BlogSlug.Derive(input.Slug)
                : BlogSlug.Derive(input.Title);

            if (string.IsNullOrEmpty(baseSlug))
                return Results.BadRequest(new { error = "Could not derive a slug from the title." });

            var resolvedSlug = BlogSlug.Resolve(baseSlug, candidate => db.BlogPosts.Any(p => p.Slug == candidate));

            var now = DateTime.UtcNow;
            var status = (BlogPostStatus)input.Status;

            var currentUser = await userManager.GetUserAsync(user);
            var defaultAuthor = currentUser?.DisplayName
                ?? currentUser?.UserName
                ?? user.Identity?.Name
                ?? "Anonymous";

            var post = new BlogPost
            {
                Slug = resolvedSlug,
                Title = input.Title.Trim(),
                Excerpt = string.IsNullOrWhiteSpace(input.Excerpt) ? null : input.Excerpt.Trim(),
                BodyHtml = sanitizer.Sanitize(input.BodyHtml ?? ""),
                AuthorName = string.IsNullOrWhiteSpace(input.AuthorName)
                    ? defaultAuthor
                    : input.AuthorName.Trim(),
                CategoryId = input.CategoryId,
                FeaturedImageId = input.FeaturedImageId,
                Status = status,
                PublishedAt = status == BlogPostStatus.Published ? now : null,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedByUserId = currentUser?.Id ?? user.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            db.BlogPosts.Add(post);
            await db.SaveChangesAsync();

            var dto = new BlogPostAdminDto(
                post.Id, post.Slug, post.Title, post.Excerpt, post.BodyHtml, post.AuthorName,
                post.CategoryId, post.FeaturedImageId, (int)post.Status,
                post.PublishedAt, post.CreatedAt, post.UpdatedAt);
            return Results.Created($"/api/admin/blog/posts/{post.Id}", dto);
        });

        blog.MapPut("/posts/{id:int}", async (
            int id,
            BlogPostWriteDto input,
            AppDbContext db,
            BlogHtmlSanitizer sanitizer) =>
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post is null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(input.Title))
                return Results.BadRequest(new { error = "Title is required." });

            var requestedSlug = !string.IsNullOrWhiteSpace(input.Slug) ? BlogSlug.Derive(input.Slug) : post.Slug;
            if (string.IsNullOrEmpty(requestedSlug))
                return Results.BadRequest(new { error = "Slug cannot be empty." });

            if (requestedSlug != post.Slug && await db.BlogPosts.AnyAsync(p => p.Slug == requestedSlug))
                return Results.Conflict(new { error = $"Slug '{requestedSlug}' is already in use." });

            var status = (BlogPostStatus)input.Status;
            var becomingPublished = status == BlogPostStatus.Published && post.Status != BlogPostStatus.Published;

            post.Slug = requestedSlug;
            post.Title = input.Title.Trim();
            post.Excerpt = string.IsNullOrWhiteSpace(input.Excerpt) ? null : input.Excerpt.Trim();
            post.BodyHtml = sanitizer.Sanitize(input.BodyHtml ?? "");
            post.AuthorName = string.IsNullOrWhiteSpace(input.AuthorName) ? post.AuthorName : input.AuthorName.Trim();
            post.CategoryId = input.CategoryId;
            post.FeaturedImageId = input.FeaturedImageId;
            post.Status = status;
            post.UpdatedAt = DateTime.UtcNow;
            if (becomingPublished && post.PublishedAt is null)
                post.PublishedAt = post.UpdatedAt;

            await db.SaveChangesAsync();

            var dto = new BlogPostAdminDto(
                post.Id, post.Slug, post.Title, post.Excerpt, post.BodyHtml, post.AuthorName,
                post.CategoryId, post.FeaturedImageId, (int)post.Status,
                post.PublishedAt, post.CreatedAt, post.UpdatedAt);
            return Results.Ok(dto);
        });

        blog.MapDelete("/posts/{id:int}", async (int id, AppDbContext db) =>
        {
            var post = await db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id);
            if (post is null) return Results.NotFound();

            db.BlogPosts.Remove(post);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        blog.MapPost("/images", async (
            HttpRequest request,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart form data." });

            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file was uploaded." });

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest(new { error = "File exceeds 10 MB limit." });

            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest(new { error = $"Content type '{file.ContentType}' is not allowed." });

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            var image = new BlogImage
            {
                ContentType = file.ContentType,
                Bytes = stream.ToArray(),
                OriginalFileName = file.FileName,
                CreatedAt = DateTime.UtcNow,
                UploadedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
            };

            db.BlogImages.Add(image);
            await db.SaveChangesAsync();

            return Results.Ok(new BlogImageUploadDto(image.Id, $"/api/blog/images/{image.Id}"));
        })
        .DisableAntiforgery();

        return admin;
    }
}
