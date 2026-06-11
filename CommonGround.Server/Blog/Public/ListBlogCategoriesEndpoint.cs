using CommonGround.Server.Auth;
using CommonGround.Server.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Blog.Public;

public sealed class ListBlogCategoriesEndpoint(AppDbContext db)
    : EndpointWithoutRequest<List<BlogCategoryDto>>
{
    public override void Configure()
    {
        Get("/categories");
        Group<PublicBlogGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var categories = await db.BlogCategories
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new BlogCategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);
        await Send.OkAsync(categories, ct);
    }
}
