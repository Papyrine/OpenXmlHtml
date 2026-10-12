public class WordImageSizingTests
{
    const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";

    static List<OpenXmlElement> Build(string html)
    {
        var stream = new MemoryStream();
        var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        return WordHtmlConverter.ToElements(html, main);
    }

    [Test]
    public Task CssWidthPx() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="width: 400px"></p>"""));

    [Test]
    public Task CssHeightPx() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="height: 200px"></p>"""));

    [Test]
    public Task CssWidthAndHeightPx() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="width: 400px; height: 200px"></p>"""));

    [Test]
    public Task CssWidthInches() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="width: 2in"></p>"""));

    [Test]
    public Task CssWidthEm() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="width: 10em; height: 5em"></p>"""));

    [Test]
    public Task CssWidthPt() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="width: 300pt"></p>"""));

    [Test]
    public Task CssOverridesHtmlAttribute() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" width="50" height="50" style="width: 400px; height: 200px"></p>"""));

    [Test]
    public Task HtmlAttributeStillWorks() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" width="75" height="60"></p>"""));

    // Narrower than the old name (CssPercentageIgnored) implied: a percentage on a table or cell
    // width is honoured. An inline image's `wp:extent` is an absolute EMU extent with no percentage
    // form, so the width attribute is used instead.
    [Test]
    public Task PercentageWidthIgnoredBecauseExtentHasNoPercentForm() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}" style="width: 50%" width="150"></p>"""));

    // The decode goes through a pooled buffer now. The bytes that reach the document have to be
    // exactly the image, whatever its length does to the padding and however the text is wrapped.
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(1000)]
    [Arguments(100_001)]
    public async Task DataUriBytesSurviveDecoding(int length)
    {
        var expected = new byte[length];
        new Random(length).NextBytes(expected);
        var base64 = Convert.ToBase64String(expected, Base64FormattingOptions.InsertLineBreaks);

        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        WordHtmlConverter.ToElements($"""<img src="data:image/png;base64,{base64}" width="1" height="1">""", main);

        using var partStream = main.ImageParts.Single().GetStream();
        using var actual = new MemoryStream();
        partStream.CopyTo(actual);

        await Assert.That(actual.ToArray().SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task UndecodableDataUriIsStillReported()
    {
        var reported = new List<HtmlDiagnostic>();
        var settings = new HtmlConvertSettings
        {
            OnDiagnostic = reported.Add
        };

        var paragraphs = WordHtmlConverter.ToParagraphs("""<img src="data:image/png;base64,not*base64" alt="fallback">""", null, settings);

        await Assert.That(paragraphs.Single().InnerText).IsEqualTo("fallback");
        await Assert.That(reported.Count).IsEqualTo(1);
    }

    // Headers only: enough of each format for its size to be read, 300 wide by 150 high.
    static readonly byte[] pngHeader =
    [
        0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A,
        0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R',
        0, 0, 0x01, 0x2C, 0, 0, 0, 0x96
    ];

    static readonly byte[] gifHeader =
    [
        (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 0x2C, 0x01, 0x96, 0x00
    ];

    static readonly byte[] bmpHeader =
    [
        (byte)'B', (byte)'M', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 40, 0, 0, 0,
        0x2C, 0x01, 0, 0,
        // top-down, so the height is stored negative
        0x6A, 0xFF, 0xFF, 0xFF
    ];

    // An application segment first, then padding, then the start of frame that holds the size.
    static readonly byte[] jpegHeader =
    [
        0xFF, 0xD8,
        0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00,
        0xFF,
        0xFF, 0xC2, 0x00, 0x11, 0x08, 0x00, 0x96, 0x01, 0x2C, 0x03, 0x01, 0x11, 0x00
    ];

    [Test]
    public async Task NaturalSizeIsReadFromEachFormat()
    {
        foreach (var header in new[] { pngHeader, gifHeader, bmpHeader, jpegHeader })
        {
            var read = ImageSize.TryReadNaturalSize(header, out var width, out var height);

            await Assert.That(read).IsTrue();
            await Assert.That(width).IsEqualTo(300);
            await Assert.That(height).IsEqualTo(150);
        }
    }

    // Not an image, too short to say, or a size of zero: the caller keeps its fallback.
    [Test]
    public async Task UnreadableSizeIsNotGuessed()
    {
        byte[][] unreadable =
        [
            [],
            [1, 2, 3],
            "<svg xmlns='http://www.w3.org/2000/svg'/>"u8.ToArray(),
            pngHeader.Take(20).ToArray(),
            [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00],
            [(byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a', 0, 0, 0, 0]
        ];

        foreach (var bytes in unreadable)
        {
            await Assert.That(ImageSize.TryReadNaturalSize(bytes, out _, out _)).IsFalse();
        }
    }

    // A missing dimension comes from the image, as in a browser: its natural size, or the given
    // one scaled by its proportions. Each used to default to 100 regardless of the image's shape.
    [Test]
    [Arguments(null, null, 300, 150)]
    [Arguments(600, null, 600, 300)]
    [Arguments(null, 50, 100, 50)]
    [Arguments(40, 400, 40, 400)]
    public async Task MissingDimensionFollowsTheImage(int? givenWidth, int? givenHeight, int width, int height)
    {
        var size = ImageSize.Resolve(new(pngHeader, "image/png", givenWidth, givenHeight));

        await Assert.That(size).IsEqualTo((width, height));
    }

    // An image nobody sized is not drawn wider than the page's text.
    [Test]
    public async Task NaturalSizeIsFittedToThePage()
    {
        byte[] wide =
        [
            0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A,
            0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R',
            0, 0, 0x0F, 0xA0, 0, 0, 0x07, 0xD0
        ];

        var size = ImageSize.Resolve(new(wide, "image/png", null, null));

        await Assert.That(size).IsEqualTo((602, 301));
    }

    // Where the size cannot be read, the old fallback of 100 still stands in.
    [Test]
    public async Task UnreadableImageKeepsTheFallback()
    {
        var size = ImageSize.Resolve(new([1, 2, 3], "image/png", 250, null));

        await Assert.That(size).IsEqualTo((250, 100));
    }

    [Test]
    public Task ImageWithNoSizeIsDrawnAtItsOwn() =>
        Verify(Build($"""<p><img src="data:image/png;base64,{png}"></p>"""));
}
