#if NETFRAMEWORK
using System.Net.Http;
#endif

// An image part is declared as the format its bytes are. The declared type used to decide, and
// anything but four spellings of it was declared as png: a gif served as application/octet-stream,
// an emf, a webp or a jpeg written as image/pjpeg all ended up in a part named .png.
public class WordImageContentTypeTests
{
    const string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
    const string gifBase64 = "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";

    // 8 by 8, red.
    const string jpegBase64 = "/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAAIAAgDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD50ooor8MP9Uz/2Q==";

    // 1 by 1, lossless.
    const string webpBase64 = "UklGRhoAAABXRUJQVlA4TA0AAAAvAAAAEAcQERGIiP4HAA==";

    // 16 by 16, a red square, as gdi writes it.
    const string emfBase64 = "AQAAAGwAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAABQAQAAUAEAACBFTUYAAAEAYAEAAA0AAAACAAAAAAAAAAAAAAAAAAAAgAcAALAEAABYAQAA1wAAAAAAAAAAAAAAAAAAAMA/BQDYRwMAIQAAAAgAAABiAAAADAAAAAEAAAAkAAAAJAAAAAAAgD0AAAAAAAAAAAAAgD0AAAAAAAAAAAIAAAAnAAAAGAAAAAEAAAAAAAAA/wAAAAAAAAAlAAAADAAAAAEAAAAlAAAADAAAAAgAAIBWAAAAMAAAAAAAAAAAAAAAEAAAABAAAAAFAAAAAAAAAAAAAAEAAQABAAEAAAAAAAAlAAAADAAAAAcAAIAlAAAADAAAAAAAAIAkAAAAJAAAAAAAgEEAAAAAAAAAAAAAgEEAAAAAAAAAAAIAAAAiAAAADAAAAP////8OAAAAFAAAAAAAAAAQAAAAFAAAAA==";

    const string svgText = """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" fill="red"/></svg>""";

    internal static byte[] Sample(string name) =>
        name switch
        {
            "png" => Convert.FromBase64String(pngBase64),
            "gif" => Convert.FromBase64String(gifBase64),
            "jpeg" => Convert.FromBase64String(jpegBase64),
            "webp" => Convert.FromBase64String(webpBase64),
            "emf" => Convert.FromBase64String(emfBase64),
            "bmp" => Bmp(),
            "tiff" => Tiff(false),
            "tiff-big-endian" => Tiff(true),
            "wmf" => Wmf(true),
            "wmf-bare" => Wmf(false),
            "ico" => Ico(),
            "svg" => Encoding.UTF8.GetBytes(svgText),
            "svg-prolog" => Encoding.UTF8.GetBytes(
                "﻿<?xml version=\"1.0\"?>\n<!-- drawn by hand -->\n<!DOCTYPE svg PUBLIC \"-//W3C//DTD SVG 1.1//EN\" \"http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd\">\n" + svgText),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
        };

