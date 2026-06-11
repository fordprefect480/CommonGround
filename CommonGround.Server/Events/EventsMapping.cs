using CommonGround.Server.Data;

namespace CommonGround.Server.Events;

internal static class EventsMapping
{
    private static readonly string[] Tones = ["leaf", "apple", "flesh"];

    public static readonly IReadOnlySet<string> AllowedTones =
        new HashSet<string>(Tones, StringComparer.OrdinalIgnoreCase);

    public const string DefaultTone = "leaf";

    public static string NormalizeTone(string? tone) =>
        tone is not null && AllowedTones.Contains(tone) ? tone.ToLowerInvariant() : DefaultTone;

    public static string? BuildImageUrl(int? imageId) =>
        imageId is { } id ? $"/api/blog/images/{id}" : null;

    public static CommunityEventAdminDto ToAdminDto(CommunityEvent e) => new(
        e.Id, e.Title, e.StartUtc, e.EndUtc, e.Body, e.Url, e.Tone, e.DisplayOrder,
        e.FeaturedImageId, BuildImageUrl(e.FeaturedImageId),
        e.CreatedAt, e.UpdatedAt);

    public static UpcomingEventDto ToUpcomingDto(CommunityEvent e) => new(
        Id: $"manual-{e.Id}",
        Source: "manual",
        Title: e.Title,
        Body: e.Body,
        StartUtc: e.StartUtc,
        EndUtc: e.EndUtc,
        Url: e.Url,
        Tone: e.Tone,
        ImageUrl: BuildImageUrl(e.FeaturedImageId));

    public static UpcomingEventDto? ToUpcomingDto(EventbriteEvent e, int orderIndex)
    {
        if (e.Start?.Utc is not DateTime startUtc) return null;
        var name = e.Name?.Text;
        if (string.IsNullOrWhiteSpace(name)) return null;

        var body = !string.IsNullOrWhiteSpace(e.Summary) ? e.Summary : e.Description?.Text ?? "";
        var tone = Tones[Math.Abs(orderIndex) % Tones.Length];

        return new UpcomingEventDto(
            Id: $"eventbrite-{e.Id}",
            Source: "eventbrite",
            Title: name,
            Body: TrimBody(body),
            StartUtc: DateTime.SpecifyKind(startUtc, DateTimeKind.Utc),
            EndUtc: e.End?.Utc is { } endUtc ? DateTime.SpecifyKind(endUtc, DateTimeKind.Utc) : null,
            Url: e.Url,
            Tone: tone,
            ImageUrl: e.Logo?.Original?.Url ?? e.Logo?.Url);
    }

    private static string TrimBody(string text)
    {
        if (text.Length <= 240) return text;
        var cut = text[..240];
        var lastSpace = cut.LastIndexOf(' ');
        return (lastSpace > 120 ? cut[..lastSpace] : cut).TrimEnd() + "…";
    }
}
