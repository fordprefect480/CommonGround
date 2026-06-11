using CommonGround.Server.Data;

namespace CommonGround.Server.Instagram;

internal static class InstagramMapping
{
    public static InstagramPostAdminDto ToAdminDto(InstagramPost p) => new(
        p.Id, p.EmbedHtml, p.DisplayOrder, p.CreatedAt, p.UpdatedAt);

    public static InstagramPostDto ToPublicDto(InstagramPost p) => new(
        p.Id, p.EmbedHtml, p.DisplayOrder);
}
