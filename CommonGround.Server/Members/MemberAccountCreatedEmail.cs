using System.Net;

namespace CommonGround.Server.Members;

/// <summary>
/// Sent when an admin creates a member account directly (rather than the member
/// signing up and paying themselves). It confirms the account exists and prompts
/// the recipient to sign in and pay for their membership from their profile page,
/// since the admin path records no payment.
/// </summary>
public static class MemberAccountCreatedEmail
{
    public const string Subject = "Your Seaford Wetlands Community Garden account";

    public static string BuildHtml(string? displayName, string? profileUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(displayName) ? "there" : WebUtility.HtmlEncode(displayName);
        var payLine = string.IsNullOrWhiteSpace(profileUrl)
            ? "<p>To activate your membership, sign in and pay for it from your profile page. If you weren't given a password, use the \"forgot password\" link on the sign-in page to set one.</p>"
            : $"""<p>To activate your membership, <a href="{WebUtility.HtmlEncode(profileUrl)}">sign in and pay for it from your profile page</a>. If you weren't given a password, use the "forgot password" link on the sign-in page to set one.</p>""";
        return $$"""
            <!doctype html>
            <html><body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2A2A2A;">
              <h2 style="margin:0 0 16px;">Hello, {{greetingName}}!</h2>
              <p>An account has been created for you at Seaford Wetlands Community Garden.</p>
              {{payLine}}
              <p>We look forward to seeing you in the garden.</p>
            </body></html>
            """;
    }

    public static string BuildText(string? displayName, string? profileUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var payLine = string.IsNullOrWhiteSpace(profileUrl)
            ? "To activate your membership, sign in and pay for it from your profile page. If you weren't given a password, use the \"forgot password\" link on the sign-in page to set one."
            : $"To activate your membership, sign in and pay for it from your profile page: {profileUrl}\nIf you weren't given a password, use the \"forgot password\" link on the sign-in page to set one.";
        return $"Hello, {greetingName}!\n\nAn account has been created for you at Seaford Wetlands Community Garden.\n\n{payLine}\n\nWe look forward to seeing you in the garden.\n";
    }
}
