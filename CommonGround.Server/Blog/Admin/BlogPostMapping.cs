using CommonGround.Server.Data;

namespace CommonGround.Server.Blog.Admin;

internal static class BlogPostMapping
{
    public const int SqlUniqueConstraintError = 2627;
    public const int SqlUniqueIndexError = 2601;

    public static BlogPostAdminDto ToAdminDto(BlogPost p) => new(
        p.Id, p.Slug, p.Title, p.Excerpt, p.BodyHtml, p.AuthorName,
        p.CategoryId, p.FeaturedImageId, (int)p.Status,
        p.PublishedAt, p.CreatedAt, p.UpdatedAt);

    public static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public static bool IsUniqueSlugViolation(Microsoft.EntityFrameworkCore.DbUpdateException ex) =>
        ex.InnerException is Microsoft.Data.SqlClient.SqlException sql &&
        (sql.Number == SqlUniqueConstraintError || sql.Number == SqlUniqueIndexError);
}
