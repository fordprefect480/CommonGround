using Ganss.Xss;

namespace CommonGround.Server.Blog;

public class BlogHtmlSanitizer
{
    private static readonly string[] AllowedTags =
    [
        "p", "h2", "h3", "h4", "ul", "ol", "li", "blockquote",
        "a", "strong", "em", "code", "pre", "br", "img", "hr",
    ];

    // "style" is allowed only so the editor's text-alignment survives; the sole
    // permitted CSS property is "text-align" (see AllowedCssProperties below), so
    // no other inline styling can slip through.
    private static readonly string[] AllowedAttributes = ["href", "src", "alt", "class", "style"];

    public static readonly string[] AllowedImageClasses =
        ["blog-img-small", "blog-img-medium", "blog-img-wide", "blog-img-left", "blog-img-center", "blog-img-right"];

    private static readonly string[] AllowedCssProperties = ["text-align"];

    private readonly HtmlSanitizer _sanitizer;

    public BlogHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer
        {
            AllowedSchemes = { "http", "https", "mailto" },
        };

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedTags) _sanitizer.AllowedTags.Add(tag);

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attr in AllowedAttributes) _sanitizer.AllowedAttributes.Add(attr);

        _sanitizer.AllowedClasses.Clear();
        foreach (var cls in AllowedImageClasses) _sanitizer.AllowedClasses.Add(cls);

        _sanitizer.AllowedCssProperties.Clear();
        foreach (var prop in AllowedCssProperties) _sanitizer.AllowedCssProperties.Add(prop);

        _sanitizer.AllowedAtRules.Clear();
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}
