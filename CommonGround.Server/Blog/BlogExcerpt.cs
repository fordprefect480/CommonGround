using System.Text.RegularExpressions;

namespace CommonGround.Server.Blog;

public static partial class BlogExcerpt
{
    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    public static string FromHtml(string? html, int maxChars)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var stripped = Tags().Replace(html, " ");
        var collapsed = Whitespace().Replace(stripped, " ").Trim();
        var decoded = System.Net.WebUtility.HtmlDecode(collapsed);
        if (decoded.Length <= maxChars) return decoded;

        var clipped = decoded[..maxChars];
        var lastSpace = clipped.LastIndexOf(' ');
        if (lastSpace > maxChars / 2) clipped = clipped[..lastSpace];
        return clipped.TrimEnd() + "…";
    }
}
