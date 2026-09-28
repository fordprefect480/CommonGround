using CommonGround.Server.Data;

namespace CommonGround.Server.Email;

public static class SentEmailRecipientStatusNames
{
    /// <summary>The lower-case status name the admin API reports for a recipient.</summary>
    public static string ToApiName(this SentEmailRecipientStatus status) => status switch
    {
        SentEmailRecipientStatus.Sent => "sent",
        SentEmailRecipientStatus.Failed => "failed",
        SentEmailRecipientStatus.Queued => "queued",
        SentEmailRecipientStatus.Skipped => "skipped",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
