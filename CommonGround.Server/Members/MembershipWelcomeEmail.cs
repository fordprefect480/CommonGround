using System.Net;

namespace CommonGround.Server.Members;

public static class MembershipWelcomeEmail
{
    public const string Subject = "Welcome to Seaford Wetlands Community Garden";

    public static string BuildHtml(string? displayName, string? signInUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(displayName) ? "there" : WebUtility.HtmlEncode(displayName);
        var signInLink = string.IsNullOrWhiteSpace(signInUrl)
            ? ""
            : $"""<p><a href="{WebUtility.HtmlEncode(signInUrl)}">Sign in to your profile</a> any time to update your details.</p>""";
        return $$"""
            <!doctype html>
            <html><body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2A2A2A;">
              <h2 style="margin:0 0 16px;">Welcome, {{greetingName}}!</h2>
              <p>Thank you for becoming a member of Seaford Wetlands Community Garden. Your membership is now active.</p>
              <p>As a member you have access to the community garden, can pick from the communal spaces, are covered by our insurance, and can vote at our AGM.</p>
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
            : $"\nSign in to your profile any time to update your details: {signInUrl}\n";
        return $"Welcome, {greetingName}!\n\nThank you for becoming a member of Seaford Wetlands Community Garden. Your membership is now active.\n{signInLine}\nWe look forward to seeing you in the garden.\n";
    }
}
