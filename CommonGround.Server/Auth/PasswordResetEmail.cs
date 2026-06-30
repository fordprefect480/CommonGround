using System.Net;

namespace CommonGround.Server.Auth;

public static class PasswordResetEmail
{
    public static string Subject(string gardenName) =>
        string.IsNullOrWhiteSpace(gardenName)
            ? "Reset your password"
            : $"Reset your {gardenName} password";

    /// <summary>
    /// The message content as an HTML fragment, fed to the membership template's BODY variable
    /// (the template supplies the surrounding chrome). Resend renders the plain-text part.
    /// </summary>
    public static string BuildHtml(string? displayName, string resetUrl)
    {
        var greetingName = string.IsNullOrWhiteSpace(displayName) ? "there" : WebUtility.HtmlEncode(displayName);
        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        return $"""
            <h2 style="margin:0 0 16px;">Reset your password</h2>
            <p>Hi {greetingName},</p>
            <p>We received a request to reset the password for your account. Click the button below to choose a new one. This link will expire after a short while.</p>
            <p style="margin:24px 0;">
              <a href="{safeUrl}" style="display:inline-block; background:#1F3A1B; color:#FFFFFF; text-decoration:none; padding:12px 20px; border-radius:8px;">Reset password</a>
            </p>
            <p style="font-size:13px; color:#4A4A4A;">If the button doesn't work, copy and paste this link into your browser:<br><a href="{safeUrl}">{safeUrl}</a></p>
            <p>If you didn't request a password reset, you can safely ignore this email - your password won't change.</p>
            """;
    }
}
