namespace CommonGround.Server.Activity;

public sealed record ActivityListDto(
    IReadOnlyList<ActivityItemDto> Items,
    string? NextCursor);

public sealed record ActivityItemDto(
    long Id,
    DateTime OccurredAt,
    string ActivityType,
    string? ActorUserId,
    string? ActorEmail,
    string? ActorName,
    string Summary,
    string? TargetType,
    string? TargetId,
    string? DetailsJson);
