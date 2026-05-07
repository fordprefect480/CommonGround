using FastEndpoints;

namespace CommonGround.Server.Auth;

public sealed class AdminGroup : Group
{
    public AdminGroup()
    {
        Configure("admin", ep => ep.Roles(AppRoles.Admin));
    }
}

public sealed class AdminBlogGroup : SubGroup<AdminGroup>
{
    public AdminBlogGroup()
    {
        Configure("blog", _ => { });
    }
}

public sealed class AdminToolsGroup : SubGroup<AdminGroup>
{
    public AdminToolsGroup()
    {
        Configure("tools", _ => { });
    }
}

public sealed class PublicBlogGroup : Group
{
    public PublicBlogGroup()
    {
        Configure("blog", ep => ep.AllowAnonymous());
    }
}
