using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonGround.Server.Blog;

public static partial class BlogSlug
{
    public const int MaxLength = 160;

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidChars();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex Whitespace();

    public static string Derive(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";

        var normalized = title.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        ascii = InvalidChars().Replace(ascii, "");
        ascii = Whitespace().Replace(ascii, "-").Trim('-');

        return ascii.Length > MaxLength ? ascii[..MaxLength] : ascii;
    }

    public static async Task<string> ResolveAsync(
        string baseSlug,
        Func<string, CancellationToken, Task<bool>> exists,
        CancellationToken ct = default)
    {
        if (!await exists(baseSlug, ct)) return baseSlug;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{baseSlug}-{i}";
            if (!await exists(candidate, ct)) return candidate;
        }
        throw new InvalidOperationException($"Could not resolve unique slug for '{baseSlug}'.");
    }
}
