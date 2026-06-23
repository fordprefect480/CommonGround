namespace CommonGround.Server.Data;

public class CommunityEvent
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public DateTime StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    public string Body { get; set; } = "";

    public string? Url { get; set; }

    public string? Location { get; set; }

    /// <summary>
    /// Visual tone for the placeholder card image when no logo is provided.
    /// One of: leaf, apple, flesh.
    /// </summary>
    public string Tone { get; set; } = "leaf";

    public int DisplayOrder { get; set; }

    public int? FeaturedImageId { get; set; }
    public BlogImage? FeaturedImage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}
