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
    public void Strips_non_alignment_style_properties()
    {
        var result = Sanitize("<p style=\"text-align: center; color: red; position: fixed\">hi</p>");

        Assert.Contains("text-align", result);
        Assert.DoesNotContain("color", result);
        Assert.DoesNotContain("position", result);
    }
}
