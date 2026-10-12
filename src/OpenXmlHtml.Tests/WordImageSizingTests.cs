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
}
