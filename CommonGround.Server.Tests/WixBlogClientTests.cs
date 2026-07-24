using CommonGround.Server.Blog;
using CommonGround.Server.Blog.BlogImport;

namespace CommonGround.Server.Tests;

// Characterisation tests for the HTML-cleaning walk in WixBlogClient. The walk
// rebuilds a post body from an allowlist of tags rather than trusting the
// source markup, so these lock in exactly what survives the import.
public class WixBlogClientTests
{
    private static string Clean(string articleHtml) => WixBlogClient.CleanArticleBody(articleHtml).Html;

    [Fact]
    public void Passthrough_tags_are_kept_but_their_attributes_are_dropped()
    {
        var result = Clean("<article><p class=\"x\" data-hook=\"whatever\">Hi</p></article>");

        Assert.Equal("<p>Hi</p>", result);
    }

    [Fact]
    public void Nested_passthrough_formatting_is_preserved()
    {
        var result = Clean("<article><p>a <strong>b</strong> <em>c</em></p></article>");

        Assert.Equal("<p>a <strong>b</strong> <em>c</em></p>", result);
    }

    [Fact]
    public void Links_keep_only_their_href()
    {
        var result = Clean("<article><a href=\"https://x.test\" target=\"_blank\" class=\"c\">link</a></article>");

        Assert.Equal("<a href=\"https://x.test\">link</a>", result);
    }

    [Fact]
    public void Dropped_hook_elements_are_removed_entirely()
    {
        var result = Clean("<article><div data-hook=\"image-expand-button\">junk</div><p>keep</p></article>");

        Assert.Equal("<p>keep</p>", result);
    }

    [Theory]
    [InlineData("header")]
    [InlineData("footer")]
    [InlineData("button")]
    [InlineData("nav")]
    public void Chrome_elements_are_removed(string tag)
    {
        var result = Clean($"<article><{tag}>x</{tag}><p>body</p></article>");

        Assert.Equal("<p>body</p>", result);
    }

    [Fact]
    public void Divider_hook_becomes_a_horizontal_rule()
    {
        var result = Clean("<article><div data-hook=\"divider\">ignored</div></article>");

        Assert.Equal("<hr>", result);
    }

    [Fact]
    public void Unknown_wrappers_are_unwrapped_keeping_their_text()
    {
        var result = Clean("<article><div><span>abc</span></div></article>");

        Assert.Equal("abc", result);
    }

    [Fact]
    public void Text_is_html_encoded()
    {
        var result = Clean("<article><p>a &amp; b &lt;c&gt;</p></article>");

        Assert.Equal("<p>a &amp; b &lt;c&gt;</p>", result);
    }

    [Fact]
    public void Images_are_emitted_with_src_and_alt_and_collected()
    {
        var (html, imageUrls) = WixBlogClient.CleanArticleBody("<article><img src=\"/media/x.png\" alt=\"pic\"></article>");

        Assert.Equal("<img src=\"/media/x.png\" alt=\"pic\">", html);
        Assert.Equal(["/media/x.png"], imageUrls);
    }

    [Fact]
    public void Figure_image_hooks_emit_their_inner_image()
    {
        var result = Clean("<article><div data-hook=\"figure-image\"><img src=\"/m/y.jpg\" alt=\"f\"></div></article>");

        Assert.Equal("<img src=\"/m/y.jpg\" alt=\"f\">", result);
    }

    [Fact]
    public void Preserves_text_colour_on_spans()
    {
        var result = Clean("<article><p>a <span style=\"color: rgb(200, 74, 48)\">red</span> b</p></article>");

        Assert.Equal("<p>a <span style=\"color: rgb(200, 74, 48)\">red</span> b</p>", result);
    }

    [Fact]
    public void Keeps_only_the_colour_from_a_coloured_spans_style()
    {
        var result = Clean("<article><p><span style=\"font-size: 40px; color: #c84a30\">big red</span></p></article>");

        Assert.Equal("<p><span style=\"color: #c84a30\">big red</span></p>", result);
    }

    [Fact]
    public void Preserves_colour_while_keeping_nested_formatting()
    {
        var result = Clean("<article><p><span style=\"color: #2e4f25\"><strong>bold green</strong></span></p></article>");

        Assert.Equal("<p><span style=\"color: #2e4f25\"><strong>bold green</strong></span></p>", result);
    }

    // Import runs clean -> normalize -> sanitize. This confirms a coloured Wix
    // span survives all three stages so it renders on the imported post.
    [Fact]
    public void Colour_survives_the_full_import_pipeline()
    {
        var cleaned = Clean("<article><p>see <span style=\"color: rgb(200, 74, 48)\">this</span></p></article>");
        var normalized = new BlogImportHtmlNormalizer().Normalize(cleaned);
        var sanitized = new BlogHtmlSanitizer().Sanitize(normalized);

        Assert.Contains("<span", sanitized);
        Assert.Contains("color", sanitized);
        Assert.Contains("this", sanitized);
    }
}
