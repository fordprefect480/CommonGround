using CommonGround.Server.Blog;

namespace CommonGround.Server.Tests;

public class BlogHtmlSanitizerTests
{
    private static string Sanitize(string html) => new BlogHtmlSanitizer().Sanitize(html);

    [Theory]
    [InlineData("blog-img-left")]
    [InlineData("blog-img-center")]
    [InlineData("blog-img-right")]
    public void Keeps_image_alignment_classes(string alignClass)
    {
        var result = Sanitize($"<img src=\"/api/blog/images/1\" alt=\"x\" class=\"blog-img-medium {alignClass}\">");

        Assert.Contains(alignClass, result);
        Assert.Contains("blog-img-medium", result);
    }

    [Fact]
    public void Strips_unknown_image_classes()
    {
        var result = Sanitize("<img src=\"/api/blog/images/1\" alt=\"x\" class=\"blog-img-medium totally-not-allowed\">");

        Assert.DoesNotContain("totally-not-allowed", result);
        Assert.Contains("blog-img-medium", result);
    }

    [Theory]
    [InlineData("left")]
    [InlineData("center")]
    [InlineData("right")]
    public void Keeps_text_align_on_paragraphs(string align)
    {
        var result = Sanitize($"<p style=\"text-align: {align}\">hi</p>");

        Assert.Contains("text-align", result);
        Assert.Contains(align, result);
    }

    [Fact]
    public void Keeps_text_align_on_headings()
    {
        var result = Sanitize("<h2 style=\"text-align: center\">Title</h2>");

        Assert.Contains("text-align", result);
        Assert.Contains("center", result);
    }

    [Fact]
    public void Strips_disallowed_style_properties()
    {
        var result = Sanitize("<p style=\"text-align: center; position: fixed\">hi</p>");

        Assert.Contains("text-align", result);
        Assert.DoesNotContain("position", result);
    }

    [Fact]
    public void Keeps_text_colour_on_spans()
    {
        // The sanitizer may re-serialize the colour value (e.g. hex -> rgba), so
        // assert the span and colour property survive rather than the exact text.
        var result = Sanitize("<p>hello <span style=\"color: #c84a30\">world</span></p>");

        Assert.Contains("<span", result);
        Assert.Contains("color", result);
        Assert.Contains("world", result);
    }

    [Fact]
    public void Strips_disallowed_properties_but_keeps_colour()
    {
        var result = Sanitize("<span style=\"color: #527e40; position: fixed\">hi</span>");

        Assert.Contains("color", result);
        Assert.DoesNotContain("position", result);
    }
}
