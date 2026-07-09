using CommonGround.Server.Misc;

namespace CommonGround.Server.Tests;

public class BlogPostMetaTagsTests
{
    private const string SiteUrl = "https://seafordwetlandscommunitygarden.com";
    private const string GardenName = "Seaford Wetlands Community Garden";

    // Mirrors the relevant head tags from frontend/index.html, including the
    // multi-line <meta name="description">, og:description, and twitter:description
    // formatting, so the injector is proven against the real shape of the file.
    private const string Fixture = """
        <!doctype html>
        <html lang="en">
          <head>
            <meta charset="UTF-8" />
            <meta name="robots" content="index, follow" />
            <meta
              name="description"
              content="Default site description."
            />
            <link rel="canonical" href="https://seafordwetlandscommunitygarden.com/" />
            <meta property="og:site_name" content="Seaford Wetlands Community Garden" />
            <meta property="og:type" content="website" />
            <meta property="og:url" content="https://seafordwetlandscommunitygarden.com/" />
            <meta property="og:title" content="Seaford Wetlands Community Garden" />
            <meta
              property="og:description"
              content="Default site description."
            />
            <meta property="og:image" content="https://seafordwetlandscommunitygarden.com/swcg/hero-image.png" />
            <meta name="twitter:card" content="summary_large_image" />
            <meta name="twitter:title" content="Seaford Wetlands Community Garden" />
            <meta
              name="twitter:description"
              content="Default site description."
            />
            <meta name="twitter:image" content="https://seafordwetlandscommunitygarden.com/swcg/hero-image.png" />
            <title>Seaford Wetlands Community Garden</title>
          </head>
          <body><div id="root"></div></body>
        </html>
        """;

    private static string Inject(BlogPostMetaTags.PostMeta post) =>
        BlogPostMetaTags.Inject(Fixture, post, SiteUrl, GardenName);

    [Fact]
    public void Injects_featured_image_into_og_and_twitter_image()
    {
        var html = Inject(new("Spring Planting", "How we sowed.", "spring-planting", 42));

        Assert.Contains(""""<meta property="og:image" content="https://seafordwetlandscommunitygarden.com/api/blog/images/42"""", html);
        Assert.Contains(""""<meta name="twitter:image" content="https://seafordwetlandscommunitygarden.com/api/blog/images/42"""", html);
        Assert.DoesNotContain("hero-image.png", html);
    }

    [Fact]
    public void Injects_title_description_url_and_type()
    {
        var html = Inject(new("Spring Planting", "How we sowed.", "spring-planting", 42));

        Assert.Contains("<title>Spring Planting | Seaford Wetlands Community Garden</title>", html);
        Assert.Contains(""""<meta name="description" content="How we sowed."""", html);
        Assert.Contains(""""<meta property="og:title" content="Spring Planting | Seaford Wetlands Community Garden"""", html);
        Assert.Contains(""""<meta property="og:description" content="How we sowed."""", html);
        Assert.Contains(""""<meta property="og:type" content="article"""", html);
        Assert.Contains(""""<meta property="og:url" content="https://seafordwetlandscommunitygarden.com/blog/spring-planting"""", html);
        Assert.Contains(""""<link rel="canonical" href="https://seafordwetlandscommunitygarden.com/blog/spring-planting"""", html);
    }

    [Fact]
    public void Without_featured_image_leaves_default_image_but_injects_text()
    {
        var html = Inject(new("No Image Post", "Body text.", "no-image", null));

        Assert.Contains("https://seafordwetlandscommunitygarden.com/swcg/hero-image.png", html);
        Assert.Contains("<title>No Image Post | Seaford Wetlands Community Garden</title>", html);
        Assert.Contains(""""<meta property="og:title" content="No Image Post | Seaford Wetlands Community Garden"""", html);
    }

    [Fact]
    public void Html_encodes_title_and_description()
    {
        var html = Inject(new("Tom & Jerry <b>", "A & B < C", "tom-jerry", 1));

        Assert.Contains("Tom &amp; Jerry &lt;b&gt; | Seaford Wetlands Community Garden", html);
        Assert.Contains(""""content="A &amp; B &lt; C"""", html);
        Assert.DoesNotContain("Tom & Jerry <b>", html);
    }

    [Fact]
    public void Preserves_unrelated_meta_tags()
    {
        var html = Inject(new("Spring Planting", "How we sowed.", "spring-planting", 42));

        Assert.Contains("""<meta property="og:site_name" content="Seaford Wetlands Community Garden" />""", html);
        Assert.Contains("""<meta name="twitter:card" content="summary_large_image" />""", html);
        Assert.Contains("""<meta name="robots" content="index, follow" />""", html);
    }

    [Fact]
    public void Rewrites_the_default_tags_in_the_real_index_html()
    {
        // Drift guard: the fixtures above are hand-copied, so they can't catch a
        // reformat of the real frontend/index.html that stops the regexes matching.
        // Running Inject against the actual file fails loudly here instead of the
        // site silently unfurling with stale default tags.
        var html = BlogPostMetaTags.Inject(
            ReadRealIndexHtml(),
            new("Real File Post", "Real excerpt.", "real-file-post", 999),
            SiteUrl,
            GardenName);

        Assert.Contains("/api/blog/images/999", html);
        Assert.DoesNotContain("hero-image.png", html); // both og:image and twitter:image rewritten
        Assert.Contains("<title>Real File Post | Seaford Wetlands Community Garden</title>", html);
        Assert.Contains(""""<meta property="og:type" content="article"""", html);
        Assert.Contains($"{SiteUrl}/blog/real-file-post", html); // og:url + canonical rewritten
    }

    private static string ReadRealIndexHtml()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "frontend", "index.html")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, "Could not locate frontend/index.html by walking up from the test output directory.");
        return File.ReadAllText(Path.Combine(dir!.FullName, "frontend", "index.html"));
    }
}
