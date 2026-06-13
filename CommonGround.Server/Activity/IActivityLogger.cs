namespace CommonGround.Server.Activity;

public interface IActivityLogger
{
    /// <summary>
    /// Records an activity entry. When the action is performed by an authenticated user,
    /// the logger prefixes <paramref name="summary"/> with the actor's display name, so
    /// for those cases <paramref name="summary"/> should be a plain-English predicate
    /// (e.g. "added a new Instagram tile (#3)" becomes "Owen Symes added a new Instagram tile (#3)").
    /// For anonymous/public actions there is no actor, so pass a complete sentence
    /// (e.g. "owen@example.com subscribed to the mailing list").
    /// </summary>
    Task LogAsync(
        string activityType,
        string summary,
        string? targetType = null,
        string? targetId = null,
        object? details = null,
        CancellationToken ct = default);
}
