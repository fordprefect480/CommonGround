using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace CommonGround.Server.Blog.BlogImport;

/// <summary>
/// Tidies the HTML produced by <see cref="WixBlogClient"/> before it is sanitised and stored.
/// Wix exports carry layout noise (stray &lt;br&gt; spacers, empty paragraphs, doubled rules) and
/// fragments inline runs across adjacent tags. This pass removes the noise, merges the fragments,
/// normalises whitespace, and applies blog presentation conventions (medium image class, an h2 lead
/// heading). It is import-only and runs before the shared <see cref="BlogHtmlSanitizer"/>.
/// </summary>
public partial class BlogImportHtmlNormalizer
{
    private const string ImageClass = "blog-img-medium";

    [GeneratedRegex(@"[ \t\r\n]+")]
    private static partial Regex AsciiWhitespaceRun();

    public string Normalize(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        var body = document.Body!;

        RemoveLineBreaks(body);
        MergeAdjacentInline(body);
        CollapseWhitespace(body);
        WrapLooseText(document, body);
        TrimBlockEdges(body);
        RemoveEmptyBlocks(body);
        GroupLists(document, body);
        PromoteLeadHeading(document, body);
        CollapseConsecutiveRules(body);
        ApplyImageClass(body);

        return body.InnerHtml;
    }

    private static void RemoveLineBreaks(IElement root)
    {
        foreach (var br in root.QuerySelectorAll("br").ToArray())
            br.Remove();
    }

    private static void MergeAdjacentInline(IElement element)
    {
        var child = element.FirstElementChild;
        while (child is not null)
        {
            if (child.NextSibling is IElement sibling && AreMergeable(child, sibling))
            {
                while (sibling.FirstChild is { } moved)
                    child.AppendChild(moved);
                sibling.Remove();
                continue; // re-check the (now extended) child against its new next sibling
            }

            MergeAdjacentInline(child);
            child = child.NextElementSibling;
        }
    }

    private static bool AreMergeable(IElement first, IElement second)
    {
        if (!first.TagName.Equals(second.TagName, StringComparison.Ordinal))
            return false;

        return first.TagName switch
        {
            "STRONG" or "EM" => true,
            "A" => string.Equals(first.GetAttribute("href"), second.GetAttribute("href"), StringComparison.Ordinal),
            _ => false,
        };
    }

    private static void CollapseWhitespace(IElement root)
    {
        foreach (var text in DescendantTextNodes(root).ToList())
        {
            if (HasPreformattedAncestor(text)) continue;

            var collapsed = AsciiWhitespaceRun().Replace(text.TextContent, " ");
            if (!string.Equals(collapsed, text.TextContent, StringComparison.Ordinal))
                text.TextContent = collapsed;
        }
    }

    private static void WrapLooseText(IDocument document, IElement body)
    {
        foreach (var node in body.ChildNodes.ToArray())
        {
            if (node is not IText text) continue;

            if (IsBlank(text.TextContent))
            {
                text.Remove();
                continue;
            }

            var paragraph = document.CreateElement("p");
            body.ReplaceChild(paragraph, text);
            paragraph.AppendChild(text);
        }
    }

    private static void TrimBlockEdges(IElement root)
    {
        foreach (var block in root.QuerySelectorAll("p, h2, h3, h4, li, blockquote"))
        {
            if (block.FirstChild is IText first)
            {
                var trimmed = TrimStartAscii(first.TextContent);
                if (trimmed.Length == 0) first.Remove();
                else first.TextContent = trimmed;
            }

            if (block.LastChild is IText last)
            {
                var trimmed = TrimEndAscii(last.TextContent);
                if (trimmed.Length == 0) last.Remove();
                else last.TextContent = trimmed;
            }
        }
    }

    private static void RemoveEmptyBlocks(IElement root)
    {
        foreach (var block in root.QuerySelectorAll("p, h2, h3, h4").ToArray())
        {
            if (block.QuerySelector("img") is null && IsBlank(block.TextContent))
                block.Remove();
        }
    }

    private enum ListKind { None, Unordered, Ordered }

