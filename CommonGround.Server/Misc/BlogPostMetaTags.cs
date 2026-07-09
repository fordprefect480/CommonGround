using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace CommonGround.Server.Misc;

/// <summary>
/// Rewrites the default social/SEO meta tags in the SPA's <c>index.html</c> to a
/// specific blog post's values, so link unfurlers (Facebook, LinkedIn, Slack, ...)
/// that read raw HTML — and never run the SPA's client-side <c>Seo</c> component —
/// show the post's featured image, title, and excerpt.
/// </summary>
public static partial class BlogPostMetaTags
{
    /// <summary>The post fields needed to build its social/SEO tags.</summary>
    public sealed record PostMeta(string Title, string Description, string Slug, int? FeaturedImageId);

    /// <summary>
    /// Returns <paramref name="html"/> with the post's title, description, canonical
    /// URL and Open Graph / Twitter tags injected. The featured image is applied only
    /// when the post has one; otherwise the default image tags are left untouched.
    /// </summary>
    public static string Inject(string html, PostMeta post, string siteUrl, string gardenName)
    {
        var enc = HtmlEncoder.Default;
        var fullTitle = enc.Encode($"{post.Title} | {gardenName}");
        var description = enc.Encode(post.Description);
        var url = enc.Encode($"{siteUrl}/blog/{post.Slug}");

        var replacements = new Dictionary<string, string>
        {
            ["description"] = description,
            ["og:title"] = fullTitle,
            ["og:description"] = description,
            ["og:type"] = "article",
            ["og:url"] = url,
            ["twitter:title"] = fullTitle,
            ["twitter:description"] = description,
        };

        if (post.FeaturedImageId is int imageId)
        {
            var imageUrl = enc.Encode($"{siteUrl}/api/blog/images/{imageId}");
            replacements["og:image"] = imageUrl;
            replacements["twitter:image"] = imageUrl;
        }

        html = MetaTag().Replace(html, m =>
            replacements.TryGetValue(m.Groups["key"].Value, out var content)
                ? $"<meta {m.Groups["attr"].Value}=\"{m.Groups["key"].Value}\" content=\"{content}\""
                : m.Value);

        html = TitleTag().Replace(html, _ => $"<title>{fullTitle}</title>", 1);
        html = CanonicalLink().Replace(html, _ => $"<link rel=\"canonical\" href=\"{url}\"", 1);

        return html;
    }

    // \s+ spans the newlines used by index.html's multi-line <meta> formatting, so
    // both single-line and wrapped tags match. Only the opening tag through the
    // content value is captured; any trailing " />" and whitespace is left in place.
    [GeneratedRegex(""""<meta\s+(?<attr>name|property)="(?<key>[^"]+)"\s+content="[^"]*"""")]
    private static partial Regex MetaTag();

    [GeneratedRegex("<title>[^<]*</title>")]
    private static partial Regex TitleTag();

    [GeneratedRegex(""""<link\s+rel="canonical"\s+href="[^"]*"""")]
    private static partial Regex CanonicalLink();
}
