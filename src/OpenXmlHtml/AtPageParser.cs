static class AtPageParser
{
    internal static AtPageLayout? Parse(string html)
    {
        var atIndex = html.IndexOf("@page", StringComparison.OrdinalIgnoreCase);
        if (atIndex < 0)
        {
            return null;
        }

        var braceStart = html.IndexOf('{', atIndex);
        if (braceStart < 0)
        {
            return null;
        }

        var body = ExtractBlockBody(html, braceStart);
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
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c) &&
                    c > 0)
                {
                    layout.ColumnCount = c;
                    break;
                }
            }
        }

        return layout.IsEmpty ? null : layout;
    }

    static void ParseSize(string size, AtPageLayout layout)
    {
        var span = size.AsSpan().Trim();
        var landscape = false;
        var portrait = false;

        // Strip orientation tokens
        if (TryStripTrailingToken(ref span, "landscape"))
        {
            landscape = true;
        }
        else if (TryStripTrailingToken(ref span, "portrait"))
        {
            portrait = true;
        }

        var trimmed = span.Trim();

        // Try a named page size
        if (TryParseNamedSize(trimmed, out var width, out var height))
        {
            // Named sizes are portrait by default
            if (landscape)
            {
                (width, height) = (height, width);
                layout.Landscape = true;
            }
            else if (portrait)
            {
                layout.Landscape = false;
            }

            layout.WidthTwips = width;
            layout.HeightTwips = height;
            return;
        }

        // Try "<width> <height>"
        var spaceIdx = -1;
        for (var i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] == ' ')
            {
                spaceIdx = i;
                break;
            }
        }

        if (spaceIdx > 0)
        {
            var w = StyleParser.ParseLengthToTwips(trimmed[..spaceIdx]);
            var h = StyleParser.ParseLengthToTwips(trimmed[(spaceIdx + 1)..]);
            if (w != null && h != null)
            {
                layout.WidthTwips = w;
                layout.HeightTwips = h;
                if (landscape)
                {
                    layout.Landscape = true;
                }
            }
        }
    }

    static bool TryStripTrailingToken(ref ReadOnlySpan<char> span, string token)
    {
        var trimmed = span.TrimEnd();
        if (trimmed.EndsWith(token.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
            (trimmed.Length == token.Length ||
             trimmed[trimmed.Length - token.Length - 1] == ' '))
        {
            span = trimmed[..^token.Length].TrimEnd();
            return true;
        }

        return false;
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
