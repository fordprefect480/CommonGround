using System.Net;

namespace CommonGround.Server.LeasedBeds;

/// <summary>Transactional email bodies for the leased-bed flow. Mirrors the membership email style.</summary>
public static class LeasedBedEmails
{
    public const string AppliedSubject = "New leased bed application";
    public const string WaitlistSubject = "New leased bed waiting-list entry";
    public const string AssignedSubject = "You've been assigned a garden bed";

    public static string BuildAppliedText(string memberName, int remaining) =>
        $"{memberName} has applied for one of the {remaining} remaining beds.\n";

    public static string BuildAppliedHtml(string memberName, int remaining) =>
        Wrap($"<p>{Enc(memberName)} has applied for one of the {remaining} remaining beds.</p>");

    public static string BuildWaitlistedText(string memberName, int total) =>
        $"{memberName} has joined the waiting list. There are now {total} members on the waiting list.\n";

    public static string BuildWaitlistedHtml(string memberName, int total) =>
        Wrap($"<p>{Enc(memberName)} has joined the waiting list. There are now {total} members on the waiting list.</p>");

    public static string BuildAssignmentText(string memberName, string bedLabel, string fyLabel, DateOnly expiresOn, string? amount, string? profileUrl)
    {
        var expiry = expiresOn.ToString("d MMM yyyy");
        if (amount is null)
        {
            return $"Hi {memberName},\n\nYou've been assigned bed {bedLabel} for the {fyLabel} financial year (expires {expiry}). No payment is required - your lease is confirmed.\n\nWe look forward to seeing you in the garden.\n";
        }
        var payLine = profileUrl is null
            ? $"Please complete your payment of {amount} from your profile to confirm it."
            : $"Please complete your payment of {amount} from your profile to confirm it: {profileUrl}";
        return $"Hi {memberName},\n\nYou've been assigned bed {bedLabel} for the {fyLabel} financial year (expires {expiry}). {payLine}\n";
    }

    public static string BuildAssignmentHtml(string memberName, string bedLabel, string fyLabel, DateOnly expiresOn, string? amount, string? profileUrl)
    {
        var expiry = Enc(expiresOn.ToString("d MMM yyyy"));
        var label = Enc(bedLabel);
        var fy = Enc(fyLabel);
        if (amount is null)
        {
            return Wrap($"""
                <h2 style="margin:0 0 16px;">Hi {Enc(memberName)},</h2>
                <p>You've been assigned bed <strong>{label}</strong> for the {fy} financial year (expires {expiry}).</p>
                <p>No payment is required &mdash; your lease is confirmed.</p>
                <p>We look forward to seeing you in the garden.</p>
                """);
        }
        var payLink = profileUrl is null
            ? $"Please complete your payment of {Enc(amount)} from your profile to confirm it."
            : $"""Please complete your payment of {Enc(amount)} from <a href="{Enc(profileUrl)}">your profile</a> to confirm it.""";
        return Wrap($"""
            <h2 style="margin:0 0 16px;">Hi {Enc(memberName)},</h2>
            <p>You've been assigned bed <strong>{label}</strong> for the {fy} financial year (expires {expiry}).</p>
            <p>{payLink}</p>
            """);
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    private static string Wrap(string inner) => $"""
        <!doctype html>
        <html><body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2A2A2A;">
        {inner}
        </body></html>
        """;
}
