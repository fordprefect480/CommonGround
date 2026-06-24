using CommonGround.Server.Configuration;
using CommonGround.Server.Data;
using CommonGround.Server.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Auth;

/// <summary>
/// Bridges the built-in ASP.NET Core Identity API password-reset flow to the
/// app's Resend-based email sending. The Identity API's <c>/forgotPassword</c>
/// endpoint generates a reset code and calls <see cref="SendPasswordResetCodeAsync"/>;
/// we turn that code into a link back to the SPA's reset page and email it.
/// </summary>
public sealed class IdentityEmailSender(
    IResend resend,
    IOptions<EmailOptions> emailOptions,
    IOptions<GardenOptions> gardenOptions,
    IHttpContextAccessor httpContextAccessor,
    ILogger<IdentityEmailSender> logger)
    : IEmailSender<ApplicationUser>
{
    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var baseUrl = ResolvePublicBaseUrl();
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&code={Uri.EscapeDataString(resetCode)}";

        var gardenName = gardenOptions.Value.Name;

        if (!emailOptions.Value.IsConfigured)
        {
            // No outbound email provider configured (e.g. local dev). Surface the
            // link in the logs so the flow can still be exercised end-to-end.
            logger.LogInformation(
                "Email is not configured; password reset link for {Email} not sent. Link: {ResetUrl}",
                email, resetUrl);
            return;
        }

        var message = new EmailMessage
        {
            From = emailOptions.Value.From,
            Subject = PasswordResetEmail.Subject(gardenName),
            HtmlBody = PasswordResetEmail.BuildHtml(user.DisplayName, resetUrl),
            TextBody = PasswordResetEmail.BuildText(user.DisplayName, resetUrl),
        };
        message.To.Add(email);

        try
        {
            await resend.EmailSendAsync(message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", email);
        }
    }

    // Confirmation-link and reset-link flows aren't used by this app: members
    // are created pre-confirmed by admins, and the reset flow above is code-based.
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        Task.CompletedTask;

    private string ResolvePublicBaseUrl()
    {
        var configured = gardenOptions.Value.PublicUrl;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.TrimEnd('/');
        }

        var request = httpContextAccessor.HttpContext?.Request;
        return request is not null ? $"{request.Scheme}://{request.Host}" : "";
    }
}
