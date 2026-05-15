namespace CommonGround.Server.Data;

public class SentEmail
{
    public long Id { get; set; }
    public DateTime SentAt { get; set; }
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string TextBody { get; set; } = "";
    public string? SenderUserId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public string? SenderEmailSnapshot { get; set; }
    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public ICollection<SentEmailRecipient> Recipients { get; set; } = new List<SentEmailRecipient>();
}
