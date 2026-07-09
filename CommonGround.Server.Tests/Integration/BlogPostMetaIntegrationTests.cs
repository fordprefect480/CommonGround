using CommonGround.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommonGround.Server.Tests.Integration;

public class BlogPostMetaIntegrationTests
{
    // A trimmed index.html carrying the default tags the injector rewrites.
    private const string IndexHtml = """
        <!doctype html>
        <html lang="en"><head>
        <meta name="description" content="Default site description." />
        <link rel="canonical" href="https://seafordwetlandscommunitygarden.com/" />
        <meta property="og:image" content="https://seafordwetlandscommunitygarden.com/swcg/hero-image.png" />
        <title>Seaford Wetlands Community Garden</title>
        </head><body><div id="root"></div></body></html>
        """;

    /// <summary>Extends the shared factory with a temp web root containing index.html.</summary>
    private sealed class WebRootFactory : TestApiFactory
    {
        public string WebRoot { get; } = Directory.CreateTempSubdirectory("cg-webroot-").FullName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            File.WriteAllText(Path.Combine(WebRoot, "index.html"), IndexHtml);
            builder.UseWebRoot(WebRoot);
            base.ConfigureWebHost(builder);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(WebRoot))
            {
                Directory.Delete(WebRoot, recursive: true);
            }
        }
    }

    /// <summary>Extends the shared factory with an empty temp web root (no index.html), so <see cref="IndexHtmlProvider"/> returns null.</summary>
    private sealed class EmptyWebRootFactory : TestApiFactory
    {
        public string WebRoot { get; } = Directory.CreateTempSubdirectory("cg-webroot-empty-").FullName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseWebRoot(WebRoot);
            base.ConfigureWebHost(builder);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(WebRoot))
            {
                Directory.Delete(WebRoot, recursive: true);
            }
        }
    }

    private static int SeedPost(IServiceProvider services, string slug, string title, bool published, bool withImage)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        int? imageId = null;
        if (withImage)
        {
            var image = new BlogImage { ContentType = "image/png", Bytes = [1, 2, 3], CreatedAt = DateTime.UtcNow };
            db.BlogImages.Add(image);
            db.SaveChanges();
            imageId = image.Id;
        }

        var post = new BlogPost
        {
            Slug = slug,
            Title = title,
            Excerpt = "An excerpt.",
            BodyHtml = "<p>Body.</p>",
            AuthorName = "Author",
            Status = published ? BlogPostStatus.Published : BlogPostStatus.Draft,
            PublishedAt = published ? DateTime.UtcNow : null,
            FeaturedImageId = imageId,
        };
        db.BlogPosts.Add(post);
        db.SaveChanges();
        return imageId ?? 0;
    }

    [Fact]
    public async Task Published_post_with_image_gets_injected_og_image_and_title()
    {
        using var factory = new WebRootFactory();
        var client = factory.CreateClient();
        var imageId = SeedPost(factory.Services, "spring-planting", "Spring Planting", published: true, withImage: true);

        var html = await client.GetStringAsync("/blog/spring-planting");

        Assert.Contains($"/api/blog/images/{imageId}", html);
        Assert.Contains("<title>Spring Planting | Seaford Wetlands Community Garden</title>", html);
        Assert.DoesNotContain("hero-image.png", html);
    }

    [Fact]
    public async Task Unknown_slug_returns_default_html_unchanged()
    {
        using var factory = new WebRootFactory();
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/blog/does-not-exist");

        Assert.Contains("hero-image.png", html);
        Assert.Contains("<title>Seaford Wetlands Community Garden</title>", html);
    }

    [Fact]
    public async Task Draft_post_is_not_injected()
    {
        using var factory = new WebRootFactory();
        var client = factory.CreateClient();
        SeedPost(factory.Services, "draft-post", "Draft Post", published: false, withImage: true);

        var html = await client.GetStringAsync("/blog/draft-post");

        Assert.Contains("hero-image.png", html);
        Assert.DoesNotContain("Draft Post", html);
    }

    [Fact]
    public async Task Missing_index_html_returns_not_found()
    {
        using var factory = new EmptyWebRootFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/blog/anything");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }
}