    // Wix exports list rows as separate paragraphs prefixed with a literal marker ("· " or "1. ").
    // Group each run of same-kind paragraphs into a real <ul>/<ol>, moving inline content into <li>
    // and stripping the marker plus its trailing spacing.
    private static void GroupLists(IDocument document, IElement body)
    {
        var child = body.FirstElementChild;
        while (child is not null)
        {
            var kind = ClassifyListItem(child);
            if (kind == ListKind.None)
            {
                child = child.NextElementSibling;
                continue;
            }

            var items = new List<IElement>();
            var cursor = child;
            while (cursor is not null && ClassifyListItem(cursor) == kind)
            {
                items.Add(cursor);
                cursor = cursor.NextElementSibling;
            }

            var list = document.CreateElement(kind == ListKind.Ordered ? "ol" : "ul");
            child.Parent!.InsertBefore(list, child);

            foreach (var paragraph in items)
            {
                var item = document.CreateElement("li");
                while (paragraph.FirstChild is { } moved)
                    item.AppendChild(moved);
                StripListMarker(item, kind);
                list.AppendChild(item);
                paragraph.Remove();
            }

            child = cursor;
        }
    }

    private static ListKind ClassifyListItem(IElement element)
    {
        if (element.TagName != "P") return ListKind.None;

        var text = element.TextContent;
        var i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        if (i >= text.Length) return ListKind.None;

        if (IsBulletMarker(text[i]))
            return FollowedByGapOrEnd(text, i + 1) ? ListKind.Unordered : ListKind.None;

        if (char.IsDigit(text[i]))
        {
            var j = i;
            while (j < text.Length && char.IsDigit(text[j])) j++;
            if (j < text.Length && (text[j] == '.' || text[j] == ')') && FollowedByGapOrEnd(text, j + 1))
                return ListKind.Ordered;
        }

        return ListKind.None;
    }

    private static void StripListMarker(IElement item, ListKind kind)
    {
        if (DescendantTextNodes(item).FirstOrDefault() is not { } text) return;

        var s = text.TextContent;
        var i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;

        if (kind == ListKind.Ordered)
        {
            while (i < s.Length && char.IsDigit(s[i])) i++;
            if (i < s.Length && (s[i] == '.' || s[i] == ')')) i++;
        }
        else if (i < s.Length && IsBulletMarker(s[i]))
        {
            i++;
        }

        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        text.TextContent = s[i..];
    }

    private static bool IsBulletMarker(char c) =>
        c is '·' or '•' or '∙' or '◦' or '▪' or '●' or '‣' or '⁃';

    private static bool FollowedByGapOrEnd(string text, int index) =>
        index >= text.Length || char.IsWhiteSpace(text[index]);

    private static void PromoteLeadHeading(IDocument document, IElement body)
    {
        if (body.FirstElementChild is not { } first) return;
        if (first.TagName is not ("H3" or "H4")) return;

        var heading = document.CreateElement("h2");
        while (first.FirstChild is { } moved)
            heading.AppendChild(moved);
        first.Parent!.ReplaceChild(heading, first);
    }

    private static void CollapseConsecutiveRules(IElement body)
    {
        IElement? previous = null;
        foreach (var element in body.Children.ToArray())
        {
            if (element.TagName == "HR" && previous?.TagName == "HR")
            {
                element.Remove();
                continue;
            }

            previous = element;
        }
    }

    private static void ApplyImageClass(IElement root)
    {
        foreach (var img in root.QuerySelectorAll("img"))
            img.SetAttribute("class", ImageClass);
    }

    private static IEnumerable<IText> DescendantTextNodes(INode node)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child is IText text) yield return text;
            else foreach (var descendant in DescendantTextNodes(child)) yield return descendant;
        }
    }

    private static bool HasPreformattedAncestor(INode node)
    {
        for (var parent = node.ParentElement; parent is not null; parent = parent.ParentElement)
            if (parent.TagName is "PRE" or "CODE") return true;
        return false;
    }

    // char.IsWhiteSpace treats U+00A0 (&nbsp;) as whitespace, so an nbsp-only block counts as blank.
    private static bool IsBlank(string value)
    {
        foreach (var c in value)
            if (!char.IsWhiteSpace(c)) return false;
        return true;
    }

    private static string TrimStartAscii(string value)
    {
        var start = 0;
        while (start < value.Length && value[start] is ' ' or '\t' or '\r' or '\n') start++;
        return value[start..];
    }

    private static string TrimEndAscii(string value)
    {
        var end = value.Length;
        while (end > 0 && value[end - 1] is ' ' or '\t' or '\r' or '\n') end--;
        return value[..end];
    }
}
