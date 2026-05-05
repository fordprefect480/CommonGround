namespace CommonGround.Server.Data;

public class BlogPost
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Excerpt { get; set; }
    public string BodyHtml { get; set; } = "";
    public string AuthorName { get; set; } = "";

    public int? CategoryId { get; set; }
    public BlogCategory? Category { get; set; }

    public int? FeaturedImageId { get; set; }
    public BlogImage? FeaturedImage { get; set; }

    public BlogPostStatus Status { get; set; } = BlogPostStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}
