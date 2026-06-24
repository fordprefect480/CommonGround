using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace CommonGround.Server.Blog.BlogImport;

public record RemotePost(
    string Slug,
    string Title,
    string AuthorName,
    DateTime PublishedAt,
    string? Description,
    string? CoverImageUrl,
    string CleanBodyHtml,
    IReadOnlyList<string> ImageUrls);

public partial class WixBlogClient(HttpClient http, IConfiguration config)
{
    private string SiteRoot => config["WixSiteRoot"] ?? "https://www.seafordwetlandscommunitygarden.com";

    [GeneratedRegex(@"/([^/?]+\.\w+)/[^?]+/\1")]
    private static partial Regex WixTransformSegment();

    private static readonly HashSet<string> DroppedHooks = new(StringComparer.OrdinalIgnoreCase)
    {
        "image-expand-button",
        "gap-spacer",
        "skeleton-loader",
        "more-button",
        "profile-link",
        "time-ago",
        "time-to-read",
        "post-title",
        "user-name",
    };

    private static readonly HashSet<string> PassthroughTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "P", "H2", "H3", "H4", "UL", "OL", "LI", "BLOCKQUOTE", "STRONG", "EM", "CODE", "PRE", "BR",
    };

    public async Task<IReadOnlyList<string>> EnumeratePostSlugsAsync(CancellationToken ct)
    {
        var url = $"{SiteRoot.TrimEnd('/')}/blog";
        using var document = await LoadDocumentAsync(url, ct);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slugs = new List<string>();

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href") ?? "";

            string path;
            if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(href, UriKind.Absolute, out var uri)) continue;
                path = uri.AbsolutePath;
            }
            else
            {
                path = href;
            }

            var qIdx = path.IndexOf('?');
            if (qIdx >= 0) path = path[..qIdx];

            if (!path.Contains("/post/", StringComparison.OrdinalIgnoreCase)) continue;

            var slug = ExtractSlug(path);
            if (slug is not null && seen.Add(slug))
                slugs.Add(slug);
        }

        return slugs;
    }

    public async Task<RemotePost?> FetchPostAsync(string slug, CancellationToken ct)
    {
        var url = $"{SiteRoot.TrimEnd('/')}/post/{slug}";
        using var document = await LoadDocumentAsync(url, ct);

        var ldData = ExtractBlogPostingJsonLd(document);

        var ogTitle = OgMeta(document, "og:title");
        var ogDescription = OgMeta(document, "og:description");
        var ogImage = OgMeta(document, "og:image");
        var articleAuthor = OgMeta(document, "article:author");
        var articlePublishedTime = OgMeta(document, "article:published_time");

        var h1Title = document.QuerySelector("[data-hook='post-title']")?.TextContent?.Trim();
        var spanAuthor = document.QuerySelector("[data-hook='user-name']")?.TextContent?.Trim();

        var title = NonEmpty(ldData.Title) ?? NonEmpty(ogTitle) ?? NonEmpty(h1Title) ?? slug;
        var authorName = NonEmpty(ldData.Author) ?? NonEmpty(articleAuthor) ?? NonEmpty(spanAuthor) ?? "Unknown";
        var publishedAt = ResolvePublishedAt(ldData.PublishedAt, articlePublishedTime);

        string? coverImageUrl = null;
        if (!string.IsNullOrWhiteSpace(ogImage))
            coverImageUrl = WixTransformSegment().Replace(ogImage, "/$1");

        var article = document.QuerySelector("article");
        if (article is null) return null;

        var imageUrls = new List<string>();
        var sb = new StringBuilder();
        WalkNode(article, sb, imageUrls);

        return new RemotePost(
            Slug: slug,
            Title: title,
            AuthorName: authorName,
            PublishedAt: publishedAt,
            Description: NonEmpty(ogDescription),
            CoverImageUrl: coverImageUrl,
            CleanBodyHtml: sb.ToString(),
            ImageUrls: imageUrls);
    }

    public async Task<(byte[] bytes, string contentType)?> FetchImageAsync(string url, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return null;

            if (response.Content.Headers.ContentLength is long declared &&
                declared > BlogImageContentType.MaxBytes)
                return null;

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            var rented = new byte[81920];
            int read;
            while ((read = await source.ReadAsync(rented, ct)) > 0)
            {
                if (buffer.Length + read > BlogImageContentType.MaxBytes) return null;
                buffer.Write(rented, 0, read);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return (buffer.ToArray(), contentType);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IDocument> LoadDocumentAsync(string url, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);
        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var parser = context.GetService<IHtmlParser>()!;
        return await parser.ParseDocumentAsync(html, ct);
    }

    private record LdBlogPosting(string? Title, string? Author, DateTime? PublishedAt);

    private static LdBlogPosting ExtractBlogPostingJsonLd(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var json = script.TextContent;
            if (string.IsNullOrWhiteSpace(json)) continue;

            JsonElement? posting;
            try
            {
                using var doc = JsonDocument.Parse(json);
                posting = FindBlogPosting(doc.RootElement);
                if (posting is null) continue;

                string? title = null;
                string? author = null;
                DateTime? publishedAt = null;

                if (posting.Value.TryGetProperty("headline", out var headline))
                    title = headline.GetString();

                if (posting.Value.TryGetProperty("author", out var authorEl))
                    author = ReadAuthorName(authorEl);

                if (posting.Value.TryGetProperty("datePublished", out var datePub) &&
                    DateTimeOffset.TryParse(datePub.GetString(), out var dto))
                    publishedAt = dto.UtcDateTime;

                return new LdBlogPosting(title, author, publishedAt);
            }
            catch (JsonException)
            {
                // malformed - try next script
            }
        }

        return new LdBlogPosting(null, null, null);
    }

    private static JsonElement? FindBlogPosting(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
                if (IsBlogPosting(el)) return el;
            return null;
        }

        return root.ValueKind == JsonValueKind.Object && IsBlogPosting(root) ? root : null;
    }

    private static bool IsBlogPosting(JsonElement el) =>
        el.TryGetProperty("@type", out var t) &&
        t.GetString()?.Equals("BlogPosting", StringComparison.OrdinalIgnoreCase) == true;

    private static string? ReadAuthorName(JsonElement authorEl) => authorEl.ValueKind switch
    {
        JsonValueKind.Object when authorEl.TryGetProperty("name", out var nameProp) => nameProp.GetString(),
        JsonValueKind.String => authorEl.GetString(),
        _ => null,
    };

    private static DateTime ResolvePublishedAt(DateTime? fromLd, string? articlePublishedTime)
    {
        if (fromLd.HasValue) return fromLd.Value;
        if (DateTimeOffset.TryParse(articlePublishedTime, out var dto)) return dto.UtcDateTime;
        return DateTime.UtcNow;
    }

    private static string? OgMeta(IDocument document, string property) =>
        document.QuerySelector($"meta[property='{property}']")?.GetAttribute("content")?.Trim();

    private static string? NonEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private void WalkNode(INode node, StringBuilder sb, List<string> imageUrls)
    {
        switch (node)
        {
            case IElement el:
                WalkElement(el, sb, imageUrls);
                break;
            case IText text when !string.IsNullOrEmpty(text.TextContent):
                sb.Append(System.Net.WebUtility.HtmlEncode(text.TextContent));
                break;
        }
    }

    private void WalkElement(IElement el, StringBuilder sb, List<string> imageUrls)
    {
        var hook = el.GetAttribute("data-hook")?.Trim() ?? "";

        if (DroppedHooks.Contains(hook)) return;

        var tagName = el.TagName.ToUpperInvariant();

        if (tagName is "HEADER" or "FOOTER" or "BUTTON" or "SVG" or "NAV") return;

        if (string.Equals(hook, "figure-IMAGE", StringComparison.OrdinalIgnoreCase))
        {
            EmitFigureImage(el, sb, imageUrls);
            return;
        }

        if (hook is "divider divider-single" or "divider")
        {
            sb.Append("<hr>");
            return;
        }

        if (string.Equals(hook, "web-link", StringComparison.OrdinalIgnoreCase) || tagName == "A")
        {
            var href = el.GetAttribute("href") ?? "#";
            sb.Append($"<a href=\"{System.Net.WebUtility.HtmlEncode(href)}\">");
            foreach (var child in el.ChildNodes) WalkNode(child, sb, imageUrls);
            sb.Append("</a>");
            return;
        }

        if (PassthroughTags.Contains(tagName))
        {
            var lowerTag = tagName.ToLowerInvariant();
            sb.Append($"<{lowerTag}>");
            foreach (var child in el.ChildNodes) WalkNode(child, sb, imageUrls);
            sb.Append($"</{lowerTag}>");
            return;
        }

        if (tagName == "HR")
        {
            sb.Append("<hr>");
            return;
        }

        if (tagName == "IMG")
        {
            EmitImg(el, sb, imageUrls);
            return;
        }

        foreach (var child in el.ChildNodes) WalkNode(child, sb, imageUrls);
    }

    private static void EmitFigureImage(IElement figure, StringBuilder sb, List<string> imageUrls)
    {
        var img = figure.QuerySelector("img");
        if (img is not null) EmitImg(img, sb, imageUrls);
    }

    private static void EmitImg(IElement img, StringBuilder sb, List<string> imageUrls)
    {
        var rawSrc = img.GetAttribute("src") ?? img.GetAttribute("data-src") ?? "";
        if (string.IsNullOrWhiteSpace(rawSrc)) return;

        var cleanSrc = WixTransformSegment().Replace(rawSrc, "/$1");
        var alt = System.Net.WebUtility.HtmlEncode(img.GetAttribute("alt") ?? "");
        sb.Append($"<img src=\"{System.Net.WebUtility.HtmlEncode(cleanSrc)}\" alt=\"{alt}\">");

        if (!string.IsNullOrWhiteSpace(cleanSrc))
            imageUrls.Add(cleanSrc);
    }

    private static string? ExtractSlug(string url)
    {
        var idx = url.LastIndexOf('/');
        if (idx < 0) return null;
        var slug = url[(idx + 1)..].Trim();
        var qIdx = slug.IndexOf('?');
        if (qIdx >= 0) slug = slug[..qIdx];
        return string.IsNullOrEmpty(slug) ? null : slug;
    }
}
