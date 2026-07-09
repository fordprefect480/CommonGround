using System.Net;

namespace CommonGround.Server.LeasedBeds;

/// <summary>Transactional email bodies for the leased-bed flow. Mirrors the membership email style.</summary>
public static class LeasedBedEmails
{
    public const string AppliedSubject = "New leased bed application";
    public const string WaitlistSubject = "New leased bed waitlist entry";
    public const string AssignedSubject = "You've been assigned a garden bed";

    public static string BuildAppliedText(string memberName, int remaining, string publicUrl, bool requiresWheelchairAccessible) =>
        $"{memberName} has applied for one of the {remaining} remaining beds.{WheelchairLineText(requiresWheelchairAccessible)}\nPlease assign them a bed on the Leased Beds page: {Enc(publicUrl)}admin/leased-beds";

    public static string BuildAppliedHtml(string memberName, int remaining, string publicUrl, bool requiresWheelchairAccessible) =>
        Wrap($"<p>{Enc(memberName)} has applied for one of the {remaining} remaining beds.</p>{WheelchairLineHtml(requiresWheelchairAccessible)}<p>Please assign them a bed on the <a href=\"{Enc(publicUrl)}admin/leased-beds\">Leased Beds page</a>.</p>");

    public static string BuildWaitlistedText(string memberName, int total, string publicUrl, bool requiresWheelchairAccessible) =>
        $"{memberName} has joined the waitlist for a leased bed. There are now {total} members on the waitlist.{WheelchairLineText(requiresWheelchairAccessible)}\nYou can view the current waitlist on the Leased Beds page: {Enc(publicUrl)}admin/leased-beds";

    public static string BuildWaitlistedHtml(string memberName, int total, string publicUrl, bool requiresWheelchairAccessible) =>
        Wrap($"<p>{Enc(memberName)} has joined the waitlist for a leased bed. There are now {total} members on the waitlist.</p>{WheelchairLineHtml(requiresWheelchairAccessible)}<p>You can view the current waitlist on the <a href=\"{Enc(publicUrl)}admin/leased-beds\">Leased Beds page</a>.</p>");

    private static string WheelchairLineText(bool required) =>
        required ? " This member needs a wheelchair-accessible bed." : "";

    private static string WheelchairLineHtml(bool required) =>
        required ? "<p><strong>This member needs a wheelchair-accessible bed.</strong></p>" : "";

    /// <summary>
    /// The member assignment notice as an HTML fragment, fed to the membership template's BODY
    /// variable (the template supplies the surrounding chrome). Resend renders the plain-text part.
    /// </summary>
    public static string BuildAssignmentHtml(string firstName, string bedLabel, string fyLabel, DateOnly expiresOn, string? amount, string? profileUrl)
    {
        var expiry = Enc(expiresOn.ToString("d MMM yyyy"));
        var label = Enc(bedLabel);
        var fy = Enc(fyLabel);
        if (amount is null)
        {
            return $"""
                <h2 style="margin:0 0 16px;">Hi {Enc(firstName)},</h2>
                <p>You've been assigned bed <strong>{label}</strong> for the {fy} financial year (expires {expiry}).</p>
                <p>No payment is required &mdash; your lease is confirmed.</p>
                <p>We look forward to seeing you in the garden.</p>
                """;
        }
        var payLink = profileUrl is null
            ? $"Please complete your payment of {Enc(amount)} from your profile to confirm it."
            : $"""Please complete your payment of {Enc(amount)} from <a href="{Enc(profileUrl)}">your profile</a> to confirm it.""";
        return $"""
            <h2 style="margin:0 0 16px;">Hi {Enc(firstName)},</h2>
            <p>You've been assigned bed <strong>{label}</strong> for the {fy} financial year (expires {expiry}).</p>
            <p>{payLink}</p>
            """;
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    private static string Wrap(string inner) => $"""
        <!doctype html>
        <html><body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2A2A2A;">
        {inner}
        </body></html>
        """;
}
