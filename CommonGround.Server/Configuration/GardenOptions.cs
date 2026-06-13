namespace CommonGround.Server.Configuration;

public class GardenOptions
{
    public const string SectionName = "Garden";

    public string Name { get; set; } = "";

    /// <summary>
    /// Public-facing base URL (scheme + host, no trailing slash) used when constructing
    /// links embedded in outgoing emails - e.g. unsubscribe URLs. When unset, the URL is
    /// derived from the current request, which is incorrect behind a reverse proxy that
    /// terminates TLS or rewrites the host.
    /// </summary>
    public string? PublicUrl { get; set; }
}
