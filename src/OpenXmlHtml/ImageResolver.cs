static class ImageResolver
{
    // The default client does NOT auto-redirect: redirects are followed manually so each hop is
    // re-checked against the image policy. Otherwise a redirect from an allowed host could point
    // at an internal address and bypass SafeDomains (an SSRF vector).
    static readonly HttpClient sharedClient = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    const int maxRedirects = 5;
    const long maxImageBytes = 50L * 1024 * 1024;

    const string webBlocked = "blocked by HtmlConvertSettings.WebImages";
    const string webFailed = "the image could not be downloaded";
    const string localBlocked = "blocked by HtmlConvertSettings.LocalImages";
    const string localFailed = "the local image file could not be read";

    // Covers a percentage, a keyword like auto, and anything that is not a css length: an inline
    // image is sized by wp:extent, which takes an absolute emu extent and nothing else.
    internal const string ExtentIsAbsolute = "wp:extent takes an absolute extent, so this size could not be resolved to one";

    static readonly Dictionary<string, string> extensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".bmp"] = "image/bmp",
        [".tiff"] = "image/tiff",
        [".tif"] = "image/tiff",
        [".svg"] = "image/svg+xml",
        [".webp"] = "image/webp",
        [".ico"] = "image/x-icon"
    };

    internal static ImageData? Resolve(IElement element, HtmlConvertSettings? settings)
    {
        var src = element.GetAttribute("src");
        if (src == null)
        {
            return null;
        }

        if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return HtmlSegmentParser.ParseImageSrc(element, settings);
        }

        if (settings == null)
        {
            return null;
        }

        byte[]? bytes;
        string? contentType;

        if (src.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            src.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!settings.WebImages.IsAllowed(src))
            {
                Diagnostic.IgnoredAttribute(settings, "src", src, webBlocked);
                return null;
            }

            var client = settings.HttpClient ?? sharedClient;
            try
            {
                var downloaded = Download(client, src, settings.WebImages);
                if (downloaded == null)
                {
                    Diagnostic.IgnoredAttribute(settings, "src", src, webFailed);
                    return null;
                }

                bytes = downloaded.Value.Bytes;
                contentType = downloaded.Value.ContentType;
            }
            catch
            {
                Diagnostic.IgnoredAttribute(settings, "src", src, webFailed);
                return null;
            }
        }
        else
        {
            if (!settings.LocalImages.IsAllowed(src))
            {
                Diagnostic.IgnoredAttribute(settings, "src", src, localBlocked);
                return null;
            }

            var path = src;
            if (src.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(src, UriKind.Absolute, out var uri))
                {
                    Diagnostic.IgnoredAttribute(settings, "src", src, localFailed);
                    return null;
                }

                path = uri.LocalPath;
            }

            try
            {
                path = Path.GetFullPath(path);
                if (!File.Exists(path))
                {
                    Diagnostic.IgnoredAttribute(settings, "src", src, localFailed);
                    return null;
                }

                bytes = File.ReadAllBytes(path);
                contentType = GuessContentType(path);
            }
            catch
            {
                Diagnostic.IgnoredAttribute(settings, "src", src, localFailed);
                return null;
            }
        }

        contentType ??= "image/png";

        var (width, height) = ParseImageDimensions(element, settings);
        return new(bytes, contentType, width, height)
        {
            Float = ParseFloat(element)
        };
    }

    internal static FloatSide ParseFloat(IElement element)
    {
        var style = element.GetAttribute("style");
        if (style == null)
        {
            return FloatSide.None;
        }

        var declarations = StyleParser.Parse(style);
        if (!declarations.TryGetValue("float", out var value))
        {
            return FloatSide.None;
        }

        if (value.Equals("left", StringComparison.OrdinalIgnoreCase))
        {
            return FloatSide.Left;
        }

        if (value.Equals("right", StringComparison.OrdinalIgnoreCase))
        {
            return FloatSide.Right;
        }

        return FloatSide.None;
    }

    internal static (int? Width, int? Height) ParseImageDimensions(IElement element, HtmlConvertSettings? settings)
    {
        int? width = null;
        int? height = null;

        var style = element.GetAttribute("style");
        if (style != null)
        {
            var declarations = StyleParser.Parse(style);
            if (declarations.TryGetValue("width", out var cssWidth))
            {
                width = StyleParser.ParseLengthToPixels(cssWidth);
                if (width == null)
                {
                    Diagnostic.DroppedProperty(settings, "width", cssWidth, ExtentIsAbsolute);
                }
            }

            if (declarations.TryGetValue("height", out var cssHeight))
            {
                height = StyleParser.ParseLengthToPixels(cssHeight);
                if (height == null)
                {
                    Diagnostic.DroppedProperty(settings, "height", cssHeight, ExtentIsAbsolute);
                }
            }
        }

        if (width == null)
        {
            width = ParseDimensionAttribute(element, "width", settings);
        }

        if (height == null)
        {
            height = ParseDimensionAttribute(element, "height", settings);
        }

        return (width, height);
    }

    static int? ParseDimensionAttribute(IElement element, string name, HtmlConvertSettings? settings)
    {
        var value = element.GetAttribute(name);
        if (value == null)
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        Diagnostic.IgnoredAttribute(settings, name, value, ExtentIsAbsolute);
        return null;
    }

    // Downloads an image, following redirects manually so each hop is policy-checked, and
    // capping the response size to guard against unbounded memory use.
    static (byte[] Bytes, string? ContentType)? Download(HttpClient client, string url, ImagePolicy policy)
    {
        for (var redirect = 0; redirect <= maxRedirects; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

            if (IsRedirect(response))
            {
                var location = response.Headers.Location;
                if (location == null)
                {
                    return null;
                }

                url = (location.IsAbsoluteUri ? location : new(new(url), location)).ToString();
                if (!policy.IsAllowed(url))
                {
                    return null;
                }

                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (response.Content.Headers.ContentLength > maxImageBytes)
            {
                return null;
            }

            var bytes = ReadCapped(response);
            if (bytes == null)
            {
                return null;
            }

            return (bytes, response.Content.Headers.ContentType?.MediaType);
        }

        return null;
    }

    static byte[]? ReadCapped(HttpResponseMessage response)
    {
        using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (memory.Length + read > maxImageBytes)
            {
                return null;
            }

            memory.Write(buffer, 0, read);
        }

        return memory.ToArray();
    }

    static bool IsRedirect(HttpResponseMessage response)
    {
        var code = (int)response.StatusCode;
        return code is >= 300 and <= 399 && code != 304;
    }

    static string GuessContentType(string path) =>
        extensionToContentType.GetValueOrDefault(Path.GetExtension(path), "image/png");
}