    // 2 by 2, 24 bit, red.
    static byte[] Bmp()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte) 'B');
        writer.Write((byte) 'M');
        writer.Write(70);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(2);
        writer.Write(2);
        writer.Write((short) 1);
        writer.Write((short) 24);
        writer.Write(0);
        writer.Write(16);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        for (var row = 0; row < 2; row++)
        {
            writer.Write([0, 0, 0xFF, 0, 0, 0xFF, 0, 0]);
        }

        writer.Flush();
        return stream.ToArray();
    }

    // 2 by 2, 8 bit grey, uncompressed, in either byte order.
    static byte[] Tiff(bool bigEndian)
    {
        var bytes = new List<byte>();

        void Short(int value)
        {
            if (bigEndian)
            {
                bytes.Add((byte) (value >> 8));
                bytes.Add((byte) value);
                return;
            }

            bytes.Add((byte) value);
            bytes.Add((byte) (value >> 8));
        }

        void Long(int value)
        {
            if (bigEndian)
            {
                Short(value >> 16);
                Short(value);
                return;
            }

            Short(value);
            Short(value >> 16);
        }

        void ShortEntry(int tag, int value)
        {
            Short(tag);
            Short(3);
            Long(1);
            Short(value);
            Short(0);
        }

        void LongEntry(int tag, int value)
        {
            Short(tag);
            Short(4);
            Long(1);
            Long(value);
        }

        if (bigEndian)
        {
            bytes.AddRange("MM"u8.ToArray());
        }
        else
        {
            bytes.AddRange("II"u8.ToArray());
        }

        Short(42);
        Long(8);
        Short(8);
        ShortEntry(256, 2);
        ShortEntry(257, 2);
        ShortEntry(258, 8);
        ShortEntry(259, 1);
        ShortEntry(262, 1);
        LongEntry(273, 110);
        ShortEntry(278, 2);
        LongEntry(279, 4);
        Long(0);
        bytes.AddRange([0x40, 0x40, 0x40, 0x40]);
        return bytes.ToArray();
    }

    // A red square 100 units wide, with or without the placeable header.
    static byte[] Wmf(bool placeable)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        if (placeable)
        {
            writer.Write(0x9AC6CDD7u);
            writer.Write((short) 0);
            writer.Write((short) 0);
            writer.Write((short) 0);
            writer.Write((short) 100);
            writer.Write((short) 100);
            writer.Write((short) 96);
            writer.Write(0);
            writer.Write((ushort) (0xCDD7 ^ 0x9AC6 ^ 100 ^ 100 ^ 96));
        }

        writer.Write((short) 1);
        writer.Write((short) 9);
        writer.Write((short) 0x0300);
        writer.Write(40);
        writer.Write((short) 1);
        writer.Write(7);
        writer.Write((short) 0);

        // SetWindowOrg, SetWindowExt
        writer.Write(5);
        writer.Write((short) 0x020B);
        writer.Write((short) 0);
        writer.Write((short) 0);
        writer.Write(5);
        writer.Write((short) 0x020C);
        writer.Write((short) 100);
        writer.Write((short) 100);

        // CreateBrushIndirect, SelectObject
        writer.Write(7);
        writer.Write((short) 0x02FC);
        writer.Write((short) 0);
        writer.Write(0x000000FF);
        writer.Write((short) 0);
        writer.Write(4);
        writer.Write((short) 0x012D);
        writer.Write((short) 0);

        // Rectangle, then the end record
        writer.Write(7);
        writer.Write((short) 0x041B);
        writer.Write((short) 100);
        writer.Write((short) 100);
        writer.Write((short) 0);
        writer.Write((short) 0);
        writer.Write(3);
        writer.Write((short) 0);

        writer.Flush();
        return stream.ToArray();
    }

    // One 2 by 2, 32 bit, red image.
    static byte[] Ico()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((short) 0);
        writer.Write((short) 1);
        writer.Write((short) 1);

        writer.Write((byte) 2);
        writer.Write((byte) 2);
        writer.Write((byte) 0);
        writer.Write((byte) 0);
        writer.Write((short) 1);
        writer.Write((short) 32);
        writer.Write(64);
        writer.Write(22);

        writer.Write(40);
        writer.Write(2);
        writer.Write(4);
        writer.Write((short) 1);
        writer.Write((short) 32);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        for (var pixel = 0; pixel < 4; pixel++)
        {
            writer.Write([0, 0, 0xFF, 0xFF]);
        }

        writer.Write(new byte[8]);
        writer.Flush();
        return stream.ToArray();
    }

    static string DataUri(string declared, byte[] bytes) =>
        $"data:{declared};base64,{Convert.ToBase64String(bytes)}";

    // ReSharper disable NotAccessedPositionalProperty.Local
    record StoredPart(string ContentType, string Extension);
    // ReSharper restore NotAccessedPositionalProperty.Local

    // The part each drawing's picture resolves to, in document order, and what the validator makes
    // of the document.
    static (List<StoredPart> Parts, List<string> Errors) Store(string html, HtmlConvertSettings? settings = null)
    {
        using var stream = new MemoryStream();
        if (settings == null)
        {
            WordHtmlConverter.ConvertToDocx(html, stream);
        }
        else
        {
            WordHtmlConverter.ConvertToDocx(html, stream, settings);
        }

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var main = document.MainDocumentPart!;
        var parts = main.Document!
            .Descendants<DocumentFormat.OpenXml.Drawing.Blip>()
            .Select(_ => main.GetPartById(_.Embed!.Value!))
            .Select(_ => new StoredPart(_.ContentType, Path.GetExtension(_.Uri.OriginalString)))
            .ToList();
        var errors = new OpenXmlValidator()
            .Validate(document)
            .Select(_ => _.Description)
            .ToList();
        return (parts, errors);
    }

    [Test]
    [Arguments("png", "image/png")]
    [Arguments("jpeg", "image/jpeg")]
    [Arguments("gif", "image/gif")]
    [Arguments("bmp", "image/bmp")]
    [Arguments("tiff", "image/tiff")]
    [Arguments("tiff-big-endian", "image/tiff")]
    [Arguments("webp", "image/webp")]
    [Arguments("emf", "image/x-emf")]
    [Arguments("wmf", "image/x-wmf")]
    [Arguments("wmf-bare", "image/x-wmf")]
    [Arguments("ico", "image/x-icon")]
    [Arguments("svg", "image/svg+xml")]
    [Arguments("svg-prolog", "image/svg+xml")]
    public async Task FormatIsDetectedFromTheBytes(string sample, string expected) =>
        await Assert.That(ImageFormat.Detect(Sample(sample))).IsEqualTo(expected);

    // Text, markup that is not svg, an element whose name only starts with svg, and headers too
    // short or too loose to be the format their first bytes suggest.
    [Test]
    public async Task UnrecognisedBytesAreNotGuessed()
    {
        byte[][] unrecognised =
        [
            [],
            [1, 2, 3],
            "BM is how this sentence starts"u8.ToArray(),
            "<html><body><svg/></body></html>"u8.ToArray(),
            "<svgx/>"u8.ToArray(),
            "<?xml version='1.0'?><root/>"u8.ToArray(),
            "<!-- never closed <svg/>"u8.ToArray(),
            "RIFF\0\0\0\0WAVEfmt "u8.ToArray(),
            [0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
            [0xFF, 0xD8]
        ];

        foreach (var bytes in unrecognised)
        {
            await Assert.That(ImageFormat.Detect(bytes)).IsNull();
        }
    }

    // Every image is declared as something it is not. The part follows the bytes.
    [Test]
    [Arguments("png", "image/gif", "image/png", ".png")]
    [Arguments("jpeg", "image/png", "image/jpeg", ".jpeg")]
    [Arguments("jpeg", "image/pjpeg", "image/jpeg", ".jpeg")]
    [Arguments("gif", "application/octet-stream", "image/gif", ".gif")]
    [Arguments("bmp", "image/png", "image/bmp", ".bmp")]
    [Arguments("tiff", "image/png", "image/tiff", ".tiff")]
    [Arguments("tiff-big-endian", "", "image/tiff", ".tiff")]
    [Arguments("webp", "image/webp", "image/webp", ".webp")]
    [Arguments("emf", "image/x-emf", "image/x-emf", ".emf")]
    [Arguments("emf", "image/png", "image/x-emf", ".emf")]
    [Arguments("wmf", "image/x-wmf", "image/x-wmf", ".wmf")]
    [Arguments("wmf-bare", "image/jpeg", "image/x-wmf", ".wmf")]
    [Arguments("ico", "image/vnd.microsoft.icon", "image/x-icon", ".ico")]
    public async Task PartIsDeclaredAsWhatItHolds(string sample, string declared, string contentType, string extension)
    {
        var (parts, errors) = Store($"""<p><img src="{DataUri(declared, Sample(sample))}" width="16" height="16"></p>""");

        await Assert.That(parts).IsEquivalentTo([new StoredPart(contentType, extension)], CollectionOrdering.Matching);
        await Assert.That(errors).IsEmpty();
    }

    // Bytes that are no image known here are stored as they were declared, whatever the spelling,
    // and as png where the declared type names no image format either.
    [Test]
    [Arguments("image/png", "image/png", ".png")]
    [Arguments("IMAGE/JPEG", "image/jpeg", ".jpeg")]
    [Arguments("image/jpg", "image/jpeg", ".jpeg")]
    [Arguments("image/jpeg; charset=binary", "image/jpeg", ".jpeg")]
    [Arguments("image/x-ms-bmp", "image/bmp", ".bmp")]
    [Arguments("image/tif", "image/tiff", ".tiff")]
    [Arguments("image/webp", "image/webp", ".webp")]
    [Arguments("image/emf", "image/x-emf", ".emf")]
    [Arguments("image/x-wmf", "image/x-wmf", ".wmf")]
    [Arguments("image/x-icon", "image/x-icon", ".ico")]
    [Arguments("image/avif", "image/png", ".png")]
    [Arguments("application/octet-stream", "image/png", ".png")]
    [Arguments("", "image/png", ".png")]
    public async Task UnrecognisedBytesAreStoredAsDeclared(string declared, string contentType, string extension)
    {
        var (parts, errors) = Store($"""<p><img src="{DataUri(declared, [1, 2, 3])}" width="16" height="16"></p>""");

        await Assert.That(parts).IsEquivalentTo([new StoredPart(contentType, extension)], CollectionOrdering.Matching);
        await Assert.That(errors).IsEmpty();
    }

    // An svg takes the svg path however it was declared: the drawing's picture is the fallback
    // png, and the svg sits behind it in the extension.
    [Test]
    [Arguments("svg", "image/png")]
    [Arguments("svg-prolog", "application/octet-stream")]
    [Arguments("svg", "IMAGE/SVG+XML")]
    public async Task SvgIsStoredWithItsFallback(string sample, string declared)
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx($"""<p><img src="{DataUri(declared, Sample(sample))}" width="16" height="16"></p>""", stream);
        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var main = document.MainDocumentPart!;
        var blip = main.Document!.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().Single();
        var svgBlip = blip.Descendants().Single(_ => _.LocalName == "svgBlip");
        var svgPart = main.GetPartById(svgBlip.GetAttributes().Single(_ => _.LocalName == "embed").Value!);

        await Assert.That(main.GetPartById(blip.Embed!.Value!).ContentType).IsEqualTo("image/png");
        await Assert.That(svgPart.ContentType).IsEqualTo("image/svg+xml");
        await Assert.That(Path.GetExtension(svgPart.Uri.OriginalString)).IsEqualTo(".svg");
        await Assert.That(new OpenXmlValidator().Validate(document).Select(_ => _.Description)).IsEmpty();
    }

    // A png declared as svg is a png, and is drawn as one.
    [Test]
    public async Task ImageDeclaredAsSvgIsStoredAsWhatItIs()
    {
        var (parts, errors) = Store($"""<p><img src="{DataUri("image/svg+xml", Sample("png"))}"></p>""");

        await Assert.That(parts).IsEquivalentTo([new StoredPart("image/png", ".png")], CollectionOrdering.Matching);
        await Assert.That(errors).IsEmpty();
    }

    // The same bytes under two declared types are one picture, stored once.
    [Test]
    public async Task SameImageUnderTwoDeclaredTypesIsStoredOnce()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            $"""
             <p><img src="{DataUri("image/gif", Sample("gif"))}" width="16" height="16"></p>
             <p><img src="{DataUri("image/png", Sample("gif"))}" width="16" height="16"></p>
             """,
            stream);
        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);

        await Assert.That(document.MainDocumentPart!.ImageParts.Select(_ => _.ContentType)).IsEquivalentTo(["image/gif"]);
    }

    // A file's extension is a claim like any other: a gif saved as .png is a gif, and bytes that
    // say nothing are stored as the extension names them.
    [Test]
    [Arguments("gif", "photo.png", "image/gif", ".gif")]
    [Arguments("jpeg", "photo", "image/jpeg", ".jpeg")]
    [Arguments("emf", "drawing.emf", "image/x-emf", ".emf")]
    [Arguments(null, "drawing.EMF", "image/x-emf", ".emf")]
    [Arguments(null, "drawing.wmf", "image/x-wmf", ".wmf")]
    [Arguments(null, "photo.jfif", "image/jpeg", ".jpeg")]
    [Arguments(null, "photo.unknown", "image/png", ".png")]
    public async Task LocalImageFollowsBytesThenExtension(string? sample, string fileName, string contentType, string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), "OpenXmlHtmlTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(directory);
        try
        {
            byte[] bytes = [1, 2, 3];
            if (sample != null)
            {
                bytes = Sample(sample);
            }

            var path = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(path, bytes);
            var settings = new HtmlConvertSettings
            {
                LocalImages = ImagePolicy.SafeDirectories(directory)
            };

            var (parts, errors) = Store($"""<p><img src="{path.Replace("\\", "/")}" width="16" height="16"></p>""", settings);

            await Assert.That(parts).IsEquivalentTo([new StoredPart(contentType, extension)], CollectionOrdering.Matching);
            await Assert.That(errors).IsEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    // A server's Content-Type is often the generic one, or missing. The bytes decide; where they
    // say nothing the media type does, and where that names no image the url's extension.
    [Test]
    [Arguments("jpeg", "application/octet-stream", "https://example.com/download?id=1", "image/jpeg", ".jpeg")]
    [Arguments("gif", null, "https://example.com/logo.png", "image/gif", ".gif")]
    [Arguments("webp", "image/png", "https://example.com/logo", "image/webp", ".webp")]
    [Arguments(null, "image/pjpeg", "https://example.com/logo.png", "image/jpeg", ".jpeg")]
    [Arguments(null, "application/octet-stream", "https://example.com/drawing.emf?v=2", "image/x-emf", ".emf")]
    [Arguments(null, null, "https://example.com/logo.gif", "image/gif", ".gif")]
    [Arguments(null, "text/plain", "https://example.com/logo", "image/png", ".png")]
    public async Task WebImageFollowsBytesThenMediaTypeThenExtension(string? sample, string? mediaType, string url, string contentType, string extension)
    {
        byte[] bytes = [1, 2, 3];
        if (sample != null)
        {
            bytes = Sample(sample);
        }

        var settings = new HtmlConvertSettings
        {
            WebImages = ImagePolicy.AllowAll(),
            HttpClient = new(new FixedImageHandler(bytes, mediaType))
        };

        var (parts, errors) = Store($"""<p><img src="{url}" width="16" height="16"></p>""", settings);

        await Assert.That(parts).IsEquivalentTo([new StoredPart(contentType, extension)], CollectionOrdering.Matching);
        await Assert.That(errors).IsEmpty();
    }

    class FixedImageHandler(byte[] bytes, string? mediaType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Cancel cancel)
        {
            var content = new ByteArrayContent(bytes);
            if (mediaType != null)
            {
                content.Headers.ContentType = new(mediaType);
            }

            return Task.FromResult(
                new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = content
                });
        }
    }

    // A header's image belongs to the header part, which stores it the same way.
    [Test]
    [Arguments("gif", "image/gif", ".gif")]
    [Arguments("webp", "image/webp", ".webp")]
    public async Task HeaderAndFooterImagesFollowTheBytes(string sample, string contentType, string extension)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            main.Document = new(body);
            var html = $"""<p><img src="{DataUri("image/png", Sample(sample))}" width="16" height="16"></p>""";
            WordHtmlConverter.AppendHtml(body, "<p>body</p>", main);
            WordHtmlConverter.SetHeader(main, html);
            WordHtmlConverter.SetFooter(main, html);
        }

        stream.Position = 0;
        using var reopened = WordprocessingDocument.Open(stream, false);
        var reopenedMain = reopened.MainDocumentPart!;
        var parts = reopenedMain.HeaderParts.SelectMany(_ => _.ImageParts)
            .Concat(reopenedMain.FooterParts.SelectMany(_ => _.ImageParts))
            .Select(_ => new StoredPart(_.ContentType, Path.GetExtension(_.Uri.OriginalString)))
            .ToList();

        await Assert.That(parts)
            .IsEquivalentTo<IList<StoredPart>, StoredPart>(
                [
                    new(contentType, extension),
                    new(contentType, extension)
                ],
                CollectionOrdering.Matching);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }

    // The size is read from the bytes, so a mislabelled image is still drawn at its own.
    [Test]
    public async Task MislabelledImageKeepsItsNaturalSize()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx($"""<p><img src="{DataUri("image/png", Sample("jpeg"))}"></p>""", stream);
        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var extent = document.MainDocumentPart!.Document!
            .Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>()
            .Single();

        await Assert.That(extent.Cx!.Value).IsEqualTo(8 * 9525L);
        await Assert.That(extent.Cy!.Value).IsEqualTo(8 * 9525L);
    }
}
