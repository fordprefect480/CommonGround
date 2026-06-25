using System.Text.RegularExpressions;
using CommonGround.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.BlogImport;

public partial class BlogImporter(
    WixBlogClient client,
    AppDbContext db,
    BlogImportHtmlNormalizer normalizer,
    BlogHtmlSanitizer sanitizer)
{
    [GeneratedRegex(@"<img\s+src=""([^""]+)""")]
    private static partial Regex ImgSrcPattern();

    public async Task<ImportBlogResultDto> ImportAsync(
        int? limit,
        Action<ImportBlogProgressDto>? onProgress,
        CancellationToken ct)
    {
        IReadOnlyList<RemotePostRef> refs;
        try
        {
            refs = await client.EnumeratePostRefsAsync(ct);
        }
        catch (Exception ex)
        {
            return new ImportBlogResultDto(0, 0, 1,
            [
                new ImportBlogErrorDto(null, $"Failed to enumerate posts: {ex.Message}"),
            ]);
        }

        // Posts we already have: keep their stored publish date so they can be ranked for
        // recency without re-fetching them over the network.
        var existing = await db.BlogPosts
            .AsNoTracking()
            .Select(p => new { p.Slug, p.PublishedAt })
            .ToDictionaryAsync(p => p.Slug, p => p.PublishedAt, ct);

        // Rank every post before fetching - posts we already have by their stored publish date,
        // new ones by the sitemap's lastmod - so "import the N most recent" picks the right
        // window and we only pay to fetch the new posts that actually fall inside it.
        IEnumerable<RemotePostRef> ranked = refs
            .OrderByDescending(r => existing.TryGetValue(r.Slug, out var published) ? published : r.LastModified);
        if (limit is int n && n > 0) ranked = ranked.Take(n);
        var window = ranked.ToList();

        var toFetch = window.Where(r => !existing.ContainsKey(r.Slug)).ToList();
        var skipped = window.Count - toFetch.Count;

        var fetched = new List<RemotePost>(toFetch.Count);
        var errors = new List<ImportBlogErrorDto>();

        for (var i = 0; i < toFetch.Count; i++)
        {
            var slug = toFetch[i].Slug;
            try
            {
                var post = await client.FetchPostAsync(slug, ct);
                if (post is null)
                    errors.Add(new ImportBlogErrorDto(slug, "No <article> element found in post page"));
                else
                    fetched.Add(post);
            }
            catch (Exception ex)
            {
                errors.Add(new ImportBlogErrorDto(slug, $"Failed to fetch post: {ex.Message}"));
            }

            onProgress?.Invoke(new ImportBlogProgressDto("fetching", i + 1, toFetch.Count, slug));
        }

        var newslettersCategory = await db.BlogCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == "newsletters", ct);

        var imported = 0;

        for (var i = 0; i < fetched.Count; i++)
        {
            var post = fetched[i];
            try
            {
                await ImportSinglePostAsync(post, newslettersCategory?.Id, errors, ct);
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportBlogErrorDto(post.Slug, $"Post failed: {ex.Message}"));
            }

            onProgress?.Invoke(new ImportBlogProgressDto("importing", i + 1, fetched.Count, post.Slug));
        }

        return new ImportBlogResultDto(imported, skipped, errors.Count, errors);
    }

    private async Task ImportSinglePostAsync(
        RemotePost remote,
        int? categoryId,
        List<ImportBlogErrorDto> errors,
        CancellationToken ct)
    {
        var allImageUrls = CollectUniqueImageUrls(remote);
        var urlToImageId = await DownloadImagesAsync(remote.Slug, allImageUrls, errors, ct);

        var rewrittenBody = ImgSrcPattern().Replace(remote.CleanBodyHtml, match =>
            urlToImageId.TryGetValue(match.Groups[1].Value, out var id)
                ? $"<img src=\"/api/blog/images/{id}\""
                : match.Value);

        var featuredImageId = ResolveFeaturedImageId(remote, urlToImageId);
        var normalizedBody = normalizer.Normalize(rewrittenBody);
        var sanitizedBody = sanitizer.Sanitize(normalizedBody);

        var excerpt = !string.IsNullOrWhiteSpace(remote.Description)
            ? BlogExcerpt.Truncate(remote.Description)
            : BlogExcerpt.FromHtml(sanitizedBody);

        var now = DateTime.UtcNow;
        var post = new BlogPost
        {
            Slug = remote.Slug,
            Title = remote.Title,
            AuthorName = remote.AuthorName,
            BodyHtml = sanitizedBody,
            Excerpt = excerpt,
            CategoryId = categoryId,
            FeaturedImageId = featuredImageId,
            Status = BlogPostStatus.Published,
            PublishedAt = remote.PublishedAt,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.BlogPosts.Add(post);
        await db.SaveChangesAsync(ct);
    }

    private static List<string> CollectUniqueImageUrls(RemotePost remote)
    {
        var urls = new List<string>();
        if (!string.IsNullOrWhiteSpace(remote.CoverImageUrl))
            urls.Add(remote.CoverImageUrl);
        foreach (var url in remote.ImageUrls)
            if (!urls.Contains(url))
                urls.Add(url);
        return urls;
    }

    private async Task<Dictionary<string, int>> DownloadImagesAsync(
        string slug,
        IReadOnlyList<string> imageUrls,
        List<ImportBlogErrorDto> errors,
        CancellationToken ct)
    {
        var pending = new List<(string Url, BlogImage Image)>(imageUrls.Count);

        foreach (var imageUrl in imageUrls)
        {
            var result = await client.FetchImageAsync(imageUrl, ct);
            if (result is null)
            {
                errors.Add(new ImportBlogErrorDto(slug, $"Image fetch failed (soft): {imageUrl}"));
                continue;
            }

            var (bytes, _) = result.Value;
            var detectedType = BlogImageContentType.DetectFromBytes(bytes);
            if (detectedType is null)
            {
                errors.Add(new ImportBlogErrorDto(slug, $"Image content not recognised as a supported format: {imageUrl}"));
                continue;
            }

            var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
            var image = new BlogImage
            {
                ContentType = detectedType,
                Bytes = bytes,
                OriginalFileName = string.IsNullOrWhiteSpace(fileName) ? null : fileName,
                CreatedAt = DateTime.UtcNow,
            };
            db.BlogImages.Add(image);
            pending.Add((imageUrl, image));
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);

        var urlToImageId = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (url, image) in pending)
            urlToImageId[url] = image.Id;
        return urlToImageId;
    }

    private static int? ResolveFeaturedImageId(RemotePost remote, Dictionary<string, int> urlToImageId)
    {
        if (!string.IsNullOrWhiteSpace(remote.CoverImageUrl) &&
            urlToImageId.TryGetValue(remote.CoverImageUrl, out var coverId))
            return coverId;

        if (remote.ImageUrls.Count > 0 &&
            urlToImageId.TryGetValue(remote.ImageUrls[0], out var firstInlineId))
            return firstInlineId;

        return null;
    }
}
