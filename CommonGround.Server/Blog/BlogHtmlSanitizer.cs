using Ganss.Xss;

namespace CommonGround.Server.Blog;

public class BlogHtmlSanitizer
{
    private static readonly string[] AllowedTags =
    [
        "p", "h2", "h3", "h4", "ul", "ol", "li", "blockquote",
        "a", "strong", "em", "code", "pre", "br", "img", "hr", "span",
    ];

    // "style" is allowed only so the editor's text-alignment and text colour
    // survive; the permitted CSS properties are limited to "text-align" and
    // "color" (see AllowedCssProperties below), so no other inline styling can
    // slip through. The sanitizer validates values, so only well-formed colours
    // are kept.
    private static readonly string[] AllowedAttributes = ["href", "src", "alt", "class", "style"];

    public static readonly string[] AllowedImageClasses =
        ["blog-img-small", "blog-img-medium", "blog-img-wide", "blog-img-left", "blog-img-center", "blog-img-right"];

    private static readonly string[] AllowedCssProperties = ["text-align", "color"];

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
