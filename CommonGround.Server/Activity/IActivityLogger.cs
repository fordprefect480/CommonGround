namespace CommonGround.Server.Activity;

public interface IActivityLogger
{
    Task LogAsync(
        string activityType,
        string summary,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        CancellationToken ct = default);
}
