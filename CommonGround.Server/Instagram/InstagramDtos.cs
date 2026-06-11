namespace CommonGround.Server.Instagram;

public record InstagramPostDto(
    int Id,
    string EmbedHtml,
    int DisplayOrder);

public record InstagramPostAdminDto(
    int Id,
    string EmbedHtml,
    int DisplayOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record InstagramPostWriteDto(
    string EmbedHtml,
    int? DisplayOrder);
