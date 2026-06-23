namespace CommonGround.Server.Events;

/// <summary>
/// Item shown in the public "Coming up" strip. May originate from a manual
/// CommunityEvent record or from the Eventbrite organizer feed.
/// </summary>
public record UpcomingEventDto(
    string Id,
    string Source,
    string Title,
    string Body,
    DateTime StartUtc,
    DateTime? EndUtc,
    string? Url,
    string Tone,
    string? ImageUrl);

public record CommunityEventAdminDto(
    int Id,
    string Title,
    DateTime StartUtc,
    DateTime? EndUtc,
    string Body,
    string? Url,
    string Tone,
    int DisplayOrder,
    int? FeaturedImageId,
    string? ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CommunityEventWriteDto(
    string Title,
    DateTime StartUtc,
    DateTime? EndUtc,
    string Body,
    string? Url,
    int? FeaturedImageId,
    string? Tone,
    int? DisplayOrder,
    string? RepeatFrequency,
    DateOnly? RepeatUntil);
