namespace CommonGround.Server.Data;

public class InstagramPost
{
    public int Id { get; set; }

    public string EmbedHtml { get; set; } = "";

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
}
