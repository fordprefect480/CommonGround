using System.Net;

namespace CommonGround.Server.Members;

/// <summary>
/// Sent when an admin creates a member account directly (rather than the member
/// signing up and paying themselves). It confirms the account exists and points
/// the recipient at the sign-in page, without claiming a paid membership is active.
/// </summary>
public static class MemberAccountCreatedEmail
{
    public const string Subject = "Your Seaford Wetlands Community Garden account";

    public static string BuildHtml(string? displayName, string? signInUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(displayName) ? "there" : WebUtility.HtmlEncode(displayName);
        var signInLink = string.IsNullOrWhiteSpace(signInUrl)
            ? ""
            : $"""<p><a href="{WebUtility.HtmlEncode(signInUrl)}">Sign in to your profile</a> to review and update your details.</p>""";
        return $$"""
            <!doctype html>
            <html><body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2A2A2A;">
              <h2 style="margin:0 0 16px;">Hello, {{greetingName}}!</h2>
              <p>An account has been created for you at Seaford Wetlands Community Garden.</p>
              <p>You can sign in with the email address this message was sent to. If you weren't given a password, use the "forgot password" link on the sign-in page to set one.</p>
              {{signInLink}}
              <p>We look forward to seeing you in the garden.</p>
            </body></html>
            """;
    }

    public static string BuildText(string? displayName, string? signInUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var signInLine = string.IsNullOrWhiteSpace(signInUrl)
            ? ""
            : $"\nSign in to your profile to review and update your details: {signInUrl}\n";
        return $"Hello, {greetingName}!\n\nAn account has been created for you at Seaford Wetlands Community Garden.\n\nYou can sign in with the email address this message was sent to. If you weren't given a password, use the \"forgot password\" link on the sign-in page to set one.\n{signInLine}\nWe look forward to seeing you in the garden.\n";
    }
}
