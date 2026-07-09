namespace CommonGround.Server.Misc;

/// <summary>
/// Supplies the built SPA shell (<c>wwwroot/index.html</c>), read once and cached
/// for the app's lifetime. Returns <c>null</c> when the file is absent — the case
/// in local dev, where the SPA is served by the Vite dev server instead.
/// </summary>
public sealed class IndexHtmlProvider
{
    private readonly string? _html;

    public IndexHtmlProvider(IWebHostEnvironment env) => _html = Read(env);

    public string? GetHtml() => _html;

    private static string? Read(IWebHostEnvironment env)
    {
        var file = env.WebRootFileProvider.GetFileInfo("index.html");
        if (!file.Exists)
        {
            return null;
        }

        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
