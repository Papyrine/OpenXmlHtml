static class AtPageParser
{
    internal static AtPageLayout? Parse(string html)
    {
        var body = FindPageRule(html);
        if (body == null)
        {
            return null;
        }

        var declarations = StyleParser.Parse(body);

        var layout = new AtPageLayout();

        if (declarations.TryGetValue("size", out var size))
        {
            ParseSize(size, layout);
        }

        if (declarations.TryGetValue("margin", out var margin))
        {
            var (t, r, b, l) = StyleParser.ParseMarginShorthand(margin);
            layout.MarginTopTwips = t;
            layout.MarginRightTwips = r;
            layout.MarginBottomTwips = b;
            layout.MarginLeftTwips = l;
        }

        if (declarations.TryGetValue("margin-top", out var mt))
        {
            layout.MarginTopTwips = StyleParser.ParseLengthToTwips(mt);
        }

        if (declarations.TryGetValue("margin-right", out var mr))
        {
            layout.MarginRightTwips = StyleParser.ParseLengthToTwips(mr);
        }

        if (declarations.TryGetValue("margin-bottom", out var mb))
        {
            layout.MarginBottomTwips = StyleParser.ParseLengthToTwips(mb);
        }

        if (declarations.TryGetValue("margin-left", out var ml))
        {
            layout.MarginLeftTwips = StyleParser.ParseLengthToTwips(ml);
        }

        if (declarations.TryGetValue("column-count", out var columnCount) &&
            int.TryParse(columnCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cc) &&
            cc > 0)
        {
            layout.ColumnCount = cc;
        }
        else if (declarations.TryGetValue("columns", out var columns))
        {
            // "columns" shorthand - look for a bare integer token
            foreach (var part in columns.Split(' ', '\t'))
            {
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) &&
                    count > 0)
                {
                    layout.ColumnCount = count;
                    break;
                }
            }
        }

        if (layout.IsEmpty)
        {
            return null;
        }

        return layout;
    }

    // size is a page size, an orientation, or both in either order: "A4", "landscape",
    // "A4 landscape", "landscape A4", "8.5in 11in". Only an orientation trailing a named size used
    // to be read, so the commonest way to ask for a landscape page — "size: landscape" by itself —
    // was ignored.
    static void ParseSize(string size, AtPageLayout layout)
    {
        var landscape = false;
        int? namedWidth = null;
        int? namedHeight = null;
        int? firstLength = null;
        int? secondLength = null;

        foreach (var token in size.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("landscape", StringComparison.OrdinalIgnoreCase))
            {
                landscape = true;
            }
            else if (token.Equals("portrait", StringComparison.OrdinalIgnoreCase))
            {
                landscape = false;
            }
            else if (TryParseNamedSize(token.AsSpan(), out var width, out var height))
            {
                namedWidth = width;
                namedHeight = height;
            }
            else if (StyleParser.ParseLengthToTwips(token) is { } length)
            {
                if (firstLength == null)
                {
                    firstLength = length;
                }
                else
                {
                    secondLength ??= length;
                }
            }
        }

        // An explicit width and height say which way round the page is by themselves.
        if (firstLength != null &&
            secondLength != null)
        {
            layout.WidthTwips = firstLength;
            layout.HeightTwips = secondLength;
            layout.Landscape = landscape;
            return;
        }

        // A named size is portrait. An orientation with no size turns the default page, A4.
        if (namedWidth == null)
        {
            if (!landscape)
            {
                return;
            }

            namedWidth = 11906;
            namedHeight = 16838;
        }

        if (landscape)
        {
            (namedWidth, namedHeight) = (namedHeight, namedWidth);
            layout.Landscape = true;
        }

        layout.WidthTwips = namedWidth;
        layout.HeightTwips = namedHeight;
    }

    // The declarations of the @page rule that applies to every page, or null when there is none.
    //
    // Only stylesheets are searched. The text "@page" anywhere in the html used to count, and the
    // next brace after it, however far on, was taken for its block: a paragraph mentioning
    // "@pagerduty" ahead of an unrelated "p { margin: 0 }" set the page margins to zero.
    static string? FindPageRule(string html)
    {
        var position = 0;
        while (true)
        {
            var open = html.IndexOf("<style", position, StringComparison.OrdinalIgnoreCase);
            if (open < 0)
            {
                return null;
            }

            var start = html.IndexOf('>', open);
            if (start < 0)
            {
                return null;
            }

            start++;
            var end = html.IndexOf("</style", start, StringComparison.OrdinalIgnoreCase);
            if (end < 0)
            {
                end = html.Length;
            }

            if (FindPageRule(html, start, end) is { } body)
            {
                return body;
            }

            position = end;
        }
    }

    static string? FindPageRule(string html, int start, int end)
    {
        var position = start;
        while (position < end)
        {
            var at = html.IndexOf("@page", position, end - position, StringComparison.OrdinalIgnoreCase);
            if (at < 0)
            {
                return null;
            }

            var selectorStart = at + "@page".Length;
            var brace = html.IndexOf('{', selectorStart, end - selectorStart);
            if (brace < 0)
            {
                return null;
            }

            // A rule with a selector is for some pages only: "@page :first" for the first, a named
            // page for those that ask for it. A document here has one page setup, so the rule that
            // counts is the one with nothing between @page and its block. Taking whichever came
            // first gave every page the first page's margins.
            if (html.AsSpan(selectorStart, brace - selectorStart).Trim().IsEmpty)
            {
                return ExtractBlockBody(html, brace);
            }

            position = selectorStart;
        }

        return null;
    }

    static bool TryParseNamedSize(ReadOnlySpan<char> name, out int width, out int height)
    {
        // Sizes in twips (portrait orientation)
        if (name.Equals("A4", StringComparison.OrdinalIgnoreCase))
        {
            width = 11906;
            height = 16838;
            return true;
        }

        if (name.Equals("A3", StringComparison.OrdinalIgnoreCase))
        {
            width = 16838;
            height = 23811;
            return true;
        }

        if (name.Equals("A5", StringComparison.OrdinalIgnoreCase))
        {
            width = 8391;
            height = 11906;
            return true;
        }

        if (name.Equals("Letter", StringComparison.OrdinalIgnoreCase))
        {
            width = 12240;
            height = 15840;
            return true;
        }

        if (name.Equals("Legal", StringComparison.OrdinalIgnoreCase))
        {
            width = 12240;
            height = 20160;
            return true;
        }

        if (name.Equals("Tabloid", StringComparison.OrdinalIgnoreCase))
        {
            width = 15840;
            height = 24480;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }

    // Extracts the top-level declarations of the @page block starting at the given '{'.
    // Nested margin-box rules (e.g. @top-center { ... }) and their selectors are skipped
    // so sibling declarations such as margin/size are not lost. Returns null if unbalanced.
    static string? ExtractBlockBody(string html, int braceStart)
    {
        var builder = new StringBuilder();
        var depth = 0;
        for (var i = braceStart; i < html.Length; i++)
        {
            var current = html[i];
            if (current == '{')
            {
                if (depth == 1)
                {
                    // Drop the buffered selector that precedes this nested block.
                    builder.Length = LastSeparatorIndex(builder) + 1;
                }

                depth++;
                continue;
            }

            if (current == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return builder.ToString();
                }

                continue;
            }

            if (depth == 1)
            {
                builder.Append(current);
            }
        }

        return null;
    }

    static int LastSeparatorIndex(StringBuilder builder)
    {
        for (var i = builder.Length - 1; i >= 0; i--)
        {
            if (builder[i] == ';')
            {
                return i;
            }
        }

        return -1;
    }
}
