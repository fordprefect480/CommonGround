using Ganss.Xss;

namespace CommonGround.Server.Blog;

public class BlogHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public BlogHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer
        {
            AllowedSchemes = { "http", "https", "mailto" },
        };

        _sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "h2", "h3", "h4", "ul", "ol", "li", "blockquote", "a", "strong", "em", "code", "pre", "br", "img", "hr" })
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("alt");

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedAtRules.Clear();
    }

    public string Sanitize(string html) => _sanitizer.Sanitize(html ?? "");
}
