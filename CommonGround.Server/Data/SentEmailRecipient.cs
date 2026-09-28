namespace CommonGround.Server.Data;

public enum SentEmailRecipientStatus
{
    Sent = 0,
    Failed = 1,

    /// <summary>Waiting for daily sending allowance; the queue worker delivers it later.</summary>
    Queued = 2,

    /// <summary>Dropped from the queue before delivery (the member unsubscribed in the meantime).</summary>
    Skipped = 3,
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

    /// <summary>When Resend accepted the message; drives the rolling daily-allowance count.</summary>
    public DateTime? SentAtUtc { get; set; }
}
