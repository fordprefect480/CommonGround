using System.Text.RegularExpressions;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.BlogImport;

public partial class BlogImporter(
    WixBlogClient client,
    AppDbContext db,
    BlogHtmlSanitizer sanitizer)
{
    [GeneratedRegex(@"<img\s+src=""([^""]+)""")]
    private static partial Regex ImgSrcPattern();

    public async Task<ImportBlogResultDto> ImportAsync(int? limit, CancellationToken ct)
    {
        // Phase 1: enumerate slugs
        IReadOnlyList<string> slugs;
        try
        {
            slugs = await client.EnumeratePostSlugsAsync(ct);
        }
        catch (Exception ex)
        {
            return new ImportBlogResultDto(0, 0, 1,
            [
                new ImportBlogErrorDto(null, $"Failed to enumerate posts: {ex.Message}")
            ]);
        }

        // Fetch metadata for every slug (needed to sort by date before applying limit)
        var fetched = new List<RemotePost>(slugs.Count);
        var fetchErrors = new List<ImportBlogErrorDto>();

        foreach (var slug in slugs)
        {
            try
            {
                var post = await client.FetchPostAsync(slug, ct);
                if (post is null)
                {
                    fetchErrors.Add(new ImportBlogErrorDto(slug, "No <article> element found in post page"));
                    continue;
                }
                fetched.Add(post);
            }
            catch (Exception ex)
            {
                fetchErrors.Add(new ImportBlogErrorDto(slug, $"Failed to fetch post: {ex.Message}"));
            }
        }

        // Phase 2: sort descending by date, apply limit
        IEnumerable<RemotePost> ordered = fetched.OrderByDescending(p => p.PublishedAt);
        if (limit is int n && n > 0) ordered = ordered.Take(n);
        var toImport = ordered.ToList();

        var existingSlugs = await db.BlogPosts
            .AsNoTracking()
            .Select(p => p.Slug)
            .ToHashSetAsync(ct);

        var categories = await db.BlogCategories
            .AsNoTracking()
            .ToListAsync(ct);

        var newslettersCategory = categories.FirstOrDefault(c =>
            c.Slug.Equals("newsletters", StringComparison.OrdinalIgnoreCase));

        var imported = 0;
        var skipped = 0;
        var errors = new List<ImportBlogErrorDto>(fetchErrors);

        // Phase 3: image download + DB insert
        foreach (var remote in toImport)
        {
            if (existingSlugs.Contains(remote.Slug))
            {
                skipped++;
                continue;
            }

            try
            {
                // Collect unique image URLs: cover first, then inline
                var allImageUrls = new List<string>();
                if (!string.IsNullOrWhiteSpace(remote.CoverImageUrl))
                    allImageUrls.Add(remote.CoverImageUrl);
                foreach (var url in remote.ImageUrls)
                    if (!allImageUrls.Contains(url))
                        allImageUrls.Add(url);

                // Download each image and map original URL → new BlogImage ID
                var urlToImageId = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var imageUrl in allImageUrls)
                {
                    var result = await client.FetchImageAsync(imageUrl, ct);
                    if (result is null)
                    {
                        errors.Add(new ImportBlogErrorDto(remote.Slug, $"Image fetch failed (soft): {imageUrl}"));
                        continue;
                    }

                    var (bytes, contentType) = result.Value;
                    var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
                    var image = new BlogImage
                    {
                        ContentType = contentType,
                        Bytes = bytes,
                        OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName,
                        CreatedAt = DateTime.UtcNow,
                    };
                    db.BlogImages.Add(image);
                    await db.SaveChangesAsync(ct);
                    urlToImageId[imageUrl] = image.Id;
                }

                // Rewrite <img src="X"> → <img src="/api/blog/images/{id}">
                var rewrittenBody = ImgSrcPattern().Replace(remote.CleanBodyHtml, match =>
                {
                    var src = match.Groups[1].Value;
                    if (urlToImageId.TryGetValue(src, out var id))
                        return $"<img src=\"/api/blog/images/{id}\"";
                    return match.Value;
                });

                // Determine featured image: cover URL if fetched, else first inline, else null
                int? featuredImageId = null;
                if (!string.IsNullOrWhiteSpace(remote.CoverImageUrl) &&
                    urlToImageId.TryGetValue(remote.CoverImageUrl, out var coverId))
                    featuredImageId = coverId;
                else if (remote.ImageUrls.Count > 0 &&
                         urlToImageId.TryGetValue(remote.ImageUrls[0], out var firstInlineId))
                    featuredImageId = firstInlineId;

                var sanitizedBody = sanitizer.Sanitize(rewrittenBody);

                var excerpt = !string.IsNullOrWhiteSpace(remote.Description)
                    ? (remote.Description.Length > 300
                        ? remote.Description[..300] + "…"
                        : remote.Description)
                    : BlogExcerpt.FromHtml(sanitizedBody, 300);

                var now = DateTime.UtcNow;
                var post = new BlogPost
                {
                    Slug = remote.Slug,
                    Title = remote.Title,
                    AuthorName = remote.AuthorName,
                    BodyHtml = sanitizedBody,
                    Excerpt = excerpt,
                    CategoryId = newslettersCategory?.Id,
                    FeaturedImageId = featuredImageId,
                    Status = BlogPostStatus.Published,
                    PublishedAt = remote.PublishedAt,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                db.BlogPosts.Add(post);
                await db.SaveChangesAsync(ct);
                existingSlugs.Add(remote.Slug);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportBlogErrorDto(remote.Slug, $"Post failed: {ex.Message}"));
            }
        }

        return new ImportBlogResultDto(imported, skipped, errors.Count, errors);
    }
}
