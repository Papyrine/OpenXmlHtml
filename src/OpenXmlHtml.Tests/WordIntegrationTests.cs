public class WordIntegrationTests
{
    [Test]
    public Task AppendHtmlToBody()
    {
        using var stream = new MemoryStream();
        using var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document);
        var main = document.AddMainDocumentPart();
        main.Document = new(new Body());

        WordHtmlConverter.AppendHtml(main.Document.Body!,
            "<h1>Report Title</h1><p>This is a <b>bold</b> statement.</p>");

        return Verify(main.Document.Body!);
    }

    [Test]
    public Task RichFormattedDocument()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            """
            <h1>Status Report</h1>
            <p><b>Date:</b> 2024-01-15</p>
            <p>All systems <span style="color: green"><b>operational</b></span>.</p>
            <ul>
              <li>Server: <span style="color: green">OK</span></li>
              <li>Database: <span style="color: green">OK</span></li>
              <li>Cache: <span style="color: red">Down</span></li>
            </ul>
            """);

        return Verify(paragraphs);
    }

    [Test]
    public Task FormattedReport()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            """
            <h2>Meeting Notes</h2>
            <p><i>Date: January 15, 2024</i></p>
            <p>Attendees: <b>Alice</b>, <b>Bob</b>, <b>Charlie</b></p>
            <h3>Action Items</h3>
            <ol>
              <li>Review <code>PR #123</code></li>
              <li>Update <u>documentation</u></li>
              <li><del>Fix bug #456</del> <ins>Done!</ins></li>
            </ol>
            """);

        return Verify(paragraphs);
    }

    [Test]
    public Task MultiParagraphWithBreaks()
    {
        var paragraphs = WordHtmlConverter.ToParagraphs(
            "Line 1<br>Line 2<br>Line 3");

        return Verify(paragraphs);
    }

    const string png = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
    const string gif = "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";

    // Each call numbered its images from rImage1, so the second call reused the first one's
    // relationship id: both drawings pointed at the first picture and the second was written as an
    // orphan part.
    [Test]
    public async Task ImagesAcrossAppendCallsKeepTheirOwnParts()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            WordHtmlConverter.AppendHtml(body, $"""<p><img src="data:image/png;base64,{png}" width="2" height="2"></p>""", main);
            WordHtmlConverter.AppendHtml(body, $"""<p><img src="data:image/gif;base64,{gif}" width="1" height="1"></p>""", main);
            main.Document = new(body);
        }

        await Assert.That(ReadImageContentTypes(stream)).IsEquivalentTo(["image/png", "image/gif"], CollectionOrdering.Matching);
    }

    [Test]
    public async Task ImagesAcrossToParagraphsCallsKeepTheirOwnParts()
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            body.Append(WordHtmlConverter.ToParagraphs($"""<p><img src="data:image/png;base64,{png}" width="2" height="2"></p>""", main));
            body.Append(WordHtmlConverter.ToParagraphs($"""<p><img src="data:image/gif;base64,{gif}" width="1" height="1"></p>""", main));
            main.Document = new(body);
        }

        await Assert.That(ReadImageContentTypes(stream)).IsEquivalentTo(["image/png", "image/gif"], CollectionOrdering.Matching);
    }

    // The content type each drawing resolves to, in document order.
    static List<string> ReadImageContentTypes(MemoryStream stream)
    {
        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var main = document.MainDocumentPart!;
        return main.Document!
            .Descendants<DocumentFormat.OpenXml.Drawing.Blip>()
            .Select(_ => main.GetPartById(_.Embed!.Value!).ContentType)
            .ToList();
    }

    const string repeatedPng = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAEElEQVR4nGP4z8AARAwQCgAf7gP9i18U1AAAAABJRU5ErkJggg==";
    const string otherGif = "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7";

    // An image used several times is stored once, and every drawing still resolves to the right
    // picture after a save and reopen. Both paths.
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task RepeatedImageIsStoredOnce(bool elements)
    {
        var html =
            $"""
             <p><img src="data:image/png;base64,{repeatedPng}" width="2" height="2"></p>
             <p><img src="data:image/gif;base64,{otherGif}" width="1" height="1"></p>
             <table><tr><td><img src="data:image/png;base64,{repeatedPng}" width="2" height="2"></td></tr></table>
             <p><img src="data:image/png;base64,{repeatedPng}" width="4" height="4"></p>
             """;

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            if (elements)
            {
                body.Append(WordHtmlConverter.ToElements(html, main));
            }
            else
            {
                body.Append(WordHtmlConverter.ToParagraphs(html, main));
            }

            main.Document = new(body);
        }

        stream.Position = 0;
        using var reopened = WordprocessingDocument.Open(stream, false);
        var reopenedMain = reopened.MainDocumentPart!;
        var blips = reopenedMain.Document!.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().ToList();
        var embedded = blips.Select(_ => _.Embed!.Value!).ToList();

        var drawingIds = reopenedMain.Document.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>()
            .Select(_ => _.Id!.Value)
            .ToList();

        await Assert.That(reopenedMain.ImageParts.Count()).IsEqualTo(2);
        // The three uses of the png share its one relationship; each drawing still has its own id.
        await Assert.That(embedded.Distinct().Count()).IsEqualTo(2);
        await Assert.That(drawingIds).HasDistinctItems();
        await Assert.That(embedded.Select(_ => reopenedMain.GetPartById(_).ContentType))
            .IsEquivalentTo(["image/png", "image/gif", "image/png", "image/png"], CollectionOrdering.Matching);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }

    // Every svg carries the same fallback png, which is written once however many svgs there are.
    [Test]
    public async Task SvgFallbackIsStoredOnce()
    {
        using var stream = new MemoryStream();
        WordHtmlConverter.ConvertToDocx(
            """
            <svg width="10" height="10" xmlns="http://www.w3.org/2000/svg"><circle cx="5" cy="5" r="4"/></svg>
            <svg width="10" height="10" xmlns="http://www.w3.org/2000/svg"><rect width="5" height="5"/></svg>
            """,
            stream);

        stream.Position = 0;
        using var document = WordprocessingDocument.Open(stream, false);
        var parts = document.MainDocumentPart!.ImageParts.Select(_ => _.ContentType).Order().ToList();

        await Assert.That(parts).IsEquivalentTo(["image/png", "image/svg+xml", "image/svg+xml"], CollectionOrdering.Matching);
    }

    // A repeat adds no relationship, and the next call carries on from the highest one there is.
    // Its drawing id must not be one that call hands out again.
    [Test]
    public async Task RepeatedImageDrawingIdsStayUniqueAcrossCalls()
    {
        var twice =
            $"""
             <p><img src="data:image/png;base64,{repeatedPng}" width="2" height="2"></p>
             <p><img src="data:image/png;base64,{repeatedPng}" width="2" height="2"></p>
             <p><img src="data:image/png;base64,{repeatedPng}" width="2" height="2"></p>
             """;

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            var body = new Body();
            WordHtmlConverter.AppendHtml(body, twice, main);
            WordHtmlConverter.AppendHtml(body, twice, main);
            WordHtmlConverter.AppendHtml(body, $"""<p><img src="data:image/gif;base64,{otherGif}" width="1" height="1"></p>""", main);
            main.Document = new(body);
        }

        stream.Position = 0;
        using var reopened = WordprocessingDocument.Open(stream, false);
        var drawingIds = reopened.MainDocumentPart!.Document!
            .Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>()
            .Select(_ => _.Id!.Value)
            .ToList();

        await Assert.That(drawingIds.Count).IsEqualTo(7);
        await Assert.That(drawingIds).HasDistinctItems();
        // One png per call, since each call stores its own, and the gif.
        await Assert.That(reopened.MainDocumentPart.ImageParts.Count()).IsEqualTo(3);
        await Assert.That(new OpenXmlValidator().Validate(reopened).Select(_ => _.Description)).IsEmpty();
    }
}
