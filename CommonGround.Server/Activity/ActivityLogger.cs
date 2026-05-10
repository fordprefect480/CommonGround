using System.Text.Json;
using CommonGround.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace CommonGround.Server.Activity;

public sealed class ActivityLogger(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor,
    UserManager<ApplicationUser> userManager,
    ILogger<ActivityLogger> logger)
    : IActivityLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(
        string activityType,
        string summary,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        CancellationToken ct = default)
    {
        try
        {
            string? actorUserId = null;
            string? actorEmail = null;

            var http = httpContextAccessor.HttpContext;
            if (http?.User is { Identity.IsAuthenticated: true })
            {
                var user = await userManager.GetUserAsync(http.User);
                if (user is not null)
                {
                    actorUserId = user.Id;
                    actorEmail = user.Email;
                }
            }

            var entry = new Data.Activity
            {
                OccurredAt = DateTime.UtcNow,
                ActivityType = activityType,
                ActorUserId = actorUserId,
                ActorEmailSnapshot = actorEmail,
                Summary = summary,
                TargetType = targetType,
                TargetId = targetId,
                DetailsJson = details is null ? null : JsonSerializer.Serialize(details, JsonOptions),
            };

            db.Activities.Add(entry);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to record activity {ActivityType} for {Summary}",
                activityType, summary);
        }
    }
}
