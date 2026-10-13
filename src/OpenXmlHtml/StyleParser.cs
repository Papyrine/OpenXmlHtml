static class StyleParser
{
    internal static Dictionary<string, string> Parse(string? style)
    {
        var result = new Dictionary<string, string>(8, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(style))
        {
            return result;
        }

        var remaining = style.AsSpan();
        while (remaining.Length > 0)
        {
            var semiIndex = remaining.IndexOf(';');
            var declaration = semiIndex >= 0 ? remaining[..semiIndex] : remaining;
            remaining = semiIndex >= 0 ? remaining[(semiIndex + 1)..] : default;

            declaration = declaration.Trim();
            if (declaration.IsEmpty)
            {
                continue;
            }

            var colonIndex = declaration.IndexOf(':');
            if (colonIndex <= 0)
            {
                continue;
            }

            var property = declaration[..colonIndex].Trim();
            var value = StripImportant(declaration[(colonIndex + 1)..].Trim());
            if (!property.IsEmpty && !value.IsEmpty)
            {
                result[property.ToString()] = value.ToString();
            }
        }

        return result;
    }

    // !important says how a declaration ranks against others, and nothing about its value. Left on,
    // it made every value unreadable to the parsers downstream — "red !important" is not a colour —
    // so the declaration was dropped whole. Inline styles written for email clients carry it on
    // nearly everything.
    static ReadOnlySpan<char> StripImportant(ReadOnlySpan<char> value)
    {
        var bang = value.LastIndexOf('!');
        if (bang < 0 ||
            !value[(bang + 1)..].Trim().Equals("important", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return value[..bang].TrimEnd();
    }

    // font-family is a list of fallbacks, and a Word run names one font. Handed the whole list,
    // Word looked for a font literally called "Arial, Helvetica, sans-serif", found none and
    // substituted its own. The first family is the one the author wanted, so that is the one named.
    // A generic keyword is not a font: monospace has an obvious stand-in, and the rest say nothing
    // Word's own default does not already say.
    internal static string? ParseFontFamily(string value)
    {
        var span = value.AsSpan();
        var comma = span.IndexOf(',');
        if (comma >= 0)
        {
            span = span[..comma];
        }

        span = span.Trim().Trim('\'').Trim('"').Trim();
        if (span.IsEmpty)
        {
            return null;
        }

        if (span.Equals("monospace", StringComparison.OrdinalIgnoreCase))
        {
            return "Courier New";
        }

        if (span.Equals("serif", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("sans-serif", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("cursive", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("fantasy", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("system-ui", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("inherit", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("initial", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("unset", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return span.ToString();
    }

    // The point size html's seven font sizes stand for, 3 being the default.
    static double[] htmlFontSizes = [7.5, 10, 12, 13.5, 18, 24, 36];

    // <font size> is a step on a scale of 1 to 7, or a signed step away from 3. Read as points, the
    // sizes real html carries came out unreadable: size="4" was 4pt and size="+1" was 1pt. A value
    // past 7 has no meaning on that scale, so it keeps the reading it has always had here, a size
    // in points, which is what callers passing size="14" expect.
    internal static double? ParseFontSizeAttribute(string value)
    {
        var span = value.AsSpan().Trim();
        if (span.IsEmpty)
        {
            return null;
        }

        var relative = span[0] is '+' or '-';
        if (!double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return null;
        }

        if (relative)
        {
            return htmlFontSizes[(int)Math.Clamp(3 + number, 1, 7) - 1];
        }

        if (number is >= 1 and <= 7 &&
            number == Math.Floor(number))
        {
            return htmlFontSizes[(int)number - 1];
        }

        if (number <= 0)
        {
            return null;
        }

        return number;
    }

    // The limits Word's schema puts on the measures css can reach. A css length is unbounded and may
    // be negative, and written through as it stood it produced attributes the schema rejects:
    // w:before="-150" from a negative margin, w:sz="-7" from a negative font size, a row height
    // wrapped round to four billion by a cast to unsigned.
    internal const double MaxFontSizePt = 1638;
    internal const int MaxTwips = 31680;

    internal static int ClampSignedTwips(int twips) =>
        Math.Clamp(twips, -MaxTwips, MaxTwips);

    // For a measure that cannot be negative. css allows a negative margin to pull content back;
    // Word has no such thing, and the nearest it can say is none at all.
    internal static int ClampUnsignedTwips(int twips) =>
        Math.Clamp(twips, 0, MaxTwips);

    internal static double? ParseFontSize(string value)
    {
        var span = value.AsSpan().Trim();

        if (TryParseSuffix(span, "pt", out var pt))
        {
            return pt;
        }

        if (TryParseSuffix(span, "px", out var px))
        {
            return px * 0.75;
        }

        if (TryParseSuffix(span, "em", out var em))
        {
            return em * 12;
        }

        if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
        {
            return raw;
        }

        if (span.Equals("xx-small", StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        if (span.Equals("x-small", StringComparison.OrdinalIgnoreCase))
        {
            return 8;
        }

        if (span.Equals("small", StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (span.Equals("medium", StringComparison.OrdinalIgnoreCase))
        {
            return 12;
        }

        if (span.Equals("large", StringComparison.OrdinalIgnoreCase))
        {
            return 14;
        }

        if (span.Equals("x-large", StringComparison.OrdinalIgnoreCase))
        {
            return 18;
        }

        if (span.Equals("xx-large", StringComparison.OrdinalIgnoreCase))
        {
            return 24;
        }

        return null;
    }

    internal static WidthValue? ParseWidth(string value) =>
        ParseWidth(value.AsSpan());

    // Like ParseLengthToTwips, but keeps a percentage as a percentage instead of failing to parse
    // it. Word measures pct widths in fiftieths of a percent, so 35% is 1750 and 100% is 5000.
    internal static WidthValue? ParseWidth(ReadOnlySpan<char> value)
    {
        var span = value.Trim();
        if (span.Length > 1 &&
            span[^1] == '%')
        {
            if (double.TryParse(span[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) &&
                percent >= 0)
            {
                return new((int)Math.Round(percent * 50), WidthUnit.Percent);
            }

            return null;
        }

        var twips = ParseLengthToTwips(span);
        if (twips == null)
        {
            return null;
        }

        return new(twips.Value, WidthUnit.Twips);
    }

    internal static int? ParseLengthToTwips(string value) =>
        ParseLengthToTwips(value.AsSpan());

    internal static int? ParseLengthToTwips(ReadOnlySpan<char> value)
    {
        var span = value.Trim();

        // 1pt = 20 twips
        if (TryParseSuffix(span, "pt", out var pt))
        {
            return (int)Math.Round(pt * 20);
        }

        // 1px ≈ 0.75pt = 15 twips
        if (TryParseSuffix(span, "px", out var px))
        {
            return (int)Math.Round(px * 15);
        }

        // 1em = 12pt = 240 twips
        if (TryParseSuffix(span, "em", out var em))
        {
            return (int)Math.Round(em * 240);
        }

        // 1in = 1440 twips
        if (TryParseSuffix(span, "in", out var inches))
        {
            return (int)Math.Round(inches * 1440);
        }

        // 1cm = 567 twips
        if (TryParseSuffix(span, "cm", out var cm))
        {
            return (int)Math.Round(cm * 567);
        }

        // 1mm = 56.7 twips
        if (TryParseSuffix(span, "mm", out var mm))
        {
            return (int)Math.Round(mm * 56.7);
        }

        // Bare number treated as px
        if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
        {
            return (int)Math.Round(raw * 15);
        }

        return null;
    }

    internal static int? ParseLengthToPixels(string value)
    {
        var twips = ParseLengthToTwips(value);
        if (twips == null)
        {
            return null;
        }

        return (int)Math.Round(twips.Value / 15d);
    }

    internal static (int? Top, int? Right, int? Bottom, int? Left) ParseMarginShorthand(string value)
    {
        Span<Range> ranges = stackalloc Range[4];
        var span = value.AsSpan();
        var count = 0;
        var i = 0;
        while (i < span.Length)
        {
            while (i < span.Length && char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            if (i >= span.Length)
            {
                break;
            }

            var start = i;
            while (i < span.Length && !char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            if (count == 4)
            {
                // More than 4 parts — invalid shorthand.
                return (null, null, null, null);
            }

            ranges[count++] = start..i;
        }

        if (count == 0)
        {
            return (null, null, null, null);
        }

        var v0 = ParseLengthToTwips(span[ranges[0]]);
        if (count == 1)
        {
            return (v0, v0, v0, v0);
        }

        var v1 = ParseLengthToTwips(span[ranges[1]]);
        if (count == 2)
        {
            return (v0, v1, v0, v1);
        }

        var v2 = ParseLengthToTwips(span[ranges[2]]);
        if (count == 3)
        {
            return (v0, v1, v2, v1);
        }

        var v3 = ParseLengthToTwips(span[ranges[3]]);
        return (v0, v1, v2, v3);
    }

    internal static int? ParseFontStretch(string value)
    {
        var span = value.AsSpan().Trim();

        if (span.EndsWith("%".AsSpan(), StringComparison.Ordinal) &&
            double.TryParse(span[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
        {
            return Clamp((int)Math.Round(pct));
        }

        if (span.Equals("ultra-condensed", StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

        if (span.Equals("extra-condensed", StringComparison.OrdinalIgnoreCase))
        {
            return 62;
        }

        if (span.Equals("condensed", StringComparison.OrdinalIgnoreCase))
        {
            return 75;
        }

        if (span.Equals("semi-condensed", StringComparison.OrdinalIgnoreCase))
        {
            return 87;
        }

        if (span.Equals("normal", StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (span.Equals("semi-expanded", StringComparison.OrdinalIgnoreCase))
        {
            return 112;
        }

        if (span.Equals("expanded", StringComparison.OrdinalIgnoreCase))
        {
            return 125;
        }

        if (span.Equals("extra-expanded", StringComparison.OrdinalIgnoreCase))
        {
            return 150;
        }

        if (span.Equals("ultra-expanded", StringComparison.OrdinalIgnoreCase))
        {
            return 200;
        }

        return null;

        static int Clamp(int v)
        {
            if (v < 1)
            {
                return 1;
            }

            if (v > 600)
            {
                return 600;
            }

            return v;
        }
    }

    internal static JustificationValues? ParseTextAlign(string value)
    {
        var span = value.AsSpan().Trim();
        if (span.Equals("left", StringComparison.OrdinalIgnoreCase))
        {
            return JustificationValues.Left;
        }

        if (span.Equals("center", StringComparison.OrdinalIgnoreCase))
        {
            return JustificationValues.Center;
        }

        if (span.Equals("right", StringComparison.OrdinalIgnoreCase))
        {
            return JustificationValues.Right;
        }

        if (span.Equals("justify", StringComparison.OrdinalIgnoreCase))
        {
            return JustificationValues.Both;
        }

        return null;
    }

    internal static BorderInfo? ParseBorder(string value)
    {
        var span = value.AsSpan();
        int? widthEighths = null;
        BorderValues? style = null;
        string? color = null;

        var i = 0;
        while (i < span.Length)
        {
            while (i < span.Length && char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            if (i >= span.Length)
            {
                break;
            }

            var start = i;
            while (i < span.Length && !char.IsWhiteSpace(span[i]))
            {
                i++;
            }

            var part = span[start..i];

            if (TryParseBorderStyle(part, out var bs))
            {
                style = bs;
            }
            else if (widthEighths == null)
            {
                var w = ParseBorderWidth(part);
                if (w != null)
                {
                    widthEighths = w;
                    continue;
                }

                // Not a width, try as color
                var parsed = ColorParser.Parse(part.ToString());
                if (parsed != null)
                {
                    color = parsed;
                }
            }
            else
            {
                var parsed = ColorParser.Parse(part.ToString());
                if (parsed != null)
                {
                    color = parsed;
                }
            }
        }

        if (style == null && widthEighths == null && color == null)
        {
            return null;
        }

        if (style == BorderValues.None)
        {
            return new(0, BorderValues.None, null);
        }

        return new(widthEighths ?? 4, style ?? BorderValues.Single, color);
    }

    static bool TryParseBorderStyle(ReadOnlySpan<char> span, out BorderValues result)
    {
        if (span.Equals("solid", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.Single;
            return true;
        }

        if (span.Equals("dotted", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.Dotted;
            return true;
        }

        if (span.Equals("dashed", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.Dashed;
            return true;
        }

        if (span.Equals("double", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.Double;
            return true;
        }

        if (span.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            span.Equals("hidden", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.None;
            return true;
        }

        if (span.Equals("groove", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.ThreeDEngrave;
            return true;
        }

        if (span.Equals("ridge", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.ThreeDEmboss;
            return true;
        }

        if (span.Equals("inset", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.Inset;
            return true;
        }

        if (span.Equals("outset", StringComparison.OrdinalIgnoreCase))
        {
            result = BorderValues.Outset;
            return true;
        }

        result = default;
        return false;
    }

    static int? ParseBorderWidth(ReadOnlySpan<char> value)
    {
        var span = value.Trim();

        if (span.Equals("thin", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (span.Equals("medium", StringComparison.OrdinalIgnoreCase))
        {
            return 12;
        }

        if (span.Equals("thick", StringComparison.OrdinalIgnoreCase))
        {
            return 18;
        }

        // 1pt = 8 eighths
        if (TryParseSuffix(span, "pt", out var pt))
        {
            return Math.Max(1, (int)Math.Round(pt * 8));
        }

        // 1px ≈ 0.75pt = 6 eighths
        if (TryParseSuffix(span, "px", out var px))
        {
            return Math.Max(1, (int)Math.Round(px * 6));
        }

        if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
        {
            if (raw == 0)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Round(raw * 6));
        }

        return null;
    }

    static bool TryParseSuffix(ReadOnlySpan<char> span, string suffix, out double result)
    {
        if (span.EndsWith(suffix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return double.TryParse(
                span[..^suffix.Length].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out result);
        }

        result = 0;
        return false;
    }
}
