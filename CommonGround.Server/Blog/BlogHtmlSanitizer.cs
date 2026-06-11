using Ganss.Xss;

namespace CommonGround.Server.Blog;

public class BlogHtmlSanitizer
{
    private static readonly string[] AllowedTags =
    [
        "p", "h2", "h3", "h4", "ul", "ol", "li", "blockquote",
        "a", "strong", "em", "code", "pre", "br", "img", "hr",
    ];

    private static readonly string[] AllowedAttributes = ["href", "src", "alt", "class"];

    public static readonly string[] AllowedImageClasses =
        ["blog-img-small", "blog-img-medium", "blog-img-wide"];

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
        _sanitizer.AllowedAtRules.Clear();
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html);
}
