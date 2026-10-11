using System.Diagnostics.CodeAnalysis;

static class LinkTarget
{
    // Whether an href is an absolute url, which is the only kind a relationship can point at.
    //
    // Uri.TryCreate alone is not the test. Asked for an absolute uri it also accepts a path, and
    // which paths depends on the platform: "//host/page", a protocol-relative link, parses as a UNC
    // file path on Windows, and "/docs/page", a root-relative one, parses as a file path on Linux
    // and macOS. Either way an ordinary relative web link came out as a file:// hyperlink, on some
    // machines and not others. An href that starts with a slash is relative, whatever the platform
    // makes of it, so it stays text and keeps its " (url)".
    internal static bool TryParseAbsolute(string? href, [NotNullWhen(true)] out Uri? uri)
    {
        if (href == null ||
            href.Length == 0 ||
            href[0] is '/' or '\\')
        {
            uri = null;
            return false;
        }

        return Uri.TryCreate(href, UriKind.Absolute, out uri);
    }
}
