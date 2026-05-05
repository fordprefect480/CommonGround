namespace CommonGround.Server.Data;

public class BlogImage
{
    public int Id { get; set; }
    public string ContentType { get; set; } = "";
    public byte[] Bytes { get; set; } = [];
    public string? OriginalFileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UploadedByUserId { get; set; }
    public ApplicationUser? UploadedBy { get; set; }
}
