using CommonGround.Server.Activity;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommonGround.Server.Email;

public static class UnsubscribeEndpoint
{
    private enum UnsubscribeResult
    {
        InvalidToken,
        UserNotFound,
        AlreadyUnsubscribed,
        Unsubscribed,
    }

    public static void MapUnsubscribeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/unsubscribe", HandleGetAsync).AllowAnonymous();
        app.MapPost("/unsubscribe", HandlePostAsync).AllowAnonymous().DisableAntiforgery();
    }

    private static async Task<IResult> HandleGetAsync(
        [FromQuery] string? token,
        UnsubscribeTokenService tokens,
        AppDbContext db,
        IActivityLogger activityLogger,
        CancellationToken ct)
    {
        var (result, email) = await ApplyAsync(token, "email link", tokens, db, activityLogger, ct);

        return result switch
        {
            UnsubscribeResult.InvalidToken => Results.Content(RenderPage(
                title: "Link invalid",
                heading: "This unsubscribe link is invalid",
                message: "If you're still receiving emails you didn't expect, reply to one of them and we'll remove you manually."),
                contentType: "text/html"),

            UnsubscribeResult.UserNotFound => Results.Content(RenderPage(
                title: "Already removed",
                heading: "You're no longer subscribed",
                message: "We couldn't find an active account for this link. You won't receive further mailing-list emails."),
                contentType: "text/html"),

            _ => Results.Content(RenderPage(
                title: "Unsubscribed",
                heading: "You've been unsubscribed",
                message: $"{email} will no longer receive mailing-list emails. If this was a mistake, sign in to your account to resubscribe."),
                contentType: "text/html"),
        };
    }

    private static async Task<IResult> HandlePostAsync(
        [FromQuery] string? token,
        UnsubscribeTokenService tokens,
        AppDbContext db,
        IActivityLogger activityLogger,
        CancellationToken ct)
    {
        var (result, _) = await ApplyAsync(token, "one-click inbox unsubscribe", tokens, db, activityLogger, ct);

        return result switch
        {
            UnsubscribeResult.InvalidToken => Results.BadRequest(),
            _ => Results.NoContent(),
        };
    }

    private static async Task<(UnsubscribeResult Result, string? Email)> ApplyAsync(
        string? token,
        string sourceLabel,
        UnsubscribeTokenService tokens,
        AppDbContext db,
        IActivityLogger activityLogger,
        CancellationToken ct)
    {
        if (!tokens.TryDecodeUserId(token, out var userId))
        {
            return (UnsubscribeResult.InvalidToken, null);
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return (UnsubscribeResult.UserNotFound, null);
        }

        if (!user.IsSubscribedToMailingList)
        {
            return (UnsubscribeResult.AlreadyUnsubscribed, user.Email);
        }

        user.IsSubscribedToMailingList = false;
        await db.SaveChangesAsync(ct);

        await activityLogger.LogAsync(
            "email.unsubscribed",
            $"{user.Email} unsubscribed from the mailing list via {sourceLabel}",
            targetType: "user",
            targetId: user.Id,
            ct: ct);

        return (UnsubscribeResult.Unsubscribed, user.Email);
    }

    private static string RenderPage(string title, string heading, string message)
    {
        var safeTitle = System.Net.WebUtility.HtmlEncode(title);
        var safeHeading = System.Net.WebUtility.HtmlEncode(heading);
        var safeMessage = System.Net.WebUtility.HtmlEncode(message);

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>{{safeTitle}}</title>
              <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background: #F4EFE6; color: #2A2A2A; margin: 0; padding: 48px 16px; }
                .card { max-width: 520px; margin: 0 auto; background: #FFFFFF; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.04); padding: 40px 32px; }
                h1 { margin: 0 0 16px 0; font-size: 22px; color: #1F3A1B; }
                p { margin: 0; font-size: 15px; line-height: 22px; color: #4A4A4A; }
              </style>
            </head>
            <body>
              <div class="card">
                <h1>{{safeHeading}}</h1>
                <p>{{safeMessage}}</p>
              </div>
            </body>
            </html>
            """;
    }
}
