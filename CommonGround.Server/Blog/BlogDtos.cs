namespace CommonGround.Server.Blog;

public record BlogPostSummaryDto(
    int Id,
    string Slug,
    string Title,
    string? Excerpt,
    string AuthorName,
    DateTime? PublishedAt,
    string? CategoryName,
    int? FeaturedImageId);

public record BlogPostDto(
    int Id,
    string Slug,
    string Title,
    string? Excerpt,
    string BodyHtml,
    string AuthorName,
    DateTime? PublishedAt,
    string? CategoryName,
    int? FeaturedImageId,
    IReadOnlyList<BlogPostSummaryDto> MorePosts);

public record BlogPostAdminListItemDto(
    int Id,
    string Slug,
    string Title,
    string AuthorName,
    int? CategoryId,
    int? FeaturedImageId,
    int Status,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Excerpt);

public record BlogPostAdminDto(
    int Id,
    string Slug,
    string Title,
    string? Excerpt,
    string BodyHtml,
    string AuthorName,
    int? CategoryId,
    int? FeaturedImageId,
    int Status,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record BlogPostWriteDto(
    string Title,
    string? Slug,
    string? Excerpt,
    string BodyHtml,
    int? CategoryId,
    int? FeaturedImageId,
    int Status);

public record BlogCategoryDto(int Id, string Name, string Slug);

public record BlogImageUploadDto(int Id, string Url);

public record ImportBlogResultDto(
    int Imported,
    int Skipped,
    int Failed,
    IReadOnlyList<ImportBlogErrorDto> Errors);

public record ImportBlogErrorDto(string? Slug, string Message);

public record ImportBlogProgressDto(string Phase, int Current, int Total, string? Slug);

public record BlogPostListDto(
    IReadOnlyList<BlogPostSummaryDto> Posts,
    int Page,
    int PageSize,
    int Total);
