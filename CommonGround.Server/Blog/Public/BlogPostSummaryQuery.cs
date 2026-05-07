using CommonGround.Server.Data;

namespace CommonGround.Server.Blog.Public;

internal record SummaryRow(
    int Id,
    string Slug,
    string Title,
    string? Excerpt,
    string AuthorName,
    DateTime? PublishedAt,
    string? CategoryName,
    int? FeaturedImageId,
    string BodyHead);

internal static class BlogPostSummaryQuery
{
    public const int BodyHeadChars = 1000;

    public static IQueryable<SummaryRow> PublishedSummary(IQueryable<BlogPost> source) =>
        source
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new SummaryRow(
                p.Id, p.Slug, p.Title, p.Excerpt, p.AuthorName,
                p.PublishedAt,
                p.Category != null ? p.Category.Name : null,
                p.FeaturedImageId,
                p.BodyHtml.Length > BodyHeadChars ? p.BodyHtml.Substring(0, BodyHeadChars) : p.BodyHtml));

    public static BlogPostSummaryDto ToSummaryDto(SummaryRow r) => new(
        r.Id, r.Slug, r.Title,
        r.Excerpt ?? BlogExcerpt.FromHtml(r.BodyHead),
        r.AuthorName, r.PublishedAt, r.CategoryName, r.FeaturedImageId);
}
