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

    [GeneratedRegex(@"/v1/[^/]+/")]
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
        "post-description",
    };

    public async Task<IReadOnlyList<string>> EnumeratePostSlugsAsync(CancellationToken ct)
    {
        var url = $"{SiteRoot.TrimEnd('/')}/blog";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var parser = context.GetService<IHtmlParser>()!;
        using var document = await parser.ParseDocumentAsync(html, ct);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var slugs = new List<string>();

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href") ?? "";

            // Handle both absolute and root-relative URLs
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

            // Strip query string from path for matching
            var qIdx = path.IndexOf('?');
            if (qIdx >= 0) path = path[..qIdx];

            if (!path.StartsWith("/post/", StringComparison.OrdinalIgnoreCase)) continue;

            var slug = ExtractSlug(path);
            if (slug is null) continue;

            if (seen.Add(slug))
                slugs.Add(slug);
        }

        return slugs;
    }

    public async Task<RemotePost?> FetchPostAsync(string slug, CancellationToken ct)
    {
        var url = $"{SiteRoot.TrimEnd('/')}/post/{slug}";
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);

        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var parser = context.GetService<IHtmlParser>()!;
        using var document = await parser.ParseDocumentAsync(html, ct);

        // --- JSON-LD extraction ---
        string? ldTitle = null;
        string? ldAuthor = null;
        DateTime? ldPublishedAt = null;

        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var json = script.TextContent;
            if (string.IsNullOrWhiteSpace(json)) continue;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement? posting = null;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in root.EnumerateArray())
                    {
                        if (el.TryGetProperty("@type", out var t) &&
                            t.GetString()?.Equals("BlogPosting", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            posting = el;
                            break;
                        }
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("@type", out var t) &&
                        t.GetString()?.Equals("BlogPosting", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        posting = root;
                    }
                }

                if (posting is null) continue;

                if (posting.Value.TryGetProperty("headline", out var headline))
                    ldTitle = headline.GetString();

                if (posting.Value.TryGetProperty("author", out var authorEl))
                {
                    if (authorEl.ValueKind == JsonValueKind.Object &&
                        authorEl.TryGetProperty("name", out var nameProp))
                        ldAuthor = nameProp.GetString();
                    else if (authorEl.ValueKind == JsonValueKind.String)
                        ldAuthor = authorEl.GetString();
                }

                if (posting.Value.TryGetProperty("datePublished", out var datePub))
                {
                    var raw = datePub.GetString();
                    if (raw is not null && DateTimeOffset.TryParse(raw, out var dto))
                        ldPublishedAt = dto.UtcDateTime;
                }

                break; // found a BlogPosting — no need to check further scripts
            }
            catch (JsonException)
            {
                // malformed — try next script
            }
        }

        // --- OG / article meta extraction ---
        string? ogTitle = OgMeta(document, "og:title");
        string? ogDescription = OgMeta(document, "og:description");
        string? ogImage = OgMeta(document, "og:image");
        string? articleAuthor = OgMeta(document, "article:author");
        string? articlePublishedTime = OgMeta(document, "article:published_time");

        // --- Fallback element extraction ---
        string? h1Title = document.QuerySelector("[data-hook='post-title']")?.TextContent?.Trim();
        string? spanAuthor = document.QuerySelector("[data-hook='user-name']")?.TextContent?.Trim();

        // --- Resolve title ---
        var title = NonEmpty(ldTitle)
            ?? NonEmpty(ogTitle)
            ?? NonEmpty(h1Title)
            ?? slug;

        // --- Resolve author ---
        var authorName = NonEmpty(ldAuthor)
            ?? NonEmpty(articleAuthor)
            ?? NonEmpty(spanAuthor)
            ?? "Unknown";

        // --- Resolve published date ---
        DateTime publishedAt;
        if (ldPublishedAt.HasValue)
        {
            publishedAt = ldPublishedAt.Value;
        }
        else if (articlePublishedTime is not null &&
                 DateTimeOffset.TryParse(articlePublishedTime, out var ogDto))
        {
            publishedAt = ogDto.UtcDateTime;
        }
        else
        {
            publishedAt = DateTime.UtcNow;
        }

        // --- Resolve cover image ---
        string? coverImageUrl = null;
        if (!string.IsNullOrWhiteSpace(ogImage))
            coverImageUrl = WixTransformSegment().Replace(ogImage, "/").TrimEnd('/');

        // --- Body extraction ---
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
            using var response = await http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return (bytes, contentType);
        }
        catch
        {
            return null;
        }
    }

    private static string? OgMeta(IDocument document, string property)
    {
        var el = document.QuerySelector($"meta[property='{property}']");
        return el?.GetAttribute("content")?.Trim();
    }

    private static string? NonEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;

    private void WalkNode(INode node, StringBuilder sb, List<string> imageUrls)
    {
        if (node is IElement el)
        {
            var hook = el.GetAttribute("data-hook")?.Trim() ?? "";

            if (DroppedHooks.Contains(hook)) return;

            var tagName = el.TagName.ToUpperInvariant();

            if (hook.Equals("figure-IMAGE", StringComparison.OrdinalIgnoreCase))
            {
                EmitFigureImage(el, sb, imageUrls);
                return;
            }

            if (hook is "divider divider-single" || hook == "divider")
            {
                sb.Append("<hr>");
                return;
            }

            if (hook.Equals("web-link", StringComparison.OrdinalIgnoreCase) || tagName == "A")
            {
                var href = el.GetAttribute("href") ?? "#";
                sb.Append($"<a href=\"{System.Net.WebUtility.HtmlEncode(href)}\">");
                foreach (var child in el.ChildNodes)
                    WalkNode(child, sb, imageUrls);
                sb.Append("</a>");
                return;
            }

            if (tagName is "P" or "H2" or "H3" or "H4" or "UL" or "OL" or "LI"
                or "BLOCKQUOTE" or "STRONG" or "EM" or "CODE" or "PRE" or "BR")
            {
                var lowerTag = tagName.ToLowerInvariant();
                sb.Append($"<{lowerTag}>");
                foreach (var child in el.ChildNodes)
                    WalkNode(child, sb, imageUrls);
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

            foreach (var child in el.ChildNodes)
                WalkNode(child, sb, imageUrls);
        }
        else if (node is IText text)
        {
            var value = text.TextContent;
            if (!string.IsNullOrEmpty(value))
                sb.Append(System.Net.WebUtility.HtmlEncode(value));
        }
    }

    private static void EmitFigureImage(IElement figure, StringBuilder sb, List<string> imageUrls)
    {
        var img = figure.QuerySelector("img");
        if (img is null) return;
        EmitImg(img, sb, imageUrls);
    }

    private static void EmitImg(IElement img, StringBuilder sb, List<string> imageUrls)
    {
        var rawSrc = img.GetAttribute("src") ?? img.GetAttribute("data-src") ?? "";
        if (string.IsNullOrWhiteSpace(rawSrc)) return;

        var cleanSrc = WixTransformSegment().Replace(rawSrc, "/");
        cleanSrc = cleanSrc.TrimEnd('/');

        var alt = System.Net.WebUtility.HtmlEncode(img.GetAttribute("alt") ?? "");
        sb.Append($"<img src=\"{System.Net.WebUtility.HtmlEncode(cleanSrc)}\" alt=\"{alt}\">");

        if (!string.IsNullOrWhiteSpace(cleanSrc))
            imageUrls.Add(cleanSrc);
    }

    private static string? ExtractSlug(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var idx = url.LastIndexOf('/');
        if (idx < 0) return null;
        var slug = url[(idx + 1)..].Trim();
        var qIdx = slug.IndexOf('?');
        if (qIdx >= 0) slug = slug[..qIdx];
        return string.IsNullOrEmpty(slug) ? null : slug;
    }
}
