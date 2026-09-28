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

    /// <summary>
    /// True for bulk newsletters (members can opt out; sent with an unsubscribe link and
    /// List-Unsubscribe headers). False for transactional/membership mail and admin notices.
    /// </summary>
    public bool IsNewsletter { get; set; }

    public int RecipientCount { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    /// <summary>Recipients still waiting for daily sending allowance. Non-zero means the email is still going out.</summary>
    public int QueuedCount { get; set; }

    /// <summary>
    /// Public site origin captured at send time, so queued newsletters delivered later by the
    /// background worker (which has no HTTP request) can still build unsubscribe links.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    public ICollection<SentEmailRecipient> Recipients { get; set; } = new List<SentEmailRecipient>();
}
