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
}
