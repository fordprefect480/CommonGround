using System.Net;

namespace CommonGround.Server.Members;

public static class MembershipWelcomeEmail
{
    public const string Subject = "Welcome to Seaford Wetlands Community Garden";

    /// <summary>
    /// The message content as an HTML fragment, fed to the membership template's BODY variable
    /// (the template supplies the surrounding chrome). Resend renders the plain-text part.
    /// </summary>
    public static string BuildHtml(string? firstName, string? signInUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(firstName) ? "there" : WebUtility.HtmlEncode(firstName);
        var signInLink = string.IsNullOrWhiteSpace(signInUrl)
            ? ""
            : $"""<p><a href="{WebUtility.HtmlEncode(signInUrl)}">Sign in to your profile</a> any time to update your details or apply for a leased bed.</p>""";
        return $"""
            <h2 style="margin:0 0 16px;">Welcome, {greetingName}!</h2>
            <p>Thank you for becoming a member of Seaford Wetlands Community Garden. Your membership is now active.</p>
            <p>As a member you have access to the community garden, can pick from the communal spaces, are covered by our insurance, and can vote at our AGM.</p>
            {signInLink}
            <p>We look forward to seeing you in the garden.</p>
            """;
    }
}
