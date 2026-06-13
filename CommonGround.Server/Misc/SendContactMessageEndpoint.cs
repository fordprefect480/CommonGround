using System.Net;
using CommonGround.Server.Activity;
using CommonGround.Server.Configuration;
using CommonGround.Server.Email;
using FastEndpoints;
using Microsoft.Extensions.Options;
using Resend;

namespace CommonGround.Server.Misc;

public sealed class SendContactMessageEndpoint(
    IResend resend,
    IOptions<EmailOptions> emailOptions,
    IOptions<ContactOptions> contactOptions,
    TurnstileVerifier turnstile,
    IHttpContextAccessor httpContextAccessor,
    IActivityLogger activityLogger,
    ILogger<SendContactMessageEndpoint> logger)
    : Endpoint<SendContactMessageEndpoint.Request, SendContactMessageEndpoint.Result>
{
    public sealed record Request(string Name, string Email, string Subject, string Message, string? CaptchaToken);
    public sealed record Result(bool Sent);

    private const int MaxFieldLength = 2000;
    private const int MaxMessageLength = 5000;

    public override void Configure()
    {
        Post("/contact");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        if (!emailOptions.Value.IsConfigured)
        {
            await Send.ResultAsync(Results.Problem(
                title: "Email not configured",
                detail: "The contact form is temporarily unavailable.",
                statusCode: 503));
            return;
        }

        var contact = contactOptions.Value;
        if (!contact.IsConfigured)
        {
            await Send.ResultAsync(Results.Problem(
                title: "Contact recipient not configured",
                detail: "The contact form is temporarily unavailable.",
                statusCode: 503));
            return;
        }

        var name = Sanitize(req.Name, MaxFieldLength);
        var email = Sanitize(req.Email, MaxFieldLength);
        var subject = Sanitize(req.Subject, MaxFieldLength);
        var message = Sanitize(req.Message, MaxMessageLength);

        if (name.Length == 0 || email.Length == 0 || subject.Length == 0 || message.Length == 0)
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "All fields are required." }));
            return;
        }

        if (!LooksLikeEmail(email))
        {
            await Send.ResultAsync(Results.BadRequest(new { error = "Please enter a valid email address." }));
            return;
        }

        if (contact.CaptchaEnabled)
        {
            var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            var ok = await turnstile.VerifyAsync(contact.TurnstileSecretKey!, req.CaptchaToken ?? "", remoteIp, ct);
            if (!ok)
            {
                await Send.ResultAsync(Results.BadRequest(new { error = "Captcha verification failed. Please try again." }));
                return;
            }
        }

        var html = BuildHtml(name, email, subject, message);
        var text = BuildText(name, email, subject, message);

        var emailMessage = new EmailMessage
        {
            From = emailOptions.Value.From,
            Subject = $"[Contact form] {subject}",
            HtmlBody = html,
            TextBody = text,
            ReplyTo = email,
        };
        emailMessage.To.Add(contact.RecipientAddress);

        try
        {
            await resend.EmailSendAsync(emailMessage, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deliver contact form submission to {Recipient}", contact.RecipientAddress);
            await Send.ResultAsync(Results.Problem(
                title: "Could not send message",
                detail: "Sorry - something went wrong sending your message. Please try again later.",
                statusCode: 502));
            return;
        }

        await activityLogger.LogAsync(
            "contact.submitted",
            $"Contact form submission from {email} - \"{subject}\"",
            details: new { Name = name, Email = email, Subject = subject },
            ct: ct);

        await Send.OkAsync(new Result(true), ct);
    }

    private static string Sanitize(string? value, int maxLength)
    {
        var trimmed = (value ?? "").Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0 && at < value.Length - 1 && !value.Contains(' ');
    }

    private static string BuildHtml(string name, string email, string subject, string message)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeEmail = WebUtility.HtmlEncode(email);
        var safeSubject = WebUtility.HtmlEncode(subject);
        var safeMessage = WebUtility.HtmlEncode(message).Replace("\n", "<br>");
        return $$"""
            <!doctype html>
            <html><body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; color: #2A2A2A;">
              <h2 style="margin:0 0 16px;">New contact form submission</h2>
              <p><strong>From:</strong> {{safeName}} &lt;<a href="mailto:{{safeEmail}}">{{safeEmail}}</a>&gt;</p>
              <p><strong>Subject:</strong> {{safeSubject}}</p>
              <hr>
              <p>{{safeMessage}}</p>
            </body></html>
            """;
    }

    private static string BuildText(string name, string email, string subject, string message) =>
        $"New contact form submission\n\nFrom: {name} <{email}>\nSubject: {subject}\n\n{message}\n";
}
