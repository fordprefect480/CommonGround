using System.Text.RegularExpressions;

namespace CommonGround.Server.Instagram;

public static class InstagramEmbedSanitizer
{
    private static readonly Regex BlockquoteRegex = new(
        @"<blockquote\b[^>]*?class\s*=\s*[""'][^""']*\binstagram-media\b[^""']*[""'][^>]*>.*?</blockquote>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex PermalinkRegex = new(
        @"data-instgrm-permalink\s*=\s*[""'](?<url>https?://(?:www\.)?instagram\.com/[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryExtract(string? raw, out string sanitized, out string? error)
    {
        sanitized = "";
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Paste the Instagram embed snippet (from the post's ⋯ → Embed menu).";
            return false;
        }

        var match = BlockquoteRegex.Match(raw);
        if (!match.Success)
        {
            error = "Couldn't find an Instagram embed in the snippet. Make sure you copied the full Embed code from Instagram.";
            return false;
        }

        var blockquote = match.Value;

        if (!PermalinkRegex.IsMatch(blockquote))
        {
            error = "The snippet is missing a valid Instagram permalink.";
            return false;
        }

        sanitized = blockquote;
        return true;
    }
}
