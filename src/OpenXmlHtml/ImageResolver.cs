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
            return HtmlSegmentParser.ParseImageSrc(element);
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
                return null;
            }

            var client = settings.HttpClient ?? sharedClient;
            try
            {
                var downloaded = Download(client, src, settings.WebImages);
                if (downloaded == null)
                {
                    return null;
                }

                bytes = downloaded.Value.Bytes;
                contentType = downloaded.Value.ContentType;
            }
            catch
            {
                return null;
            }
        }
        else
        {
            if (!settings.LocalImages.IsAllowed(src))
            {
                return null;
            }

            var path = src;
            if (src.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(src, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                path = uri.LocalPath;
            }

            try
            {
                path = Path.GetFullPath(path);
                if (!File.Exists(path))
                {
                    return null;
                }

                bytes = File.ReadAllBytes(path);
                contentType = GuessContentType(path);
            }
            catch
            {
                return null;
            }
        }

        contentType ??= "image/png";

        var (width, height) = ParseImageDimensions(element);
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

    internal static (int? Width, int? Height) ParseImageDimensions(IElement element)
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
            }

            if (declarations.TryGetValue("height", out var cssHeight))
            {
                height = StyleParser.ParseLengthToPixels(cssHeight);
            }
        }

        if (width == null)
        {
            var widthAttr = element.GetAttribute("width");
            if (widthAttr != null && int.TryParse(widthAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var w))
            {
                width = w;
            }
        }

        if (height == null)
        {
            var heightAttr = element.GetAttribute("height");
            if (heightAttr != null && int.TryParse(heightAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h))
            {
                height = h;
            }
        }

        return (width, height);
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

                url = (location.IsAbsoluteUri ? location : new Uri(new Uri(url), location)).ToString();
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
