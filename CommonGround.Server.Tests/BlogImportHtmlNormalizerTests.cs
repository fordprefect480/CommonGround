using CommonGround.Server.Blog.BlogImport;

namespace CommonGround.Server.Tests;

public class BlogImportHtmlNormalizerTests
{
    private static string Normalize(string html) => new BlogImportHtmlNormalizer().Normalize(html);

    [Fact]
    public void Strips_all_line_breaks()
    {
        Assert.Equal("<p>a</p><p>b</p>", Normalize("<p>a</p><br><br><p>b</p>"));
    }

    [Fact]
    public void Adds_medium_class_to_images()
    {
        var result = Normalize("<p>x</p><img src=\"/api/blog/images/1\" alt=\"cat\">");

        Assert.Equal("<p>x</p><img src=\"/api/blog/images/1\" alt=\"cat\" class=\"blog-img-medium\">", result);
    }

    [Fact]
    public void Removes_empty_paragraphs()
    {
        Assert.Equal("<p>x</p>", Normalize("<p>&nbsp;</p><p>x</p>"));
    }

    [Fact]
    public void Drops_empty_headings_and_leaves_following_paragraph_as_paragraph()
    {
        var result = Normalize("<h4><strong>&nbsp;</strong></h4><p><strong>DIY green houses</strong></p>");

        Assert.Equal("<p><strong>DIY green houses</strong></p>", result);
    }

    [Fact]
    public void Collapses_consecutive_rules_into_one()
    {
        Assert.Equal("<hr><p>x</p>", Normalize("<hr><hr><p>x</p>"));
    }

    [Fact]
    public void Merges_adjacent_strong_tags()
    {
        Assert.Equal("<p><strong>26th</strong></p>", Normalize("<p><strong>26</strong><strong>th</strong></p>"));
    }

    [Fact]
    public void Does_not_merge_strong_tags_separated_by_a_link()
    {
        const string html = "<p><strong>a</strong><a href=\"x\"><strong>b</strong></a></p>";

        Assert.Equal(html, Normalize(html));
    }

    [Fact]
    public void Merges_adjacent_links_with_the_same_href_and_collapses_whitespace()
    {
        var result = Normalize("<p>due.<a href=\"http://due.At\">       </a><a href=\"http://due.At\">At</a> next</p>");

        Assert.Equal("<p>due.<a href=\"http://due.At\"> At</a> next</p>", result);
    }

    [Fact]
    public void Collapses_runs_of_spaces_but_preserves_nbsp()
    {
        Assert.Equal("<p>$25. Bed&nbsp;&nbsp;fee</p>", Normalize("<p>$25.  Bed&nbsp;&nbsp;fee</p>"));
    }

    [Fact]
    public void Trims_trailing_whitespace_inside_blocks()
    {
        Assert.Equal("<p>flower.</p>", Normalize("<p>flower. </p>"));
    }

    [Fact]
    public void Wraps_loose_text_between_blocks_in_a_paragraph()
    {
        Assert.Equal("<hr><p>Our Committee</p><p>x</p>", Normalize("<hr>Our Committee <p>x</p>"));
    }

    [Fact]
    public void Promotes_a_leading_heading_to_h2()
    {
        Assert.Equal("<h2>Lead</h2><h4>Other</h4>", Normalize("<h4>Lead</h4><h4>Other</h4>"));
    }

    [Fact]
    public void Leaves_a_leading_h2_and_later_headings_untouched()
    {
        const string html = "<h2>Lead</h2><h3>Section</h3><h4>Sub</h4>";

        Assert.Equal(html, Normalize(html));
    }

    [Fact]
    public void Groups_bullet_paragraphs_into_an_unordered_list()
    {
        var result = Normalize("<p>&#183;&nbsp;&nbsp;&nbsp; One</p><p>&#183;&nbsp; Two</p>");

        Assert.Equal("<ul><li>One</li><li>Two</li></ul>", result);
    }

    [Fact]
    public void Groups_numbered_paragraphs_into_an_ordered_list()
    {
        var result = Normalize("<p>1.&nbsp;&nbsp; One</p><p>2.&nbsp; Two</p>");

        Assert.Equal("<ol><li>One</li><li>Two</li></ol>", result);
    }

    [Fact]
    public void Preserves_inline_formatting_inside_list_items()
    {
        var result = Normalize("<p>&#183;&nbsp; <strong>Reminder</strong> - clean tools</p>");

        Assert.Equal("<ul><li><strong>Reminder</strong> - clean tools</li></ul>", result);
    }

    [Fact]
    public void Does_not_merge_list_runs_separated_by_a_divider()
    {
        var result = Normalize("<p>&#183;&nbsp; a</p><hr><p>&#183;&nbsp; b</p>");

        Assert.Equal("<ul><li>a</li></ul><hr><ul><li>b</li></ul>", result);
    }

    [Fact]
    public void Switches_list_kind_between_consecutive_runs()
    {
        var result = Normalize("<p>&#183;&nbsp; a</p><p>1.&nbsp; b</p>");

        Assert.Equal("<ul><li>a</li></ul><ol><li>b</li></ol>", result);
    }

    [Theory]
    [InlineData("<p>2026-27 was a good year</p>")]
    [InlineData("<p>30th of June is the deadline</p>")]
    [InlineData("<p>Just an ordinary paragraph.</p>")]
    public void Leaves_non_list_paragraphs_untouched(string html)
    {
        Assert.Equal(html, Normalize(html));
    }
}
