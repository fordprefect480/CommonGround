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

public sealed class AdminInstagramGroup : SubGroup<AdminGroup>
{
    public AdminInstagramGroup()
    {
        Configure("instagram", _ => { });
    }
}

public sealed class PublicInstagramGroup : Group
{
    public PublicInstagramGroup()
    {
        Configure("instagram", ep => ep.AllowAnonymous());
    }
}

public sealed class AdminEventsGroup : SubGroup<AdminGroup>
{
    public AdminEventsGroup()
    {
        Configure("events", _ => { });
    }
}

public sealed class AdminLeasedBedsGroup : SubGroup<AdminGroup>
{
    public AdminLeasedBedsGroup()
    {
        Configure("leased-beds", _ => { });
    }
}

public sealed class PublicEventsGroup : Group
{
    public PublicEventsGroup()
    {
        Configure("events", ep => ep.AllowAnonymous());
    }
}
