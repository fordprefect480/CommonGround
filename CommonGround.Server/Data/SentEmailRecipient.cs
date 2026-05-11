namespace CommonGround.Server.Data;

public enum SentEmailRecipientStatus
{
    Sent = 0,
    Failed = 1,
}

public class SentEmailRecipient
{
    public long Id { get; set; }
    public long SentEmailId { get; set; }
    public SentEmail SentEmail { get; set; } = null!;
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Email { get; set; } = "";
    public SentEmailRecipientStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
